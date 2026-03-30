import './Navbar.scss';
import {Link} from "react-router-dom";

const Navbar = () => (
    <nav className="navbar">
        <Link className="navbar__brand" to="/">AI Document Platform</Link>
        <div className="navbar__links">
            <Link className="navbar__link" to="/upload">Upload</Link>
        </div>
    </nav>
);

export default Navbar;