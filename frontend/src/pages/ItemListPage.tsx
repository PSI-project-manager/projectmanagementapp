import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import type { Item, ProjectOption } from '../types/item'
import type { ItemType } from '../types/itemType'
import type { Status } from '../types/status'
import { isForbidden } from '../services/apiClient'
import PermissionDeniedPage from './PermissionDeniedPage'
import { listItems, listProjectOptions } from '../features/items/itemsApi'
import { listItemTypes } from '../features/itemTypes/itemTypesApi'
import { listStatuses } from '../features/statuses/statusesApi'
import { ItemList } from '../features/items/ItemList'

// The result of one items request, tagged with the request it belongs to.
// "Loading" is derived by comparing this tag with the current request, so
// the effect below never has to call setState synchronously.
interface ItemsResult {
  key: string
  items: Item[]
  error: string | null
}

type FilterParam = 'projectId' | 'itemTypeId' | 'statusId'

const parseId = (raw: string | null) => (raw && /^\d+$/.test(raw) ? Number(raw) : null)

// A select shows its placeholder when the id from the URL is not among the options.
const selectValue = (id: number | null, options: { id: number }[]) =>
  id !== null && options.some((option) => option.id === id) ? String(id) : ''

export default function ItemListPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [itemTypes, setItemTypes] = useState<ItemType[]>([])
  const [statuses, setStatuses] = useState<Status[]>([])
  const [optionsLoading, setOptionsLoading] = useState(true)
  const [optionsError, setOptionsError] = useState<string | null>(null)
  const [forbidden, setForbidden] = useState(false)

  const [refreshCount, setRefreshCount] = useState(0)
  const [result, setResult] = useState<ItemsResult | null>(null)

  // The filters live in the URL (?projectId=3&statusId=1) so reloads and links keep them.
  const projectId = parseId(searchParams.get('projectId'))
  const itemTypeId = parseId(searchParams.get('itemTypeId'))
  const statusId = parseId(searchParams.get('statusId'))
  const hasFilters = projectId !== null || itemTypeId !== null || statusId !== null
  const requestKey = `${projectId ?? 'all'}:${itemTypeId ?? 'all'}:${statusId ?? 'all'}:${refreshCount}`

  // Dropdown data: projects the user can access (the backend already filters them),
  // item types and statuses.
  useEffect(() => {
    let cancelled = false
    Promise.all([listProjectOptions(), listItemTypes(), listStatuses()])
      .then(([projectData, typeData, statusData]) => {
        if (!cancelled) {
          setProjects(projectData)
          setItemTypes(typeData)
          setStatuses(statusData)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          if (isForbidden(err)) {
            setForbidden(true)
          } else {
            setOptionsError(err instanceof Error ? err.message : 'Failed to load filters.')
          }
        }
      })
      .finally(() => {
        if (!cancelled) setOptionsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  // Items matching the current filters. Runs again on filter change and on Refresh.
  // The backend only ever returns items from projects the user can access, and answers
  // 404 for an explicitly requested project the user has no access to.
  useEffect(() => {
    let cancelled = false
    listItems({ projectId, itemTypeId, statusId })
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
  }, [projectId, itemTypeId, statusId, requestKey])

  const updateFilter = (name: FilterParam, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) {
      next.set(name, value)
    } else {
      next.delete(name)
    }
    setSearchParams(next)
  }

  const clearFilters = () => setSearchParams({})

  if (optionsLoading) {
    return <p>Loading...</p>
  }

  if (forbidden) {
    return <PermissionDeniedPage />
  }

  const itemsLoading = result?.key !== requestKey
  const current = result?.key === requestKey ? result : null
  const emptyMessage = hasFilters
    ? 'No items match the selected filters.'
    : 'No items to show yet.'

  return (
    <section className="items-page">
      <h2>Items</h2>

      {optionsError && (
        <p className="item-form-error" role="alert">
          {optionsError}
        </p>
      )}

      <div className="items-toolbar">
        <div className="items-toolbar-field">
          <label htmlFor="items-project">Project</label>
          <select
            id="items-project"
            value={selectValue(projectId, projects)}
            onChange={(event) => updateFilter('projectId', event.target.value)}
          >
            <option value="">All projects</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </select>
        </div>
        <div className="items-toolbar-field">
          <label htmlFor="items-type">Type</label>
          <select
            id="items-type"
            value={selectValue(itemTypeId, itemTypes)}
            onChange={(event) => updateFilter('itemTypeId', event.target.value)}
          >
            <option value="">All types</option>
            {itemTypes.map((itemType) => (
              <option key={itemType.id} value={itemType.id}>
                {itemType.name}
                {itemType.isActive ? '' : ' (inactive)'}
              </option>
            ))}
          </select>
        </div>
        <div className="items-toolbar-field">
          <label htmlFor="items-status">Status</label>
          <select
            id="items-status"
            value={selectValue(statusId, statuses)}
            onChange={(event) => updateFilter('statusId', event.target.value)}
          >
            <option value="">All statuses</option>
            {statuses.map((status) => (
              <option key={status.id} value={status.id}>
                {status.name}
              </option>
            ))}
          </select>
        </div>
        <div className="items-toolbar-actions">
          <button type="button" onClick={clearFilters} disabled={!hasFilters}>
            Clear filters
          </button>
          <button
            type="button"
            onClick={() => setRefreshCount((count) => count + 1)}
            disabled={itemsLoading}
          >
            Refresh
          </button>
          <Link className="link-button" to="/items/new">
            Create item
          </Link>
        </div>
      </div>

      {projects.length === 0 && !optionsError && (
        <p className="item-list-empty">
          No projects to show. Projects you have access to will appear here.
        </p>
      )}

      {itemsLoading && <p>Loading items...</p>}

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
          <ItemList
            items={current.items}
            showProject={projectId === null}
            emptyMessage={emptyMessage}
          />
        </>
      )}
    </section>
  )
}