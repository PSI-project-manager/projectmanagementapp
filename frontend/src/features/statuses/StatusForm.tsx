import { useState, type FormEvent } from 'react'
import type { Status } from '../../types/status'

interface StatusFormValues {
  name: string
  sortOrder: number
  isActive: boolean
}

interface StatusFormProps {
  initialStatus?: Status
  onSubmit: (values: StatusFormValues) => Promise<void>
  onCancel?: () => void
}

export function StatusForm({ initialStatus, onSubmit, onCancel }: StatusFormProps) {
  const [name, setName] = useState(initialStatus?.name ?? '')
  const [sortOrder, setSortOrder] = useState(String(initialStatus?.sortOrder ?? 0))
  const [isActive, setIsActive] = useState(initialStatus?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const isEditing = initialStatus !== undefined

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!name.trim()) {
      setError('Status name is required.')
      return
    }

    const order = Number(sortOrder)
    if (isEditing && (!Number.isInteger(order) || order < 0)) {
      setError('Sort order must be a whole number, 0 or greater.')
      return
    }

    setSubmitting(true)
    try {
      await onSubmit({ name, sortOrder: order, isActive })
      if (!isEditing) {
        setName('')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="status-form">
      <div className="status-form-field">
        <label htmlFor="status-name">Name</label>
        <input
          id="status-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. In Progress"
          maxLength={100}
        />
      </div>
      {isEditing && (
        <>
          <div className="status-form-field">
            <label htmlFor="status-sort-order">Sort order</label>
            <input
              id="status-sort-order"
              type="number"
              min={0}
              step={1}
              value={sortOrder}
              onChange={(event) => setSortOrder(event.target.value)}
            />
          </div>
          <label className="status-form-checkbox">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(event) => setIsActive(event.target.checked)}
            />
            Active (selectable for new items)
          </label>
        </>
      )}
      {error && (
        <p className="status-form-error" role="alert">
          {error}
        </p>
      )}
      <div className="status-form-actions">
        <button type="submit" disabled={submitting}>
          {isEditing ? 'Save changes' : 'Create status'}
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