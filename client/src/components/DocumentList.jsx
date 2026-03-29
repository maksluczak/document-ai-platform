import { useEffect, useState } from 'react';
import './DocumentList.scss';

const DocumentList = () => {
    const [documents, setDocuments] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch('http://localhost:8080/api/documents')
            .then(res => res.json())
            .then(data => {
                setDocuments(data);
                setLoading(false);
            })
            .catch(err => console.error("Błąd pobierania:", err));
    }, []);

    if (loading) return <div className="loader">Loading...</div>;

    return (
        <div className="container">
            <h1>Przetworzone Dokumenty</h1>
            <table className="doc-table">
                <thead>
                <tr>
                    <th>Nazwa pliku</th>
                    <th>Typ</th>
                    <th>Data przetworzenia</th>
                    <th>Status</th>
                </tr>
                </thead>
                <tbody>
                {documents.map(doc => (
                    <tr key={doc.id}>
                        <td>{doc.fileName}</td>
                        <td>{doc.documentType}</td>
                        <td>{new Date(doc.processedAt).toLocaleString()}</td>
                        <td><span className="badge">Zakończono</span></td>
                    </tr>
                ))}
                </tbody>
            </table>
        </div>
    );
};

export default DocumentList;