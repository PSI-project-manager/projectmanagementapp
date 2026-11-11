import type { Item } from '../../types/item'
import type { ItemType } from '../../types/itemType'

interface ItemListProps {
  items: Item[]
  itemTypes: ItemType[]
  statuses: Status[]
}

export function ItemList({ items, itemTypes, statuses }: ItemListProps) {
  if (items.length === 0) {
    return <p className="item-list-empty">No items in this project yet.</p>
  }
  const statusName = (id: number) => statuses.find((s) => s.id === id)?.name ?? `Status #${id}`
  const typeName = (id: number) => itemTypes.find((t) => t.id === id)?.name ?? `Type #${id}`

  return (
    <ul className="item-list">
      {items.map((item) => (
        <li key={item.id} className="item-list-item">
          <h3>{item.title}</h3>
          <p>
            {typeName(item.itemTypeId)} · {statusName(item.statusId)}
          </p>
        </li>
      ))}
    </ul>
  )
}