import './App.scss';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/layout/Navbar.jsx';
import DashboardPage from "./pages/DashboardPage.jsx";
import UploadPage from "./pages/UploadPage.jsx";

function App() {
    return (
        <Router>
            <div className="app">
                <Navbar />
                <main className="app__content">
                    <Routes>
                        <Route path="/" element={<DashboardPage />} />
                        <Route path="/upload" element={<UploadPage />} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;