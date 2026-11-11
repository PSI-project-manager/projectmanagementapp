import type { Status } from '../../types/status'
import { parse } from '../../services/apiClient'

function headers(): HeadersInit {
  const token = localStorage.getItem('token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

export async function listStatuses(): Promise<Status[]> {
  const response = await fetch('/api/statuses', { headers: headers() })
  return parse<Status[]>(response)
}