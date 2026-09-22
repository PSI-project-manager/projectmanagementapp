import type { CreateUserInput, UpdateUserInput, User } from '../../types/user'

const BASE_URL = '/api/users'

function headers(): HeadersInit {
  const token = localStorage.getItem('token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    // Backend errors are ProblemDetails: { status, title, detail }
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail || problem?.title || 'Request failed.')
  }
  return response.json()
}

export async function listUsers(options?: { activeOnly?: boolean }): Promise<User[]> {
  const query = options?.activeOnly ? '?activeOnly=true' : ''
  const response = await fetch(`${BASE_URL}${query}`, { headers: headers() })
  return parse<User[]>(response)
}

export async function createUser(input: CreateUserInput): Promise<User> {
  const response = await fetch(BASE_URL, {
    method: 'POST',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<User>(response)
}

export async function updateUser(id: number, input: UpdateUserInput): Promise<User> {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<User>(response)
}

// Activation is its own endpoint pair on the backend, not part of the update payload.
export async function setUserActive(id: number, isActive: boolean): Promise<User> {
  const action = isActive ? 'activate' : 'deactivate'
  const response = await fetch(`${BASE_URL}/${id}/${action}`, {
    method: 'POST',
    headers: headers(),
  })
  return parse<User>(response)
}
