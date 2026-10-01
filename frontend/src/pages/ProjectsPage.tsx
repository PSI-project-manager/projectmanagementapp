import { useEffect, useState } from 'react'
import type { Project } from '../types/project'
import { createProject, listProjects, updateProject } from '../features/projects/projectsApi'
import { ProjectForm } from '../features/projects/ProjectForm'
import { ProjectList } from '../features/projects/ProjectList'
import './ProjectsPage.css'

/**
 * Projects screen (list + create/edit form). The list only contains the projects the
 * current user is authorized for - that scoping is enforced by the backend.
 */
export default function ProjectsPage() {
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [editingProject, setEditingProject] = useState<Project | null>(null)

  useEffect(() => {
    let cancelled = false
    listProjects()
      .then((data) => {
        if (!cancelled) setProjects(data)
      })
      .catch((err) => {
        if (!cancelled) {
          setLoadError(err instanceof Error ? err.message : 'Failed to load projects.')
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const handleCreate = async (values: { name: string; description: string }) => {
    const project = await createProject(values)
    setProjects((current) => [...current, project])
  }

  const handleUpdate = async (values: { name: string; description: string }) => {
    if (!editingProject) return
    const updated = await updateProject(editingProject.id, values)
    setProjects((current) => current.map((project) => (project.id === updated.id ? updated : project)))
    setEditingProject(null)
  }

  if (loading) {
    return <p>Loading projects...</p>
  }

  if (loadError) {
    return <p role="alert">{loadError}</p>
  }

  return (
    <section className="projects-page">
      <h2>Projects</h2>
      <ProjectForm
        key={editingProject?.id ?? 'create'}
        initialProject={editingProject ?? undefined}
        onSubmit={editingProject ? handleUpdate : handleCreate}
        onCancel={editingProject ? () => setEditingProject(null) : undefined}
      />
      <ProjectList projects={projects} onEdit={setEditingProject} />
    </section>
  )
}