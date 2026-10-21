import { useState, type FormEvent } from 'react'
import type { CreateItemInput, ProjectOption } from '../../types/item'
import type { ItemType } from '../../types/itemType'

interface ItemFormProps {
  projects: ProjectOption[]
  itemTypes: ItemType[]
  onSubmit: (values: CreateItemInput) => Promise<void>
  onProjectChange?: (projectId: number | null) => void
}

type FieldErrors = Partial<
  Record<'title' | 'description' | 'projectId' | 'itemTypeId' | 'statusId', string>
>

export function ItemForm({ projects, itemTypes, onSubmit, onProjectChange }: ItemFormProps) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [projectId, setProjectId] = useState('')
  const [itemTypeId, setItemTypeId] = useState('')
  const [statusId, setStatusId] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Same rules as CreateItemRequestValidator on the backend.
  const validate = (): FieldErrors => {
    const errors: FieldErrors = {}
    if (!title.trim()) errors.title = 'Title is required.'
    if (!description.trim()) errors.description = 'Description is required.'
    if (!projectId) errors.projectId = 'Project is required.'
    if (!itemTypeId) errors.itemTypeId = 'Item type is required.'
    if (!(Number(statusId) > 0)) errors.statusId = 'Status is required.'
    return errors
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    const errors = validate()
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) {
      return // blocked: nothing is sent to the backend
    }

    setSubmitting(true)
    try {
      await onSubmit({
        title,
        description,
        projectId: Number(projectId),
        itemTypeId: Number(itemTypeId),
        statusId: Number(statusId),
      })
      setTitle('')
      setDescription('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="item-form">
      <div className="item-form-field">
        <label htmlFor="item-title">Title</label>
        <input
          id="item-title"
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          maxLength={200}
        />
        {fieldErrors.title && <p className="item-form-error">{fieldErrors.title}</p>}
      </div>
      <div className="item-form-field">
        <label htmlFor="item-description">Description</label>
        <textarea
          id="item-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
        />
        {fieldErrors.description && <p className="item-form-error">{fieldErrors.description}</p>}
      </div>
      <div className="item-form-field">
        <label htmlFor="item-project">Project</label>
        <select
          id="item-project"
          value={projectId}
          onChange={(event) => {
            setProjectId(event.target.value)
            onProjectChange?.(event.target.value ? Number(event.target.value) : null)
          }}
        >
          <option value="">Select a project</option>
          {projects.map((project) => (
            <option key={project.id} value={project.id}>
              {project.name}
            </option>
          ))}
        </select>
        {fieldErrors.projectId && <p className="item-form-error">{fieldErrors.projectId}</p>}
      </div>
      <div className="item-form-field">
        <label htmlFor="item-type">Type</label>
        <select
          id="item-type"
          value={itemTypeId}
          onChange={(event) => setItemTypeId(event.target.value)}
        >
          <option value="">Select a type</option>
          {itemTypes.map((itemType) => (
            <option key={itemType.id} value={itemType.id}>
              {itemType.name}
            </option>
          ))}
        </select>
        {fieldErrors.itemTypeId && <p className="item-form-error">{fieldErrors.itemTypeId}</p>}
      </div>
      {/* TODO (US-08): replace with a status dropdown once there is a statuses endpoint. */}
      <div className="item-form-field">
        <label htmlFor="item-status">Status ID</label>
        <input
          id="item-status"
          type="number"
          value={statusId}
          onChange={(event) => setStatusId(event.target.value)}
        />
        {fieldErrors.statusId && <p className="item-form-error">{fieldErrors.statusId}</p>}
      </div>
      {error && (
        <p className="item-form-error" role="alert">
          {error}
        </p>
      )}
      <div className="item-form-actions">
        <button type="submit" disabled={submitting}>
          Create item
        </button>
      </div>
    </form>
  )
}