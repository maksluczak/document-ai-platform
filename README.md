# AI Document Processing Platform

## Project Overview

**AI Document Processing Platform** is a microservices-based system that classifies uploaded PDF documents and extracts structured data from them.

A document goes through three steps: OCR, classification by a self-trained model, and field extraction by Azure AI Document Intelligence. The project demonstrates event-driven architecture, asynchronous processing, and integration with external AI services.

---

## System Architecture

![system-architecture](./docs/system-architecture.drawio.png)

* **Microservices Architecture:** Upload, Processing, and Document services operate independently.
* **Event-Driven Communication:** Services communicate via RabbitMQ events.
* **API Gateway:** YARP routes requests from frontend to backend services.
* **Asynchronous Processing:** AI processing runs in a background worker.
* **Classifier Service:** A separate Python service decides the document type before data extraction.

---

## Key Features

### Document Upload
* **File Upload API:** Upload PDF documents through a single endpoint.
* **Object Storage:** Files are stored in MinIO.
* **Event Publishing:** Upload triggers a `DocumentUploaded` event.

### AI Processing
* **Background Worker:** Processing service consumes events from RabbitMQ.
* **OCR:** Text is read from the document using Azure AI Document Intelligence.
* **Classification:** A fine-tuned DistilBERT model assigns one of eight types: `invoice`, `receipt`, `bank_statement`, `tax_document`, `legal`, `hr`, `logistics`, `other`.
* **Structured Data:** The document type selects the Azure model used to extract fields (e.g. amount, date, vendor).

### Data Storage
* **Metadata Storage:** Processed documents and their extracted fields are saved in PostgreSQL.
* **Document List:** The frontend shows processed documents with their type and fields.

---

## Technologies Used

### Backend
* **.NET 8 (C#)** – Core backend framework
* **Entity Framework Core** – ORM for database operations
* **MassTransit** – Messaging abstraction for RabbitMQ
* **YARP** – API gateway

### Machine Learning
* **Python + FastAPI** – Classifier service
* **DistilBERT (Hugging Face Transformers)** – Document type classification
* **Azure AI Document Intelligence** – OCR and document data extraction

### Frontend
* **React + Vite** – Lightweight frontend interface

### Infrastructure
* **PostgreSQL** – Relational database
* **RabbitMQ** – Event bus for service communication
* **MinIO** – S3-compatible object storage for documents
* **Docker Compose** – Local microservices infrastructure

---

## Local Setup

### Requirements

- Docker with Compose v2
- Python 3.12 (to download the classifier model)
- An Azure AI Document Intelligence resource (endpoint + key)

For running services outside Docker: .NET SDK 8 and Node 22.

### 1. Clone the repository

```bash
git clone https://github.com/maksluczak/document-ai-platform.git
cd document-ai-platform
cp .env.example .env
```

### 2. Create an Azure AI Document Intelligence key

The free tier (F0) is enough. With the Azure CLI:

```bash
az login
az group create --name rg-document-ai --location westeurope
az cognitiveservices account create \
    --name <unique-name> --resource-group rg-document-ai \
    --kind FormRecognizer --sku F0 --location westeurope --yes
az cognitiveservices account show --name <unique-name> \
    --resource-group rg-document-ai --query properties.endpoint -o tsv
az cognitiveservices account keys list --name <unique-name> \
    --resource-group rg-document-ai --query key1 -o tsv
```

The same resource can be created in the Azure portal: "Document Intelligence" → Create → pricing tier F0. The endpoint and key are in the "Keys and Endpoint" tab.

Put both values into `.env` as `AZURE_AI_ENDPOINT` and `AZURE_AI_KEY`.

> The free tier analyzes only the first two pages of each document.

### 3. Download the classifier model

```bash
cd server/ML/classifier-service
python3 -m venv venv
source venv/bin/activate
pip install -r requirements-dev.txt
python scripts/download_model.py
cd ../../..
```

The model is downloaded from the Hugging Face Hub (`maksluczak/document-classifier`) into `server/ML/classifier-service/model`.

### 4. Start the application

```bash
docker compose up --build
```

| Service | Address | Login |
|---|---|---|
| Application | http://localhost:3000 | – |
| RabbitMQ console | http://localhost:15672 | `RABBITMQ_USER` / `RABBITMQ_PASSWORD` from `.env` |
| MinIO console | http://localhost:9001 | `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` from `.env` |

### Stopping

```bash
docker compose down # stop containers, keep data
docker compose down -v # stop containers and delete data
```

---

## API

All requests go through the gateway, available under the same address as the application.

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/upload` | Upload one or more PDF documents |
| `GET` | `/api/documents` | List processed documents |

---

## Tests

```bash
# backend
cd server
dotnet test

# classifier
cd server/ML/classifier-service
source venv/bin/activate
pip install -r requirements.txt -r requirements-dev.txt
pytest
```

---

## Project Structure

```
.
├── client
├── docker-compose.yaml
├── docs
└── server
    ├── ML
    │   └── classifier-service
    ├── Services
    │   ├── ApiGateway
    │   ├── DocumentService
    │   ├── ProcessingService
    │   └── UploadService
    ├── Shared
    │   └── Contracts
    └── Tests
```