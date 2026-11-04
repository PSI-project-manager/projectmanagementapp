import { Link, useNavigate } from "react-router-dom";
import { hasRole, logout } from "../services/session";

export default function HomePage() {
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate("/login", { replace: true });
    };

    return(
        <div className="home-page">
            <h1> Contribute </h1>
            <Link to="/projects">View projects</Link>
            <Link to="/items/new">Create new item</Link>

            {hasRole("Admin") && (
                <>
                    <h1> Administrate </h1>
                    <Link to="/admin/users">View & manage users</Link>
                    <Link to="/admin/item-types">View & manage item types</Link>
                </>
            )}

            <button onClick={handleLogout}>Log out</button>
        </div>
    )

}
