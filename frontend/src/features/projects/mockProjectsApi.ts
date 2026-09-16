import type { CreateProjectInput, Project, UpdateProjectInput } from '../../types/project'

/**
 * Stand-in for the real projects API until the backend endpoints exist. Shaped the same
 * way a fetch-based client would be (async, throws on invalid input) so swapping this
 * module out later doesn't require touching the components that call it.
 */

const CURRENT_ORGANIZATION_ID = 'org-demo-1'

let projects: Project[] = [
  {
    id: 'project-1',
    organizationId: CURRENT_ORGANIZATION_ID,
    name: 'Website Revamp',
    description: 'Redesign the public marketing site.',
    createdAt: new Date('2026-01-10').toISOString(),
    updatedAt: new Date('2026-01-10').toISOString(),
  },
  {
    id: 'project-2',
    organizationId: CURRENT_ORGANIZATION_ID,
    name: 'Mobile App Launch',
    description: 'Ship v1 of the companion mobile app.',
    createdAt: new Date('2026-02-01').toISOString(),
    updatedAt: new Date('2026-02-01').toISOString(),
  },
]

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), 150))
}

export async function listProjects(): Promise<Project[]> {
  return delay([...projects])
}

export async function createProject(input: CreateProjectInput): Promise<Project> {
  const name = input.name.trim()
  if (!name) {
    throw new Error('Project name is required.')
  }

  const now = new Date().toISOString()
  const project: Project = {
    id: crypto.randomUUID(),
    organizationId: CURRENT_ORGANIZATION_ID,
    name,
    description: input.description?.trim() || undefined,
    createdAt: now,
    updatedAt: now,
  }

  projects = [...projects, project]
  return delay(project)
}

export async function updateProject(id: string, input: UpdateProjectInput): Promise<Project> {
  const name = input.name.trim()
  if (!name) {
    throw new Error('Project name is required.')
  }

  const existing = projects.find((project) => project.id === id)
  if (!existing) {
    throw new Error(`Project "${id}" was not found.`)
  }

  const updated: Project = {
    ...existing,
    name,
    description: input.description?.trim() || undefined,
    updatedAt: new Date().toISOString(),
  }

  projects = projects.map((project) => (project.id === id ? updated : project))
  return delay(updated)
}
