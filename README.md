# AI Document Processing Platform

### Project Overview

**AI Document Processing Platform** is a microservices-based backend system designed to automatically process uploaded documents (such as invoices or PDFs) using AI OCR and extract structured data.

The project demonstrates event-driven architecture, asynchronous processing, and integration with external AI services. It is designed as a lean, production-style system that showcases modern backend practices used in real-world distributed applications.

---

### System Architecture

![system-architecture](./docs/system-architecture.drawio.png)

* **Microservices Architecture:** Upload, Processing, and Document services operate independently.
* **Event-Driven Communication:** Services communicate via RabbitMQ events.
* **API Gateway:** YARP routes requests from frontend to backend services.
* **Asynchronous Processing:** AI processing runs in background workers.

---

### Technologies Used

#### Backend
* **.NET 8 (C#)** – Core backend framework
* **Entity Framework Core** – ORM for database operations
* **MassTransit** – Messaging abstraction for RabbitMQ

#### Infrastructure
* **PostgreSQL** – Relational database with Full-Text Search
* **RabbitMQ** – Event bus for service communication
* **MinIO** – S3-compatible object storage for documents
* **Docker Compose** – Local microservices infrastructure

#### AI & Frontend
* **Azure AI Document Intelligence** – OCR and document data extraction
* **React + Vite** – Lightweight frontend interface
* **YARP Gateway** – Unified API entry point

---

### Local Setup

#### Requirements

- Docker
- An Azure AI Document Intelligence resource (endpoint + key)
- The classifier model files in `server/ML/classifier-service/model`

For running services outside Docker: .NET SDK 8, Node 22, Python 3.12.

#### Getting started

```bash
git clone git@github.com:maksluczak/document-ai-platform.git
cd document-ai-platform
```

#### Azure AI Document Intelligence key

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

Put both values into `.env` as `AZURE_AI_ENDPOINT` and `AZURE_AI_KEY`.

#### Model
```bash
cd server/ML/classifier-service
python -m venv venv
source ./bin/venv/activate
pip install -r reqirements.txt -r reqirements-dev.txt
python scripts/download_model.py
```

#### Start application
```bash
cp .env.example .env # fill in AZURE_AI_ENDPOINT and AZURE_AI_KEY
docker compose up --build
```

- Application: http://localhost:3000
- RabbitMQ console: http://localhost:15672
- MinIO console: http://localhost:9001

---

### Key Features

#### Document Upload
* **File Upload API:** Upload PDF documents through a single endpoint.
* **Object Storage:** Files are stored in MinIO.
* **Event Publishing:** Upload triggers a `DocumentUploaded` event.

#### AI Processing
* **Background Worker:** Processing service consumes events from RabbitMQ.
* **AI Integration:** Documents are analyzed using Azure Document Intelligence.
* **Structured Data:** Extracted metadata (e.g. amount, date, vendor) is parsed from AI responses.

#### Data Storage & Search
* **Metadata Storage:** Processed documents saved in PostgreSQL.
* **Processing Status:** Documents tracked as `Processing` or `Completed`.