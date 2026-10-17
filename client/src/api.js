export async function fetchDocuments() {
    const response = await fetch('/api/documents');
    if (!response.ok) {
        throw new Error(`Could not load documents (HTTP ${response.status}).`);
    }
    return response.json();
}

export async function uploadFiles(files) {
    const formData = new FormData();
    files.forEach(file => formData.append('files', file));

    const response = await fetch('/api/upload', { method: 'POST', body: formData });
    if (!response.ok) {
        const body = await response.text();
        throw new Error(readErrorMessage(body) || `Upload failed (HTTP ${response.status}).`);
    }
}

function readErrorMessage(body) {
    try {
        const parsed = JSON.parse(body);
        return parsed.message ?? parsed.Message ?? parsed.title ?? body;
    } catch {
        return body;
    }
}