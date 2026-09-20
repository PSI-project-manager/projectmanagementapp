import type { ItemType } from '../../types/itemType'

interface ItemTypeListProps {
  itemTypes: ItemType[]
  onEdit: (itemType: ItemType) => void
  onToggleActive: (itemType: ItemType) => void
}

export function ItemTypeList({ itemTypes, onEdit, onToggleActive }: ItemTypeListProps) {
  if (itemTypes.length === 0) {
    return <p className="item-type-list-empty">No item types yet. Create one to get started.</p>
  }

  return (
    <ul className="item-type-list">
      {itemTypes.map((itemType) => (
        <li key={itemType.id} className="item-type-list-item">
          <div className="item-type-list-name">
            <span>{itemType.name}</span>
            {!itemType.isActive && <span className="item-type-badge">Inactive</span>}
          </div>
          <div className="item-type-list-actions">
            <button type="button" onClick={() => onEdit(itemType)}>
              Edit
            </button>
            <button type="button" onClick={() => onToggleActive(itemType)}>
              {itemType.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}