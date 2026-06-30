import os
from transformers import pipeline

MODEL_PATH = os.getenv("MODEL_PATH", "./model")
_classifier = None

AZURE_MODEL_MAP = {
    "invoice": "prebuilt-invoice",
    "receipt": "prebuilt-receipt",
    "bank_statement": "prebuilt-document",
    "tax_document": "prebuilt-document",
    "hr": "prebuilt-document",
    "legal": "prebuilt-document",
    "logistics": "prebuilt-document",
    "other": "prebuilt-read",
}

def get_classifier():
    global _classifier
    if _classifier is None:
        _classifier = pipeline(
            task="text-classification",
            model=MODEL_PATH,
            device=-1
        )
    return _classifier

def predict(text: str):
    clf = get_classifier()
    result = clf(text[:1000], truncation=True)[0]
    doc_type   = result["label"]
    confidence = round(result["score"], 4)
    azure_model = AZURE_MODEL_MAP.get(doc_type, "prebuilt-read")
    return doc_type, confidence, azure_model