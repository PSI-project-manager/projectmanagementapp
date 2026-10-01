export interface Project {
  id: number
  name: string
  description?: string | null
  isActive: boolean
  createdByUserId: number
}

export interface CreateProjectInput {
  name: string
  description?: string
}

export interface UpdateProjectInput {
  name: string
  description?: string
}