export interface ItemType {
  id: number
  name: string
  isActive: boolean
}

export interface CreateItemTypeInput {
  name: string
}

export interface UpdateItemTypeInput {
  name: string
  isActive: boolean
}