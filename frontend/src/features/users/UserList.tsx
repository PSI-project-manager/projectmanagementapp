import type { User } from '../../types/user'

interface UserListProps {
  users: User[]
  onEdit: (user: User) => void
  onToggleActive: (user: User) => void
}

export function UserList({ users, onEdit, onToggleActive }: UserListProps) {
  if (users.length === 0) {
    return <p className="user-list-empty">No users yet. Create one to get started.</p>
  }

  return (
    <ul className="user-list">
      {users.map((user) => (
        <li key={user.id} className="user-list-item">
          <div className="user-list-identity">
            <span className="user-list-name">
              {user.fullName}
              {!user.isActive && <span className="user-badge">Inactive</span>}
            </span>
            <span className="user-list-email">{user.email}</span>
          </div>
          <div className="user-list-actions">
            <button type="button" onClick={() => onEdit(user)}>
              Edit
            </button>
            <button type="button" onClick={() => onToggleActive(user)}>
              {user.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}
