import { useEffect } from 'react';
import './MetadataModel.scss';

const MetadataModal = ({ metadata, onClose }) => {
    useEffect(() => {
        const handleKey = e => { if (e.key === 'Escape') onClose(); };
        window.addEventListener('keydown', handleKey);
        return () => window.removeEventListener('keydown', handleKey);
    }, [onClose]);

    const entries = Object.entries(metadata);

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal" onClick={e => e.stopPropagation()}>
                <div className="modal__header">
                    <h3 className="modal__title">Metadata</h3>
                    <button className="modal__close" onClick={onClose} aria-label="Close">✕</button>
                </div>

                {entries.length === 0 ? (
                    <p className="modal__empty">No metadata available.</p>
                ) : (
                    <dl className="modal__list">
                        {entries.map(([key, value]) => (
                            <div className="modal__row" key={key}>
                                <dt className="modal__key">{key}</dt>
                                <dd className="modal__value">{value}</dd>
                            </div>
                        ))}
                    </dl>
                )}
            </div>
        </div>
    );
};

export default MetadataModal;