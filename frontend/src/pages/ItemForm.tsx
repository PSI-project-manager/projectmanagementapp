import { useState, type FormEvent } from 'react'
import type { CreateItemInput, ProjectOption } from '../../types/item'
import type { ItemType } from '../../types/itemType'

interface ItemFormProps {
  projects: ProjectOption[]
  itemTypes: ItemType[]
  onSubmit: (values: CreateItemInput) => Promise<void>
}

export function ItemForm({ projects, itemTypes, onSubmit }: ItemFormProps) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [projectId, setProjectId] = useState('')
  const [itemTypeId, setItemTypeId] = useState('')
  const [statusId, setStatusId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
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
      </div>
      <div className="item-form-field">
        <label htmlFor="item-description">Description</label>
        <textarea
          id="item-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
        />
      </div>
      <div className="item-form-field">
        <label htmlFor="item-project">Project</label>
        <select
          id="item-project"
          value={projectId}
          onChange={(event) => setProjectId(event.target.value)}
        >
          <option value="">Select a project</option>
          {projects.map((project) => (
            <option key={project.id} value={project.id}>
              {project.name}
            </option>
          ))}
        </select>
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