from fastapi import FastAPI
from monitoring import logger
from datetime import date, timedelta
import os
import random
import httpx

# Enable remote debugging (debugpy) when launched by Aspire with ENABLE_DEBUGPY=1.
# The process listens on port 5678; attach from VS Code using "Attach to PyApi01".
# Inert in production because the env var is not set there.
if os.environ.get("ENABLE_DEBUGPY") == "1":
    import debugpy
    debugpy.listen(("0.0.0.0", 5678))
    logger.info("debugpy listening on 0.0.0.0:5678 — attach with VS Code 'Attach to PyApi01'.")

logger.info(f"Program started.")

app = FastAPI()

@app.get("/")
def read_root():
    logger.info(f"Root endpoint accessed.")
    return {"message": "Hello from FastAPI"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}


def _apiservice_base_url() -> str:
    # preferisci https, con fallback su http
    return (
        os.getenv("services__apiservice__https__0")
        or os.getenv("services__apiservice__http__0")
        or "http://localhost:5000"  # fallback per esecuzione standalone
    )


@app.get("/proxy-weather")
async def proxy_weather():
    base = _apiservice_base_url()
    logger.info(f"Chiamo apiservice su {base}")
    async with httpx.AsyncClient(verify=False) as client:  # dev cert self-signed
        resp = await client.get(f"{base}/weatherforecast")
        resp.raise_for_status()
        return resp.json()

@app.get("/weatherforecast")
async def read_weather_forecast():
    logger.info(f"Weather forecast endpoint accessed.")

    logger.info(f"Using API service base URL: {_apiservice_base_url()}")

    # Recupera l'output di proxy_weather aspettando la coroutine.
    x = await proxy_weather()
    logger.info(f"I've received {len(x)} records from apiservice: {x}")

    return x

    summaries = ["Helado", "Refrescante", "Frío", "Fresco", "Templado",
                 "Cálido", "Agradable", "Caliente", "Sofocante", "Abrumador"]
    today = date.today()
    return [
        {
            "date": (today + timedelta(days=i)).isoformat(),
            "temperatureC": random.randint(-20, 55),
            "summary": random.choice(summaries),
        }
        for i in range(1, 6)
    ]