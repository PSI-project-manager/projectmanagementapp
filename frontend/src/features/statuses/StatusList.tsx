import type { Status } from '../../types/status'

interface StatusListProps {
  statuses: Status[]
  onEdit: (status: Status) => void
  onToggleActive: (status: Status) => void
}

export function StatusList({ statuses, onEdit, onToggleActive }: StatusListProps) {
  if (statuses.length === 0) {
    return <p className="status-list-empty">No statuses yet. Create one to get started.</p>
  }

  return (
    <ul className="status-list">
      {statuses.map((status) => (
        <li key={status.id} className="status-list-item">
          <div className="status-list-name">
            <span>{status.name}</span>
            <span className="status-badge">#{status.sortOrder}</span>
            {!status.isActive && <span className="status-badge">Inactive</span>}
          </div>
          <div className="status-list-actions">
            <button type="button" onClick={() => onEdit(status)}>
              Edit
            </button>
            <button type="button" onClick={() => onToggleActive(status)}>
              {status.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}