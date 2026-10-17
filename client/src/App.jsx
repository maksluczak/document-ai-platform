import { useCallback, useEffect, useState } from 'react';
import { fetchDocuments } from './api.js';
import { statusName } from './components/documentStatus.js';
import UploadPanel from './components/UploadPanel.jsx';
import DocumentTable from './components/DocumentTable.jsx';

const REFRESH_INTERVAL_MS = 3000;

function App() {
    const [documents, setDocuments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const loadDocuments = useCallback(() => {
        return fetchDocuments()
            .then(data => {
                setDocuments(data);
                setError(null);
            })
            .catch(err => setError(err.message))
            .finally(() => setLoading(false));
    }, []);

    useEffect(() => {
        loadDocuments();
    }, [loadDocuments]);

    const hasProcessingDocuments = documents.some(document => statusName(document.documentStatus) === 'Processing');

    useEffect(() => {
        if (!hasProcessingDocuments) return undefined;
        const timer = setInterval(loadDocuments, REFRESH_INTERVAL_MS);
        return () => clearInterval(timer);
    }, [hasProcessingDocuments, loadDocuments]);

    return (
        <>
            <header className="app-header">
                <span className="app-header__title">Document AI Platform</span>
            </header>

            <main className="app-content">
                <section className="panel">
                    <h2 className="panel__title">Upload documents</h2>
                    <UploadPanel onUploaded={loadDocuments} />
                </section>

                <section className="panel">
                    <div className="panel__header">
                        <h2 className="panel__title">Documents</h2>
                        <button className="button" onClick={loadDocuments}>Refresh</button>
                    </div>
                    <DocumentTable documents={documents} loading={loading} error={error} />
                </section>
            </main>
        </>
    );
}

export default App;