import { Link } from "react-router-dom";

export default function HomePage() {
    return(
        <div>
            <h1> Contribute </h1>
            <Link to="/projects">View projects</Link>
            <Link to="/items/new">Create new item</Link>

            <h1> Administrate </h1>
            <Link to="/admin/users">View & manage users</Link>
            <Link to="/admin/item-types">View & manage item types</Link>
        </div>
    )
    
}