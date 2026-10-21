import type { Item } from '../../types/item'
import type { ItemType } from '../../types/itemType'

interface ItemListProps {
  items: Item[]
  itemTypes: ItemType[]
}

export function ItemList({ items, itemTypes }: ItemListProps) {
  if (items.length === 0) {
    return <p className="item-list-empty">No items in this project yet.</p>
  }

  const typeName = (id: number) => itemTypes.find((t) => t.id === id)?.name ?? `Type #${id}`

  return (
    <ul className="item-list">
      {items.map((item) => (
        <li key={item.id} className="item-list-item">
          <h3>{item.title}</h3>
          <p>
            {typeName(item.itemTypeId)} · Status #{item.statusId}
          </p>
        </li>
      ))}
    </ul>
  )
}