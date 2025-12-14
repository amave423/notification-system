import pytest
from app.models import Message
from fastapi.testclient import TestClient
from system.service import app

@pytest.fixture
def client():
    return TestClient(app = app)

@pytest.fixture
def message():
    return Message(title = "TestTitle", body = "TestBody")