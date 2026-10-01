import type { CreateProjectInput, Project, UpdateProjectInput } from '../../types/project'

const BASE_URL = '/api/projects'

function headers(): HeadersInit {
  const token = localStorage.getItem('token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    // Backend errors are ProblemDetails: { status, title, detail }
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail || problem?.title || 'Request failed.')
  }
  return response.json()
}

// The backend only returns the projects the logged-in user is authorized for
// (admins get all of them), so no filtering is needed here.
export async function listProjects(): Promise<Project[]> {
  const response = await fetch(BASE_URL, { headers: headers() })
  return parse<Project[]>(response)
}

export async function createProject(input: CreateProjectInput): Promise<Project> {
  const createdByUserId = Number(localStorage.getItem('userId'))
  const response = await fetch(BASE_URL, {
    method: 'POST',
    headers: headers(),
    body: JSON.stringify({ ...input, createdByUserId }),
  })
  return parse<Project>(response)
}

export async function updateProject(id: number, input: UpdateProjectInput): Promise<Project> {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: headers(),
    body: JSON.stringify(input),
  })
  return parse<Project>(response)
}