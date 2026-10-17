import { useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { uploadFiles } from '../api.js';

const UploadPanel = ({ onUploaded }) => {
    const [files, setFiles] = useState([]);
    const [uploading, setUploading] = useState(false);
    const [message, setMessage] = useState(null);

    const onDrop = (acceptedFiles, rejectedFiles) => {
        setFiles(previous => [...previous, ...acceptedFiles]);
        if (rejectedFiles.length > 0) {
            setMessage({ type: 'error', text: 'Only PDF files are accepted.' });
        } else {
            setMessage(null);
        }
    };

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: { 'application/pdf': ['.pdf'] },
        multiple: true,
    });

    const removeFile = index => setFiles(previous => previous.filter((_, position) => position !== index));

    const handleUpload = async () => {
        setUploading(true);
        setMessage(null);
        try {
            await uploadFiles(files);
            setMessage({ type: 'info', text: `${files.length} file(s) uploaded. Processing has started.` });
            setFiles([]);
            onUploaded();
        } catch (err) {
            setMessage({ type: 'error', text: err.message });
        } finally {
            setUploading(false);
        }
    };

    return (
        <div className="upload">
            <div {...getRootProps({ className: isDragActive ? 'dropzone dropzone--active' : 'dropzone' })}>
                <input {...getInputProps()} />
                <span>Drop PDF files here or <span className="link">browse</span></span>
            </div>

            {files.length > 0 && (
                <ul className="file-list">
                    {files.map((file, index) => (
                        <li key={`${file.name}-${index}`} className="file-list__item">
                            <span>{file.name}</span>
                            <button className="button button--text" onClick={() => removeFile(index)}>Remove</button>
                        </li>
                    ))}
                </ul>
            )}

            <div className="upload__actions">
                <button
                    className="button button--primary"
                    onClick={handleUpload}
                    disabled={uploading || files.length === 0}
                >
                    {uploading ? 'Uploading...' : 'Upload'}
                </button>
                {message && (
                    <span className={message.type === 'error' ? 'message message--error' : 'message'}>
                        {message.text}
                    </span>
                )}
            </div>
        </div>
    );
};

export default UploadPanel;