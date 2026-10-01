import { useEffect, useState } from 'react'
import type { CreateItemInput, ProjectOption } from '../types/item'
import type { ItemType } from '../types/itemType'
import { createItem, listProjectOptions } from '../features/items/itemsApi'
import { listItemTypes } from '../features/itemTypes/itemTypesApi'
import { ItemForm } from '../features/items/ItemForm'

export default function ItemsPage() {
  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [itemTypes, setItemTypes] = useState<ItemType[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [createdTitle, setCreatedTitle] = useState<string | null>(null)

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

  const handleCreate = async (values: CreateItemInput) => {
    const created = await createItem(values)
    setCreatedTitle(created.title)
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
      <ItemForm projects={projects} itemTypes={itemTypes} onSubmit={handleCreate} />
    </section>
  )
}