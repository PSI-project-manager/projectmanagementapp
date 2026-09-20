import type { CreateItemTypeInput, ItemType, UpdateItemTypeInput } from '../../types/itemType'

const BASE_URL = '/api/item-types'

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

export async function listItemTypes(options?: { activeOnly?: boolean }): Promise<ItemType[]> {
  const query = options?.activeOnly ? '?activeOnly=true' : ''
  const response = await fetch(`${BASE_URL}${query}`, { headers: headers() })
  return parse<ItemType[]>(response)
}

export async function createItemType(input: CreateItemTypeInput): Promise<ItemType> {
  const response = await fetch(BASE_URL, {
    method: 'POST',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<ItemType>(response)
}

export async function updateItemType(id: number, input: UpdateItemTypeInput): Promise<ItemType> {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<ItemType>(response)
}