import { useState, type FormEvent } from 'react'
import type { ItemType } from '../../types/itemType'

interface ItemTypeFormValues {
  name: string
  isActive: boolean
}

interface ItemTypeFormProps {
  initialItemType?: ItemType
  onSubmit: (values: ItemTypeFormValues) => Promise<void>
  onCancel?: () => void
}

export function ItemTypeForm({ initialItemType, onSubmit, onCancel }: ItemTypeFormProps) {
  const [name, setName] = useState(initialItemType?.name ?? '')
  const [isActive, setIsActive] = useState(initialItemType?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const isEditing = initialItemType !== undefined

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!name.trim()) {
      setError('Item type name is required.')
      return
    }

    setSubmitting(true)
    try {
      await onSubmit({ name, isActive })
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
    <form onSubmit={handleSubmit} className="item-type-form">
      <div className="item-type-form-field">
        <label htmlFor="item-type-name">Name</label>
        <input
          id="item-type-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. Bug"
          maxLength={100}
        />
      </div>
      {isEditing && (
        <label className="item-type-form-checkbox">
          <input
            type="checkbox"
            checked={isActive}
            onChange={(event) => setIsActive(event.target.checked)}
          />
          Active (selectable for new items)
        </label>
      )}
      {error && (
        <p className="item-type-form-error" role="alert">
          {error}
        </p>
      )}
      <div className="item-type-form-actions">
        <button type="submit" disabled={submitting}>
          {isEditing ? 'Save changes' : 'Create item type'}
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