from fastapi import FastAPI
from monitoring import logger
from datetime import date, timedelta
import random

logger.info(f"Program started.")

app = FastAPI()

@app.get("/")
def read_root():
    logger.info(f"Root endpoint accessed.")
    return {"message": "Hello from FastAPI"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}

@app.get("/weatherforecast")
def read_weather_forecast():
    logger.info(f"Weather forecast endpoint accessed.")
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