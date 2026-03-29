import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import './UploadFile.scss';

const UploadFile = () => {
    const [files, setFiles] = useState([]);

    const onDrop = useCallback(acceptedFiles => {
        setFiles(prev => [...prev, ...acceptedFiles]);
    }, []);

    const removeFile = (name) => setFiles(prev => prev.filter(f => f.name !== name));

    const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
        onDrop,
        accept: { 'application/pdf': ['.pdf'] },
        multiple: true,
    });

    const zoneClass = [
        'dropzone',
        isDragActive  && 'dropzone--active',
        isDragReject  && 'dropzone--rejected',
    ].filter(Boolean).join(' ');

    return (
        <div className="upload-card">
            <div {...getRootProps()} className={zoneClass}>
                <input {...getInputProps()} />
                <div className="dropzone__icon" aria-hidden>
                    {isDragReject ? '✕' : '↑'}
                </div>
                <p className="dropzone__label">
                    {isDragActive
                        ? 'Drop files here…'
                        : 'Drag & drop PDFs here, or click to browse'}
                </p>
                <span className="dropzone__hint">Only .pdf files are accepted</span>
            </div>

            {files.length > 0 && (
                <ul className="file-list">
                    {files.map(file => (
                        <li key={file.name} className="file-list__item">
                            <span className="file-list__name">{file.name}</span>
                            <span className="file-list__size">
                                {(file.size / 1024).toFixed(1)} KB
                            </span>
                            <button
                                className="file-list__remove"
                                onClick={() => removeFile(file.name)}
                                aria-label={`Remove ${file.name}`}
                            >
                                ✕
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
};

export default UploadFile;