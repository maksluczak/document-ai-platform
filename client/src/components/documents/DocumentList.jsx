import { useEffect, useState } from 'react';
import MetadataModal from './MetadataModel.jsx';
import './DocumentList.scss';

const DocumentList = () => {
    const [documents, setDocuments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [selectedMetadata, setSelectedMetadata] = useState(null);

    useEffect(() => {
        fetch('http://localhost:8080/api/documents')
            .then(res => {
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                return res.json();
            })
            .then(data => setDocuments(data))
            .catch(err => setError(err.message))
            .finally(() => setLoading(false));
    }, []);

    if (loading) return <div className="state-message">Loading documents…</div>;
    if (error) return <div className="state-message state-message--error">Error: {error}</div>;
    if (!documents.length) return <div className="state-message">No documents found.</div>;

    return (
        <>
            <div className="doc-table-wrapper">
                <table className="doc-table">
                    <thead>
                    <tr>
                        <th>File name</th>
                        <th>Type</th>
                        <th>Blob URL</th>
                        <th>Processed at</th>
                        <th>Status</th>
                        <th>Metadata</th>
                    </tr>
                    </thead>
                    <tbody>
                    {documents.map(doc => (
                        <tr key={doc.id}>
                            <td className="doc-table__filename">{doc.fileName}</td>
                            <td>{doc.documentType}</td>
                            <td>
                                <a
                                    href={doc.blobUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="doc-table__link"
                                >
                                    Open
                                </a>
                            </td>
                            <td>{new Date(doc.processedAt).toLocaleString()}</td>
                            <td>
                                <span className="badge">Done</span>
                            </td>
                            <td>
                                <button
                                    className="meta-btn"
                                    onClick={() => setSelectedMetadata(doc.metadata)}
                                >
                                    View
                                </button>
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>

            {selectedMetadata && (
                <MetadataModal
                    metadata={selectedMetadata}
                    onClose={() => setSelectedMetadata(null)}
                />
            )}
        </>
    );
};

export default DocumentList;