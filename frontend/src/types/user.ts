export interface User {
  id: number
  email: string
  fullName: string
  isActive: boolean
  // The backend's UserDto does not return roles yet, so this is optional and
  // the role editor treats a missing value as "no roles assigned".
  roles?: string[]
}

export interface CreateUserInput {
  email: string
  fullName: string
  password: string
}

export interface UpdateUserInput {
  email: string
  fullName: string
  roles: string[]
}
