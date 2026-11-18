import type { CreateStatusInput, Status, UpdateStatusInput } from '../../types/status'
import { parse } from '../../services/apiClient'

const BASE_URL = '/api/statuses'

function headers(): HeadersInit {
  const token = localStorage.getItem('token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

export async function listStatuses(options?: { activeOnly?: boolean }): Promise<Status[]> {
  const query = options?.activeOnly ? '?activeOnly=true' : ''
  const response = await fetch(`${BASE_URL}${query}`, { headers: headers() })
  return parse<Status[]>(response)
}

export async function createStatus(input: CreateStatusInput): Promise<Status> {
  const response = await fetch(BASE_URL, {
    method: 'POST',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<Status>(response)
}

export async function updateStatus(id: number, input: UpdateStatusInput): Promise<Status> {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<Status>(response)
}