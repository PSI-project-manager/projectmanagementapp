import { jwtDecode } from 'jwt-decode'
import { Navigate, Outlet } from 'react-router-dom'

// The backend adds one Claim("role", name) per role, and JwtSecurityTokenHandler
// writes a lone claim as a string but several as an array.
type TokenPayload = {
  role?: string | string[]
}

function getRoles(token: string | null): string[] {
  if (!token) return []

  try {
    const { role } = jwtDecode<TokenPayload>(token)
    if (!role) return []
    return Array.isArray(role) ? role : [role]
  } catch {
    return []
  }
}

export default function RequireRole({ roles }: { roles: string[] }) {
  const userRoles = getRoles(localStorage.getItem('token'))

  if (!roles.some((role) => userRoles.includes(role))) {
    return <Navigate to="/forbidden" replace />
  }
  return <Outlet />
}
