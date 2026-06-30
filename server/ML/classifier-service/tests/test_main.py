from fastapi.testclient import TestClient
from app.main import app
import app.main as main

client = TestClient(app)

def test_classify_success(monkeypatch):
    def mock_predict(text):
        return "invoice", 0.9542, "prebuilt-invoice"

    monkeypatch.setattr(main, "predict", mock_predict)

    response = client.post(
        "/classify",
        json={"text": "Faktura VAT nr 109274618 Sprzedawca: PolBud NIP: 19937469912 "
                       "Kwota netto: 19000.00 PLN VAT 23% Termin płatności 14 dni"}
    )

    assert response.status_code == 200
    assert response.json() == {
        "document_type": "invoice",
        "confidence": 0.9542,
        "azure_model": "prebuilt-invoice"
    }

def test_classify_empty_text():
    response = client.post("/classify", json={"text": ""})
    assert response.status_code == 400
    assert response.json() == {"detail": "Bad request. Text is too short."}

def test_classify_too_short_text():
    response = client.post("/classify", json={"text": "Short"})
    assert response.status_code == 400
    assert response.json() == {"detail": "Bad request. Text is too short."}

def test_classify_missing_text_field():
    response = client.post("/classify", json={})
    assert response.status_code == 422

def test_classify_low_confidence_fallback(monkeypatch):
    def mock_predict(text):
        return "other", 0.42, "prebuilt-read"

    monkeypatch.setattr(main, "predict", mock_predict)

    response = client.post(
        "/classify",
        json={"text": "Jakiś niejednoznaczny tekst dokumentu, testowanie co na to model"}
    )

    assert response.status_code == 200
    assert response.json()["document_type"] == "other"

def test_health_endpoint():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}

def test_classify_long_text_does_not_crash(monkeypatch):
    captured_text = {}

    def mock_predict(text):
        captured_text["value"] = text
        return "other", 0.75, "prebuilt-read"

    monkeypatch.setattr(main, "predict", mock_predict)

    long_text = "Faktura " * 2000

    response = client.post("/classify", json={"text": long_text})

    assert response.status_code == 200
    assert len(captured_text["value"]) <= 2000