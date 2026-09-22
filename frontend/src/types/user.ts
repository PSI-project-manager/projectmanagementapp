export interface User {
  id: number
  email: string
  fullName: string
  isActive: boolean
}

export interface CreateUserInput {
  email: string
  fullName: string
  password: string
}

export interface UpdateUserInput {
  email: string
  fullName: string
}
