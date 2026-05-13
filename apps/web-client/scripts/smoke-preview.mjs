import { spawn } from 'node:child_process'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const DEFAULT_HOST = '127.0.0.1'
const DEFAULT_PORT = '4173'
const DEFAULT_TIMEOUT_MS = 15_000
const DEFAULT_INTERVAL_MS = 250

export function buildSmokeUrl({
  host = process.env.FRONTEND_SMOKE_HOST ?? DEFAULT_HOST,
  port = process.env.FRONTEND_SMOKE_PORT ?? DEFAULT_PORT,
} = {}) {
  return new URL(`http://${host}:${port}/`)
}

export function isAcceptableSmokeHtml(html) {
  if (typeof html !== 'string' || html.trim().length === 0) {
    return false
  }

  const hasAppMount = /<div\s+[^>]*id=["']app["'][^>]*>/i.test(html)
  const hasViteModuleAsset =
    /<script\s+[^>]*type=["']module["'][^>]*src=["'][^"']*\/assets\/[^"']+\.js["'][^>]*>/i.test(
      html,
    )

  return hasAppMount && hasViteModuleAsset
}

async function waitForSmokeResponse(
  url,
  {
    timeoutMs = DEFAULT_TIMEOUT_MS,
    intervalMs = DEFAULT_INTERVAL_MS,
    fetchImpl = fetch,
    isServerRunning = () => true,
  } = {},
) {
  const deadline = Date.now() + timeoutMs
  let lastError = new Error('preview server did not respond yet')

  while (Date.now() < deadline) {
    if (!isServerRunning()) {
      throw new Error('preview server exited before smoke check completed')
    }

    try {
      const response = await fetchImpl(url)
      const body = await response.text()

      if (response.ok && isAcceptableSmokeHtml(body)) {
        return
      }

      lastError = new Error(`unexpected response ${response.status} from ${url}`)
    } catch (error) {
      lastError = error
    }

    await new Promise(resolve => setTimeout(resolve, intervalMs))
  }

  throw new Error(`frontend smoke check failed for ${url}: ${lastError.message}`)
}

export function buildPreviewCommand({
  cwd = process.cwd(),
  host = process.env.FRONTEND_SMOKE_HOST ?? DEFAULT_HOST,
  port = process.env.FRONTEND_SMOKE_PORT ?? DEFAULT_PORT,
} = {}) {
  return {
    command: process.execPath,
    args: [
      path.join(cwd, 'node_modules', 'vite', 'bin', 'vite.js'),
      'preview',
      '--host',
      host,
      '--port',
      port,
      '--strictPort',
    ],
  }
}

async function runSmokePreview() {
  const host = process.env.FRONTEND_SMOKE_HOST ?? DEFAULT_HOST
  const port = process.env.FRONTEND_SMOKE_PORT ?? DEFAULT_PORT
  const url = buildSmokeUrl({ host, port })
  const previewCommand = buildPreviewCommand({ host, port })

  const preview = spawn(
    previewCommand.command,
    previewCommand.args,
    {
      cwd: process.cwd(),
      stdio: ['ignore', 'pipe', 'pipe'],
    },
  )

  preview.stdout.on('data', chunk => process.stdout.write(chunk))
  preview.stderr.on('data', chunk => process.stderr.write(chunk))
  const previewStartFailure = new Promise((_, reject) => {
    preview.once('error', reject)
  })

  try {
    await Promise.race([
      waitForSmokeResponse(url, {
        isServerRunning: () => preview.exitCode === null,
      }),
      previewStartFailure,
    ])
    console.log(`Frontend smoke check passed: ${url}`)
  } finally {
    if (preview.exitCode === null && !preview.killed) {
      preview.kill()
    }
  }
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  runSmokePreview().catch(error => {
    console.error(error.message)
    process.exitCode = 1
  })
}
