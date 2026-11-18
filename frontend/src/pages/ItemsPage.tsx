import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { CreateItemInput, Item, ProjectOption } from '../types/item'
import { isForbidden } from '../services/apiClient'
import PermissionDeniedPage from './PermissionDeniedPage'
import type { ItemType } from '../types/itemType'
import { createItem, listItems, listProjectOptions } from '../features/items/itemsApi'
import { listItemTypes } from '../features/itemTypes/itemTypesApi'
import { ItemForm } from '../features/items/ItemForm'
import { ItemList } from '../features/items/ItemList'
import type { Status } from '../types/status'
import { listStatuses } from '../features/statuses/statusesApi'

export default function ItemsPage() {
  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [itemTypes, setItemTypes] = useState<ItemType[]>([])
  const [statuses, setStatuses] = useState<Status[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [forbidden, setForbidden] = useState(false)
  const [created, setCreated] = useState<{ title: string; projectId: number } | null>(null)


  const [selectedProjectId, setSelectedProjectId] = useState<number | null>(null)
  const [items, setItems] = useState<Item[]>([])
  const [listLoading, setListLoading] = useState(false)
  const [listError, setListError] = useState<string | null>(null)

  // Load the dropdown data once, when the page first appears.
  useEffect(() => {
    let cancelled = false
    // Only active types can be assigned to new items.
    Promise.all([listProjectOptions(), listItemTypes({ activeOnly: true }), listStatuses()])
      .then(([projectData, typeData, statusData]) => {
        if (!cancelled) {
          setProjects(projectData)
          setItemTypes(typeData)
          setStatuses(statusData)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          // a 403 means the account lacks the role, not that the request was bad
          if (isForbidden(err)) {
            setForbidden(true)
          } else {
            setLoadError(err instanceof Error ? err.message : 'Failed to load form data.')
          }
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  // Load the item list whenever the selected project changes.
  useEffect(() => {
    if (selectedProjectId === null) return
    let cancelled = false
    listItems(selectedProjectId)
      .then((data) => {
        if (!cancelled) setItems(data)
      })
      .catch((err) => {
        if (!cancelled) {
          setListError(err instanceof Error ? err.message : 'Failed to load items.')
        }
      })
      .finally(() => {
        if (!cancelled) setListLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [selectedProjectId])

  const handleProjectChange = (projectId: number | null) => {
    setItems([])
    setListError(null)
    setListLoading(projectId !== null)
    setSelectedProjectId(projectId)
  }

  const handleCreate = async (values: CreateItemInput) => {
    const createdItem = await createItem(values)
    setCreated({ title: createdItem.title, projectId: createdItem.projectId })

    // Re-read the list from the server when the new item belongs to the project being
    // displayed, so it shows up with the same data (type/status names) as the others.
    if (createdItem.projectId === selectedProjectId) {
      try {
        setItems(await listItems(createdItem.projectId))
        setListError(null)
      } catch (err) {
        // the item itself was created, so this is a list problem, not a form error
        setListError(err instanceof Error ? err.message : 'Failed to refresh items.')
      }
    }
  }

  if (loading) {
    return <p>Loading...</p>
  }

  if (forbidden) {
    return <PermissionDeniedPage />
  }

  return (
    <section className="items-page">
      <h2>Create item</h2>
      {loadError && (
        <p className="item-form-error" role="alert">
          {loadError}
        </p>
      )}
      {created && (
        <p role="status">
          Item "{created.title}" created.{' '}
          <Link to={`/items?projectId=${created.projectId}`}>View project items</Link>
        </p>
      )}
      <ItemForm
        projects={projects}
        itemTypes={itemTypes}
        statuses={statuses.filter((s) => s.isActive)}
        onSubmit={handleCreate}
        onProjectChange={handleProjectChange}
      />

      {selectedProjectId !== null && (
        <>
          <h3>Items in this project</h3>
          {listError && (
            <p className="item-form-error" role="alert">
              {listError}
            </p>
          )}
          {listLoading ? <p>Loading items...</p> : <ItemList items={items} itemTypes={itemTypes} statuses={statuses} />}
        </>
      )}
    </section>
  )
}