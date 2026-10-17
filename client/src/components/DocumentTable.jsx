import { Fragment, useState } from 'react';
import { statusName } from './documentStatus.js';

function formatDate(value) {
    if (!value) return '-';
    const date = new Date(value);
    if (date.getFullYear() < 2000) return '-';
    return date.toLocaleString();
}

function sortNewestFirst(documents) {
    return [...documents].sort((first, second) =>
        new Date(second.uploadedAt ?? second.processedAt) - new Date(first.uploadedAt ?? first.processedAt));
}

const DocumentTable = ({ documents, loading, error }) => {
    const [expandedId, setExpandedId] = useState(null);

    if (loading) return <p className="message">Loading documents...</p>;
    if (error) return <p className="message message--error">{error}</p>;
    if (documents.length === 0) return <p className="message">No documents yet. Upload a PDF to get started.</p>;

    const toggle = id => setExpandedId(current => (current === id ? null : id));

    return (
        <div className="table-wrapper">
            <table className="table">
                <thead>
                    <tr>
                        <th>File name</th>
                        <th>Type</th>
                        <th>Status</th>
                        <th>Uploaded</th>
                        <th>Processed</th>
                        <th>Fields</th>
                    </tr>
                </thead>
                <tbody>
                    {sortNewestFirst(documents).map(document => {
                        const status = statusName(document.documentStatus);
                        const fields = Object.entries(document.metadata ?? {}).filter(([, value]) => value);
                        const expanded = expandedId === document.id;

                        return (
                            <Fragment key={document.id}>
                                <tr>
                                    <td>{document.fileName}</td>
                                    <td>{document.documentType || '-'}</td>
                                    <td>
                                        <span className={`status status--${status.toLowerCase()}`}>{status}</span>
                                        {status === 'Failed' && document.error && (
                                            <div className="table__error">{document.error}</div>
                                        )}
                                    </td>
                                    <td>{formatDate(document.uploadedAt)}</td>
                                    <td>{formatDate(document.processedAt)}</td>
                                    <td>
                                        {fields.length > 0 ? (
                                            <button className="button button--text" onClick={() => toggle(document.id)}>
                                                {expanded ? 'Hide' : `Show (${fields.length})`}
                                            </button>
                                        ) : '-'}
                                    </td>
                                </tr>
                                {expanded && (
                                    <tr className="table__details">
                                        <td colSpan={6}>
                                            <dl className="fields">
                                                {fields.map(([key, value]) => (
                                                    <Fragment key={key}>
                                                        <dt>{key}</dt>
                                                        <dd>{value}</dd>
                                                    </Fragment>
                                                ))}
                                            </dl>
                                        </td>
                                    </tr>
                                )}
                            </Fragment>
                        );
                    })}
                </tbody>
            </table>
        </div>
    );
};

export default DocumentTable;