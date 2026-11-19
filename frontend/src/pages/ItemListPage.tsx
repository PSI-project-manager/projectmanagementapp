import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import type { Item, ProjectOption } from '../types/item'
import { isForbidden } from '../services/apiClient'
import PermissionDeniedPage from './PermissionDeniedPage'
import { listItems, listProjectOptions } from '../features/items/itemsApi'
import { ItemList } from '../features/items/ItemList'

// The result of one items request, tagged with the request it belongs to.
// "Loading" is derived by comparing this tag with the current request, so
// the effect below never has to call setState synchronously.
interface ItemsResult {
  key: string
  items: Item[]
  error: string | null
}

export default function ItemListPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [projectsLoading, setProjectsLoading] = useState(true)
  const [projectsError, setProjectsError] = useState<string | null>(null)
  const [forbidden, setForbidden] = useState(false)

  const [refreshCount, setRefreshCount] = useState(0)
  const [result, setResult] = useState<ItemsResult | null>(null)

  // The selected project lives in the URL (?projectId=3) so reloads and links keep it.
  const rawProjectId = searchParams.get('projectId')
  const projectId = rawProjectId && /^\d+$/.test(rawProjectId) ? Number(rawProjectId) : null
  const requestKey = projectId === null ? null : `${projectId}:${refreshCount}`

  // Projects the user can access (the backend already filters them).
  useEffect(() => {
    let cancelled = false
    listProjectOptions()
      .then((data) => {
        if (!cancelled) setProjects(data)
      })
      .catch((err) => {
        if (!cancelled) {
          if (isForbidden(err)) {
            setForbidden(true)
          } else {
            setProjectsError(err instanceof Error ? err.message : 'Failed to load projects.')
          }
        }
      })
      .finally(() => {
        if (!cancelled) setProjectsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  // Items of the selected project. Runs again on project change and on Refresh.
  // The backend answers 404 for a project the user has no access to, which is
  // shown as an error and no items are displayed.
  useEffect(() => {
    if (projectId === null || requestKey === null) return
    let cancelled = false
    listItems(projectId)
      .then((data) => {
        if (!cancelled) setResult({ key: requestKey, items: data, error: null })
      })
      .catch((err) => {
        if (!cancelled) {
          setResult({
            key: requestKey,
            items: [],
            error: err instanceof Error ? err.message : 'Failed to load items.',
          })
        }
      })
    return () => {
      cancelled = true
    }
  }, [projectId, requestKey])

  const handleProjectChange = (value: string) => {
    setSearchParams(value ? { projectId: value } : {})
  }

  if (projectsLoading) {
    return <p>Loading...</p>
  }

  if (forbidden) {
    return <PermissionDeniedPage />
  }

  const selectedValue =
    projectId !== null && projects.some((p) => p.id === projectId) ? String(projectId) : ''
  const itemsLoading = requestKey !== null && result?.key !== requestKey
  const current = result?.key === requestKey ? result : null

  return (
    <section className="items-page">
      <h2>Items</h2>

      {projectsError && (
        <p className="item-form-error" role="alert">
          {projectsError}
        </p>
      )}

      <div className="items-toolbar">
        <div className="items-toolbar-field">
          <label htmlFor="items-project">Project</label>
          <select
            id="items-project"
            value={selectedValue}
            onChange={(event) => handleProjectChange(event.target.value)}
          >
            <option value="">Select a project</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </select>
        </div>
        <div className="items-toolbar-actions">
          <button
            type="button"
            onClick={() => setRefreshCount((count) => count + 1)}
            disabled={projectId === null || itemsLoading}
          >
            Refresh
          </button>
          <Link className="link-button" to="/items/new">
            Create item
          </Link>
        </div>
      </div>

      {projects.length === 0 && !projectsError && (
        <p className="item-list-empty">
          No projects to show. Projects you have access to will appear here.
        </p>
      )}

      {projects.length > 0 && projectId === null && (
        <p className="item-list-empty">Select a project to see its items.</p>
      )}

      {projectId !== null && itemsLoading && <p>Loading items...</p>}

      {current?.error && (
        <p className="item-form-error" role="alert">
          {current.error}
        </p>
      )}

      {current && !current.error && (
        <>
          <p className="items-count">
            {current.items.length} {current.items.length === 1 ? 'item' : 'items'}
          </p>
          <ItemList items={current.items} />
        </>
      )}
    </section>
  )
}