import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import DocumentList from './components/DocumentList';
import { MyDropzone } from './hooks/MyDropzone';
import './App.scss';

function App() {
    return (
        <Router>
            <div className="app-layout">
                <nav className="navbar">
                    <div className="logo">AI Document System</div>
                    <div className="links">
                        <Link to="/upload" className="btn-upload">Add new file</Link>
                    </div>
                </nav>

                <main className="content">
                    <Routes>
                        <Route path="/" element={<DocumentList />} />
                        <Route path="/upload" element={<MyDropzone />} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;