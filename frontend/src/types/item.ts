export interface Item {
  id: number
  projectId: number
  title: string
  description?: string | null
  itemTypeId: number
  statusId: number
  // Filled in by GET /api/items; the create response doesn't have them.
  itemTypeName?: string | null
  statusName?: string | null
}

export interface CreateItemInput {
  title: string
  description: string
  projectId: number
  itemTypeId: number
  statusId: number
}

export interface ProjectOption {
  id: number
  name: string
}