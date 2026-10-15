import type { ChangeEvent } from 'react'
import { AVAILABLE_ROLES } from './roles'

interface RoleEditorProps {
  roles: string[]
  onChange: (roles: string[]) => void
  disabled?: boolean
}

export function RoleEditor({ roles, onChange, disabled }: RoleEditorProps) {
  const remaining = AVAILABLE_ROLES.filter((role) => !roles.includes(role))

  // The select is controlled on '' so it always falls back to its placeholder
  // after a pick, which lets the same option be re-added once removed.
  const handleAdd = (event: ChangeEvent<HTMLSelectElement>) => {
    const role = event.target.value
    if (role) {
      onChange([...roles, role])
    }
  }

  const handleRemove = (role: string) => {
    onChange(roles.filter((current) => current !== role))
  }

  return (
    <div className="user-form-field">
      <label htmlFor="user-roles">Roles</label>
      {roles.length === 0 ? (
        <p className="role-chips-empty">No roles assigned.</p>
      ) : (
        <ul className="role-chips">
          {roles.map((role) => (
            <li key={role} className="role-chip">
              <span>{role}</span>
              <button
                type="button"
                className="role-chip-remove"
                onClick={() => handleRemove(role)}
                disabled={disabled}
                aria-label={`Remove ${role} role`}
              >
                &times;
              </button>
            </li>
          ))}
        </ul>
      )}
      <select
        id="user-roles"
        value=""
        onChange={handleAdd}
        disabled={disabled || remaining.length === 0}
      >
        <option value="">
          {remaining.length === 0 ? 'All roles assigned' : 'Add a role...'}
        </option>
        {remaining.map((role) => (
          <option key={role} value={role}>
            {role}
          </option>
        ))}
      </select>
    </div>
  )
}
