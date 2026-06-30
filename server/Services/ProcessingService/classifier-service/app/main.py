from fastapi import FastAPI, HTTPException

from app.model import predict
from app.schemas import ClassifierRequest, ClassifierResponse

app = FastAPI(title="Document Classifier")

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/classify", response_model=ClassifierResponse)
def classify(request: ClassifierRequest):
    if not request.text and len(request.text) < 10:
        raise HTTPException(status_code=400, detail="Bad request. Text is too short.")

    doc_type, confidence, azure_model = predict(request.text)
    return ClassifierResponse(
        document_type=doc_type,
        confidence=confidence,
        azure_model=azure_model,
    )