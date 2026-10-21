import { useEffect, useState } from 'react'
import type { CreateItemInput, Item, ProjectOption } from '../types/item'
import type { ItemType } from '../types/itemType'
import { createItem, listItems, listProjectOptions } from '../features/items/itemsApi'
import { listItemTypes } from '../features/itemTypes/itemTypesApi'
import { ItemForm } from '../features/items/ItemForm'
import { ItemList } from '../features/items/ItemList'

export default function ItemsPage() {
  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [itemTypes, setItemTypes] = useState<ItemType[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [createdTitle, setCreatedTitle] = useState<string | null>(null)

  const [selectedProjectId, setSelectedProjectId] = useState<number | null>(null)
  const [items, setItems] = useState<Item[]>([])
  const [listLoading, setListLoading] = useState(false)
  const [listError, setListError] = useState<string | null>(null)

  // Load the dropdown data once, when the page first appears.
  useEffect(() => {
    let cancelled = false
    // Only active types can be assigned to new items.
    Promise.all([listProjectOptions(), listItemTypes({ activeOnly: true })])
      .then(([projectData, typeData]) => {
        if (!cancelled) {
          setProjects(projectData)
          setItemTypes(typeData)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setLoadError(err instanceof Error ? err.message : 'Failed to load form data.')
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
    const created = await createItem(values)
    setCreatedTitle(created.title)
    // Show the new item straight away if it belongs to the project being displayed.
    if (created.projectId === selectedProjectId) {
      setItems((current) => [created, ...current])
    }
  }

  if (loading) {
    return <p>Loading...</p>
  }

  return (
    <section className="items-page">
      <h2>Create item</h2>
      {loadError && (
        <p className="item-form-error" role="alert">
          {loadError}
        </p>
      )}
      {createdTitle && <p role="status">Item "{createdTitle}" created.</p>}
      <ItemForm
        projects={projects}
        itemTypes={itemTypes}
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
          {listLoading ? <p>Loading items...</p> : <ItemList items={items} itemTypes={itemTypes} />}
        </>
      )}
    </section>
  )
}