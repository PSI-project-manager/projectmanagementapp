import { useState, type FormEvent } from 'react'
import type { Project } from '../../types/project'

interface ProjectFormValues {
  name: string
  description: string
}

interface ProjectFormProps {
  initialProject?: Project
  onSubmit: (values: ProjectFormValues) => Promise<void>
  onCancel?: () => void
}

export function ProjectForm({ initialProject, onSubmit, onCancel }: ProjectFormProps) {
  const [name, setName] = useState(initialProject?.name ?? '')
  const [description, setDescription] = useState(initialProject?.description ?? '')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const isEditing = initialProject !== undefined

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!name.trim()) {
      setError('Project name is required.')
      return
    }

    setSubmitting(true)
    try {
      await onSubmit({ name, description })
      if (!isEditing) {
        setName('')
        setDescription('')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="project-form">
      <div className="project-form-field">
        <label htmlFor="project-name">Name</label>
        <input
          id="project-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. Website Revamp"
          maxLength={200}
        />
      </div>
      <div className="project-form-field">
        <label htmlFor="project-description">Description</label>
        <textarea
          id="project-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Optional description"
          maxLength={2000}
        />
      </div>
      {error && (
        <p className="project-form-error" role="alert">
          {error}
        </p>
      )}
      <div className="project-form-actions">
        <button type="submit" disabled={submitting}>
          {isEditing ? 'Save changes' : 'Create project'}
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
