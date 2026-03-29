import './Navbar.scss';

const NAV_ITEMS = [
    { id: 'upload',    label: 'Upload' },
];

const Navbar = ({ currentPage, onNavigate }) => (
    <nav className="navbar">
        <span className="navbar__brand">AI Document Platform</span>
        <ul className="navbar__links">
            {NAV_ITEMS.map(({ id, label }) => (
                <li key={id}>
                    <button
                        className={`navbar__link ${currentPage === id ? 'navbar__link--active' : ''}`}
                        onClick={() => onNavigate(id)}
                    >
                        {label}
                    </button>
                </li>
            ))}
        </ul>
    </nav>
);

export default Navbar;