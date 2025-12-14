from fastapi import FastAPI
import logging
from app.rabbitmq.consumer import RabbitMQConsumer
from contextlib import asynccontextmanager

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s: %(message)s"
)

@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.consumer = RabbitMQConsumer(
        rabbitmq_url = 'amqp://guest:guest@rabbitmq:5672/',
        queue_name = 'test')
    await app.state.consumer.start_consuming()
    yield

app = FastAPI(lifespan=lifespan)
