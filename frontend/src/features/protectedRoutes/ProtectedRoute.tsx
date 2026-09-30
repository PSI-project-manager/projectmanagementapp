import { jwtDecode } from 'jwt-decode'
import { Navigate, Outlet } from 'react-router-dom'

export default function ProtectedRoute( ) {

  const token = localStorage.getItem('token');
  const validToken = token && jwtDecode<{ exp: number }>(token).exp * 1000 > Date.now();

  if (!token || !validToken) {
    return <Navigate to="/login" replace />;
  }
  return <Outlet/>
}