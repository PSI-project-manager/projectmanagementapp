import { jwtDecode } from 'jwt-decode'
import { Navigate, Outlet } from 'react-router-dom'

function isTokenValid(token: string | null) {
  return !!token && jwtDecode<{ exp: number }>(token).exp * 1000 > Date.now();
}

export default function ProtectedRoute( ) {

  const token = localStorage.getItem('token');

  if (!isTokenValid(token)) {
    return <Navigate to="/login" replace />;
  }
  return <Outlet/>
}
