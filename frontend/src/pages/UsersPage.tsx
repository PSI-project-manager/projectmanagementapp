import { useEffect, useState } from 'react'
import type { User } from '../types/user'
import {
  createUser,
  listUsers,
  setUserActive,
  updateUser,
} from '../features/users/usersApi'
import { UserForm } from '../features/users/UserForm'
import { UserList } from '../features/users/UserList'
import './UsersPage.css'

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [editingUser, setEditingUser] = useState<User | null>(null)

  useEffect(() => {
    let cancelled = false
    listUsers()
      .then((data) => {
        if (!cancelled) setUsers(data)
      })
      .catch((err) => {
        if (!cancelled) {
          setLoadError(err instanceof Error ? err.message : 'Failed to load users.')
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const reload = async () => {
    setUsers(await listUsers())
  }

  const handleCreate = async (values: { email: string; fullName: string; password: string }) => {
    await createUser(values)
    await reload()
  }

  const handleUpdate = async (values: { email: string; fullName: string }) => {
    if (!editingUser) return
    await updateUser(editingUser.id, { email: values.email, fullName: values.fullName })
    await reload()
    setEditingUser(null)
  }

  const handleToggleActive = async (user: User) => {
    setActionError(null)
    try {
      await setUserActive(user.id, !user.isActive)
      await reload()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Something went wrong.')
    }
  }

  if (loading) {
    return <p>Loading users...</p>
  }

  return (
    <section className="users-page">
      <h2>Users</h2>
      {loadError && (
        <p className="user-form-error" role="alert">
          {loadError}
        </p>
      )}
      <UserForm
        key={editingUser?.id ?? 'create'}
        initialUser={editingUser ?? undefined}
        onSubmit={editingUser ? handleUpdate : handleCreate}
        onCancel={editingUser ? () => setEditingUser(null) : undefined}
      />
      {actionError && (
        <p className="user-form-error" role="alert">
          {actionError}
        </p>
      )}
      <UserList users={users} onEdit={setEditingUser} onToggleActive={handleToggleActive} />
    </section>
  )
}
