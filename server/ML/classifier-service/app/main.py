from fastapi import FastAPI, HTTPException

from app.model import predict
from app.schemas import ClassifierRequest, ClassifierResponse

app = FastAPI(title="Document Classifier")

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/classify", response_model=ClassifierResponse)
def classify(request: ClassifierRequest):
    if not request.text or len(request.text.strip()) < 10:
        raise HTTPException(status_code=400, detail="Bad request. Text is too short.")

    truncated_text = request.text[:2000]
    doc_type, confidence, azure_model = predict(truncated_text)

    return ClassifierResponse(
        document_type=doc_type,
        confidence=confidence,
        azure_model=azure_model,
    )