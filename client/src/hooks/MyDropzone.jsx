import { useCallback } from 'react'
import { useDropzone } from 'react-dropzone'

export const MyDropzone = () => {
    const onDrop = useCallback(acceptedFiles => {
        console.log('Loaded files: ', acceptedFiles);
    }, [])
    const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf']
        },
        multiple: true
    })

    return (
        <div
            {...getRootProps()}
            className={`dropzone-container ${isDragActive ? 'active' : ''} ${isDragReject ? 'rejected' : ''}`}
        >
            <input {...getInputProps()} />
            <div className="dropzone-content">
                {isDragActive ? (
                    <p>Drop the files here ...</p>
                ) : (
                    <p>Drag 'n' drop some files here, or click to select files</p>
                )}
                <span className="hint">Only .pdf files accepted</span>
            </div>
        </div>
    )
}