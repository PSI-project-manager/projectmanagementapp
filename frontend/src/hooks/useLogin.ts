import { useState } from 'react'
import { authService, type LoginRequest } from '../services/authService'

export const useLogin = () => {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const login = async (credentials: LoginRequest) => {
    setIsLoading(true)
    setError(null)

    try {
      const data = await authService.login(credentials)
      localStorage.setItem('token', data.token)
      localStorage.setItem('userId', JSON.stringify(data.userId))
      return data
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Login failed'
      setError(message)
      throw err
    } finally {
      setIsLoading(false)
    }
  }

  return { login, isLoading, error }
}
