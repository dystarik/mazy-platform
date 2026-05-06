FROM python:3.13-alpine

WORKDIR /app
COPY exporter.py /app/exporter.py

ENV PYTHONUNBUFFERED=1
EXPOSE 9487

CMD ["python", "/app/exporter.py"]
