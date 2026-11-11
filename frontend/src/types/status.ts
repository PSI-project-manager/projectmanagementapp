export interface Status {
  id: number
  name: string
  sortOrder: number
  isActive: boolean
}

export interface CreateStatusInput {
  name: string
  sortOrder?: number
}

export interface UpdateStatusInput {
  name: string
  sortOrder: number
  isActive: boolean
}