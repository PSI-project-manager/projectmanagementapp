import { useEffect, useState } from 'react'
import type { ItemType } from '../types/itemType'
import {
  createItemType,
  listItemTypes,
  updateItemType,
} from '../features/itemTypes/itemTypesApi'
import { ItemTypeForm } from '../features/itemTypes/ItemTypeForm'
import { ItemTypeList } from '../features/itemTypes/ItemTypeList'
import './ItemTypesPage.css'

export default function ItemTypesPage() {
  const [itemTypes, setItemTypes] = useState<ItemType[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [editingItemType, setEditingItemType] = useState<ItemType | null>(null)

  useEffect(() => {
    let cancelled = false
    listItemTypes()
      .then((data) => {
        if (!cancelled) setItemTypes(data)
      })
      .catch((err) => {
        if (!cancelled) {
          setLoadError(err instanceof Error ? err.message : 'Failed to load item types.')
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const reload = async () => {
    setItemTypes(await listItemTypes())
  }

  const handleCreate = async (values: { name: string; isActive: boolean }) => {
    await createItemType({ name: values.name })
    await reload()
  }

  const handleUpdate = async (values: { name: string; isActive: boolean }) => {
    if (!editingItemType) return
    await updateItemType(editingItemType.id, values)
    await reload()
    setEditingItemType(null)
  }

  const handleToggleActive = async (itemType: ItemType) => {
    setActionError(null)
    try {
      await updateItemType(itemType.id, {
        name: itemType.name,
        isActive: !itemType.isActive,
      })
      await reload()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Something went wrong.')
    }
  }

  if (loading) {
    return <p>Loading item types...</p>
  }

  return (
    <section className="item-types-page">
      <h2>Item types</h2>
      {loadError && (
        <p className="item-type-form-error" role="alert">
          {loadError}
        </p>
      )}
      <ItemTypeForm
        key={editingItemType?.id ?? 'create'}
        initialItemType={editingItemType ?? undefined}
        onSubmit={editingItemType ? handleUpdate : handleCreate}
        onCancel={editingItemType ? () => setEditingItemType(null) : undefined}
      />
      {actionError && (
        <p className="item-type-form-error" role="alert">
          {actionError}
        </p>
      )}
      <ItemTypeList
        itemTypes={itemTypes}
        onEdit={setEditingItemType}
        onToggleActive={handleToggleActive}
      />
    </section>
  )
}