from pathlib import Path
from huggingface_hub import snapshot_download

MODEL_REPOSITORY = "maksluczak/document-classifier"
TARGET_DIRECTORY = Path(__file__).resolve().parent.parent / "model"

snapshot_download(repo_id=MODEL_REPOSITORY, local_dir=TARGET_DIRECTORY)