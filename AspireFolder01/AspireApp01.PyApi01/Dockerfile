FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY main.py monitoring.py favicon.ico ./

# uvicorn come ENTRYPOINT: Docker Compose sovrascrive solo il CMD (gli argomenti),
# non l'ENTRYPOINT. Così il "command" generato da Aspire (main:app --host ... --port ...)
# viene passato come argomenti a uvicorn.
ENTRYPOINT ["uvicorn"]
CMD ["main:app", "--host", "0.0.0.0", "--port", "8000"]