import type { CreateItemInput, Item, ProjectOption } from '../../types/item'
import { parse } from '../../services/apiClient'

function headers(): HeadersInit {
  const token = localStorage.getItem('token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

export async function createItem(input: CreateItemInput): Promise<Item> {
  const response = await fetch('/api/items', {
    method: 'POST',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<Item>(response)
}

// TODO: replace once ProjectsPage moves off the mock API and has a real real-ID client.
export async function listProjectOptions(): Promise<ProjectOption[]> {
  const response = await fetch('/api/projects', { headers: headers() })
  return parse<ProjectOption[]>(response)
}