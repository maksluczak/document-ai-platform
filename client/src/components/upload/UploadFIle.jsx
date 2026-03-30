import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import './UploadFile.scss';

const UploadFile = () => {
    const [files, setFiles] = useState([]);
    const [loading, setLoading] = useState(false);

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

    const handleUpload = async () => {
        if (files.length === 0) return;

        try {
            setLoading(true);

            const formData = new FormData();
            files.forEach(file => {
                formData.append("files", file);
            });

            const response = await fetch("http://localhost:8080/api/upload", {
                method: "POST",
                body: formData
            });

            if (!response.ok) {
                const errorData = await response.text();
                throw new Error(errorData || "Upload failed.");
            }

            alert("File uploaded successfully.");
            setFiles([]);

        } catch (err) {
            console.error("Upload error:", err);
            alert(`Error: ${err.message}`);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="upload-card">
            <div {...getRootProps()} className={zoneClass}>
                <input {...getInputProps()} />
                <div className="dropzone__icon">↑</div>
                <p className="dropzone__label">Drag & drop PDFs here</p>
            </div>
            {files.length > 0 && (
                <ul className="file-list">
                    {files.map(file => (
                        <li key={file.name} className="file-list__item">
                            {file.name}
                            <button onClick={() => removeFile(file.name)}>✕</button>
                        </li>
                    ))}
                </ul>
            )}
            <div className="upload-actions">
                <button
                    className="btn-primary"
                    onClick={handleUpload}
                    disabled={loading || files.length === 0}
                >
                    {loading ? 'Uploading...' : `Upload ${files.length} files`}
                </button>
            </div>
        </div>
    );
};

export default UploadFile;