from pydantic import BaseModel

class ClassifierRequest(BaseModel):
    text: str

class ClassifierResponse(BaseModel):
    document_type: str
    confidence: float
    azure_model: str