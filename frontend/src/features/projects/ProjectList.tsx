import type { Project } from '../../types/project'

interface ProjectListProps {
  projects: Project[]
  onEdit: (project: Project) => void
}

export function ProjectList({ projects, onEdit }: ProjectListProps) {
  if (projects.length === 0) {
    return <p className="project-list-empty">No projects yet. Create one to get started.</p>
  }

  return (
    <ul className="project-list">
      {projects.map((project) => (
        <li key={project.id} className="project-list-item">
          <div>
            <h3>{project.name}</h3>
            {project.description && <p>{project.description}</p>}
          </div>
          <button type="button" onClick={() => onEdit(project)}>
            Edit
          </button>
        </li>
      ))}
    </ul>
  )
}
