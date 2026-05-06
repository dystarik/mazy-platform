import json
import socket
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import quote


DOCKER_SOCKET = "/var/run/docker.sock"
PROJECT_LABEL = "com.docker.compose.project"
SERVICE_LABEL = "com.docker.compose.service"
PROJECT_NAME = "mazy-platform-dev"


def docker_get(path: str) -> dict | list:
    request = (
        f"GET {path} HTTP/1.1\r\n"
        "Host: docker\r\n"
        "Connection: close\r\n"
        "\r\n"
    ).encode("utf-8")

    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as client:
        client.settimeout(5)
        client.connect(DOCKER_SOCKET)
        client.sendall(request)

        chunks = []
        while True:
            chunk = client.recv(65536)
            if not chunk:
                break
            chunks.append(chunk)

    response = b"".join(chunks)
    headers, _, body = response.partition(b"\r\n\r\n")
    if b"transfer-encoding: chunked" in headers.lower():
        body = decode_chunked(body)
    return json.loads(body.decode("utf-8"))


def decode_chunked(body: bytes) -> bytes:
    decoded = bytearray()
    index = 0

    while index < len(body):
        line_end = body.find(b"\r\n", index)
        if line_end == -1:
            break

        chunk_size_text = body[index:line_end].split(b";", 1)[0]
        chunk_size = int(chunk_size_text, 16)
        index = line_end + 2

        if chunk_size == 0:
            break

        decoded.extend(body[index:index + chunk_size])
        index += chunk_size + 2

    return bytes(decoded)


def prom_escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace("\n", "\\n").replace('"', '\\"')


def metric(name: str, labels: dict[str, str], value: float) -> str:
    label_text = ",".join(f'{key}="{prom_escape(val)}"' for key, val in labels.items())
    return f"{name}{{{label_text}}} {value}"


def memory_working_set(stats: dict) -> tuple[int, int]:
    memory = stats.get("memory_stats", {})
    usage = int(memory.get("usage") or 0)
    memory_stats = memory.get("stats") or {}
    inactive_file = int(
        memory_stats.get("inactive_file")
        or memory_stats.get("total_inactive_file")
        or 0
    )
    working_set = max(usage - inactive_file, 0)
    return usage, working_set


def collect_metrics() -> str:
    containers = docker_get("/containers/json?all=1")
    lines = [
        "# HELP mazy_container_memory_usage_bytes Docker container memory usage from Docker stats.",
        "# TYPE mazy_container_memory_usage_bytes gauge",
        "# HELP mazy_container_memory_working_set_bytes Docker container memory usage minus inactive file cache.",
        "# TYPE mazy_container_memory_working_set_bytes gauge",
        "# HELP mazy_container_up Docker container running state, 1 for running.",
        "# TYPE mazy_container_up gauge",
    ]

    running_containers = []

    for container in containers:
        labels = container.get("Labels") or {}
        if labels.get(PROJECT_LABEL) != PROJECT_NAME:
            continue

        container_id = container.get("Id", "")
        service = labels.get(SERVICE_LABEL) or "unknown"
        name = (container.get("Names") or [""])[0].lstrip("/")
        state = container.get("State") or ""

        base_labels = {
            "container": name,
            "service": service,
        }

        lines.append(metric("mazy_container_up", base_labels, 1 if state == "running" else 0))

        if state != "running" or not container_id:
            continue

        running_containers.append((container_id, base_labels))

    with ThreadPoolExecutor(max_workers=12) as executor:
        futures = {
            executor.submit(collect_container_memory, container_id): base_labels
            for container_id, base_labels in running_containers
        }

        for future in as_completed(futures):
            base_labels = futures[future]
            try:
                usage, working_set = future.result()
            except Exception:
                continue

            lines.append(metric("mazy_container_memory_usage_bytes", base_labels, usage))
            lines.append(metric("mazy_container_memory_working_set_bytes", base_labels, working_set))

    lines.append(f"mazy_docker_stats_exporter_scrape_timestamp_seconds {time.time()}")
    return "\n".join(lines) + "\n"


def collect_container_memory(container_id: str) -> tuple[int, int]:
    stats_path = f"/containers/{quote(container_id, safe='')}/stats?stream=false"
    stats = docker_get(stats_path)
    return memory_working_set(stats)


class Handler(BaseHTTPRequestHandler):
    def do_GET(self) -> None:
        if self.path not in ("/", "/metrics"):
            self.send_response(404)
            self.end_headers()
            return

        try:
            body = collect_metrics().encode("utf-8")
            self.send_response(200)
        except Exception as exc:
            body = f"mazy_docker_stats_exporter_error 1\n# {exc}\n".encode("utf-8")
            self.send_response(500)

        self.send_header("Content-Type", "text/plain; version=0.0.4; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, format: str, *args) -> None:
        return


if __name__ == "__main__":
    server = ThreadingHTTPServer(("0.0.0.0", 9487), Handler)
    server.serve_forever()
