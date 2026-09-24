import { useState, type FormEvent } from 'react'
import type { User } from '../../types/user'

interface UserFormValues {
  email: string
  fullName: string
  password: string
}

interface UserFormProps {
  initialUser?: User
  onSubmit: (values: UserFormValues) => Promise<void>
  onCancel?: () => void
}

export function UserForm({ initialUser, onSubmit, onCancel }: UserFormProps) {
  const [email, setEmail] = useState(initialUser?.email ?? '')
  const [fullName, setFullName] = useState(initialUser?.fullName ?? '')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const isEditing = initialUser !== undefined

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!email.trim()) {
      setError('Email is required.')
      return
    }

    if (!fullName.trim()) {
      setError('Full name is required.')
      return
    }

    if (!isEditing && password.length < 8) {
      setError('Password must be at least 8 characters long.')
      return
    }

    setSubmitting(true)
    try {
      await onSubmit({ email, fullName, password })
      if (!isEditing) {
        setEmail('')
        setFullName('')
        setPassword('')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="user-form">
      <div className="user-form-field">
        <label htmlFor="user-email">Email</label>
        <input
          id="user-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          placeholder="e.g. jane@example.com"
          maxLength={255}
        />
      </div>
      <div className="user-form-field">
        <label htmlFor="user-full-name">Full name</label>
        <input
          id="user-full-name"
          value={fullName}
          onChange={(event) => setFullName(event.target.value)}
          placeholder="e.g. Jane Doe"
          maxLength={150}
        />
      </div>
      {/* Editing doesn't change the password - the backend has no reset endpoint yet. */}
      {!isEditing && (
        <div className="user-form-field">
          <label htmlFor="user-password">Password</label>
          <input
            id="user-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="At least 8 characters"
          />
        </div>
      )}
      {error && (
        <p className="user-form-error" role="alert">
          {error}
        </p>
      )}
      <div className="user-form-actions">
        <button type="submit" disabled={submitting}>
          {isEditing ? 'Save changes' : 'Create user'}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} disabled={submitting}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
