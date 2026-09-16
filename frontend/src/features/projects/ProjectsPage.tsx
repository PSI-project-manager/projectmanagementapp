import { useEffect, useState } from 'react'
import type { Project } from '../../types/project'
import { createProject, listProjects, updateProject } from './mockProjectsApi'
import { ProjectForm } from './ProjectForm'
import { ProjectList } from './ProjectList'
import './ProjectsPage.css'

/**
 * Self-contained projects screen (list + create/edit form) backed by mock in-memory
 * data. Not wired into the app shell yet since navigation/routing spans other stories -
 * swap `mockProjectsApi` for real HTTP calls once the backend endpoints exist.
 */
export function ProjectsPage() {
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [editingProject, setEditingProject] = useState<Project | null>(null)

  useEffect(() => {
    let cancelled = false
    listProjects().then((data) => {
      if (!cancelled) {
        setProjects(data)
        setLoading(false)
      }
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
