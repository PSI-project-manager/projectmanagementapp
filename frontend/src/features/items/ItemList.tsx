import type { Item } from '../../types/item'
import type { ItemType } from '../../types/itemType'

interface ItemListProps {
  items: Item[]
  itemTypes?: ItemType[]
  /** Show which project each item belongs to (useful when items of many projects are listed). */
  showProject?: boolean
  emptyMessage?: string
}

export function ItemList({
  items,
  itemTypes = [],
  showProject = false,
  emptyMessage = 'No items in this project yet.',
}: ItemListProps) {
  if (items.length === 0) {
    return <p className="item-list-empty">{emptyMessage}</p>
  }

  const typeName = (item: Item) =>
    item.itemTypeName ??
    itemTypes.find((t) => t.id === item.itemTypeId)?.name ??
    `Type #${item.itemTypeId}`

  const statusName = (item: Item) => item.statusName ?? `Status #${item.statusId}`

  return (
    <ul className="item-list">
      {items.map((item) => (
        <li key={item.id} className="item-list-item">
          <h3>{item.title}</h3>
          <div className="item-list-meta">
            {showProject && item.projectName && <span>{item.projectName} ·</span>}
            <span>{typeName(item)}</span>
            <span className="item-type-badge">{statusName(item)}</span>
          </div>
          {item.description && <p>{item.description}</p>}
        </li>
      ))}
    </ul>
  )
}