import { Navigate, Outlet } from 'react-router-dom'
import { getRoles } from '../../services/session'

export default function RequireRole({ roles }: { roles: string[] }) {
  const userRoles = getRoles()

  if (!roles.some((role) => userRoles.includes(role))) {
    return <Navigate to="/forbidden" replace />
  }
  return <Outlet />
}
