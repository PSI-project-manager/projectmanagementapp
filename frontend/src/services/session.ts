import { jwtDecode } from 'jwt-decode'

// The backend adds one Claim("role", name) per role, and JwtSecurityTokenHandler
// writes a lone claim as a string but several as an array.
type TokenPayload = {
  role?: string | string[]
}

/**
 * Roles carried by the stored token. Empty when signed out, when the token has
 * no role claim, or when it cannot be decoded at all.
 */
export function getRoles(): string[] {
  const token = localStorage.getItem('token')
  if (!token) return []

  try {
    const { role } = jwtDecode<TokenPayload>(token)
    if (!role) return []
    return Array.isArray(role) ? role : [role]
  } catch {
    return []
  }
}

export function hasRole(role: string): boolean {
  return getRoles().includes(role)
}
