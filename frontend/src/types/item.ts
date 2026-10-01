export interface Item {
  id: number
  projectId: number
  title: string
  description?: string
  itemTypeId: number
  statusId: number
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