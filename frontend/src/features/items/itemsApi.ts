import type { CreateItemInput, Item, ItemFilters, ProjectOption } from '../../types/item'
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

// With no filters this returns every item the user can access.
export async function listItems(filters: ItemFilters = {}): Promise<Item[]> {
  const params = new URLSearchParams()
  if (filters.projectId != null) params.set('projectId', String(filters.projectId))
  if (filters.itemTypeId != null) params.set('itemTypeId', String(filters.itemTypeId))
  if (filters.statusId != null) params.set('statusId', String(filters.statusId))

  const query = params.toString()
  const response = await fetch(`/api/items${query ? `?${query}` : ''}`, { headers: headers() })
  return parse<Item[]>(response)
}