import { useEffect, useState } from 'react'
import { isForbidden } from '../services/apiClient'
import PermissionDeniedPage from './PermissionDeniedPage'
import type { Status } from '../types/status'
import { createStatus, listStatuses, updateStatus } from '../features/statuses/statusesApi'
import { StatusForm } from '../features/statuses/StatusForm'
import { StatusList } from '../features/statuses/StatusList'

export default function StatusesPage() {
  const [statuses, setStatuses] = useState<Status[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [forbidden, setForbidden] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [editingStatus, setEditingStatus] = useState<Status | null>(null)

  useEffect(() => {
    let cancelled = false
    listStatuses()
      .then((data) => {
        if (!cancelled) setStatuses(data)
      })
      .catch((err) => {
        if (!cancelled) {
          if (isForbidden(err)) {
            setForbidden(true)
          } else {
            setLoadError(err instanceof Error ? err.message : 'Failed to load statuses.')
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

  const reload = async () => {
    setStatuses(await listStatuses())
  }

  const handleCreate = async (values: { name: string }) => {
    await createStatus({ name: values.name })
    await reload()
  }

  const handleUpdate = async (values: { name: string; sortOrder: number; isActive: boolean }) => {
    if (!editingStatus) return
    await updateStatus(editingStatus.id, values)
    await reload()
    setEditingStatus(null)
  }

  const handleToggleActive = async (status: Status) => {
    setActionError(null)
    try {
      await updateStatus(status.id, {
        name: status.name,
        sortOrder: status.sortOrder,
        isActive: !status.isActive,
      })
      await reload()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Something went wrong.')
    }
  }

  if (loading) {
    return <p>Loading statuses...</p>
  }

  if (forbidden) {
    return <PermissionDeniedPage />
  }

  return (
    <section className="statuses-page">
      <h2>Statuses</h2>
      {loadError && (
        <p className="status-form-error" role="alert">
          {loadError}
        </p>
      )}
      <StatusForm
        key={editingStatus?.id ?? 'create'}
        initialStatus={editingStatus ?? undefined}
        onSubmit={editingStatus ? handleUpdate : handleCreate}
        onCancel={editingStatus ? () => setEditingStatus(null) : undefined}
      />
      {actionError && (
        <p className="status-form-error" role="alert">
          {actionError}
        </p>
      )}
      <StatusList
        statuses={statuses}
        onEdit={setEditingStatus}
        onToggleActive={handleToggleActive}
      />
    </section>
  )
}