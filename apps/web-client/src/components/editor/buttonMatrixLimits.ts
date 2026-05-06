import type { NodeParamItem } from '@/types/api'

export interface ObjectMatrixLimits {
  maxRows?: number
  maxItemsPerRow?: number
  maxItemsTotal?: number
}

type NodeParamItemWithLimits = NodeParamItem & {
  maxRows?: unknown
  max_items?: unknown
  max_rows?: unknown
  maxItemsPerRow?: unknown
  max_items_per_row?: unknown
  maxItemsTotal?: unknown
  max_items_total?: unknown
}

export function readObjectMatrixLimits(schema: NodeParamItem): ObjectMatrixLimits {
  const source = schema as NodeParamItemWithLimits

  return {
    maxRows: readPositiveInt(source.maxRows ?? source.max_rows),
    maxItemsPerRow: readPositiveInt(source.maxItemsPerRow ?? source.max_items_per_row),
    maxItemsTotal: readPositiveInt(source.maxItemsTotal ?? source.max_items_total ?? source.max_items),
  }
}

export function canAddObjectMatrixRow(
  rows: readonly (readonly unknown[])[],
  schema: NodeParamItem,
  itemCountInNewRow = 0,
): boolean {
  const limits = readObjectMatrixLimits(schema)
  const totalItems = countObjectMatrixItems(rows)

  return !(
    (limits.maxRows != null && rows.length + 1 > limits.maxRows)
    || (limits.maxItemsPerRow != null && itemCountInNewRow > limits.maxItemsPerRow)
    || (limits.maxItemsTotal != null && totalItems + itemCountInNewRow > limits.maxItemsTotal)
  )
}

export function canAddObjectMatrixItem(
  rows: readonly (readonly unknown[])[],
  rowIndex: number,
  schema: NodeParamItem,
): boolean {
  const limits = readObjectMatrixLimits(schema)
  const row = rows[rowIndex] ?? []
  const totalItems = countObjectMatrixItems(rows)

  return !(
    (limits.maxItemsPerRow != null && row.length + 1 > limits.maxItemsPerRow)
    || (limits.maxItemsTotal != null && totalItems + 1 > limits.maxItemsTotal)
  )
}

export function getObjectMatrixLimitMessage(
  rows: readonly (readonly unknown[])[],
  schema: NodeParamItem,
  itemLabel = 'элементов',
): string {
  const violation = getObjectMatrixLimitViolation(rows, schema, itemLabel)
  if (violation) return violation

  const limits = readObjectMatrixLimits(schema)
  const totalItems = countObjectMatrixItems(rows)

  if (limits.maxItemsTotal != null && totalItems >= limits.maxItemsTotal) {
    return `Лимит: не больше ${limits.maxItemsTotal} ${itemLabel}.`
  }

  if (limits.maxRows != null && rows.length >= limits.maxRows) {
    return `Лимит: не больше ${limits.maxRows} рядов.`
  }

  return ''
}

export function getObjectMatrixLimitViolation(
  rows: readonly (readonly unknown[])[],
  schema: NodeParamItem,
  itemLabel = 'элементов',
): string {
  const limits = readObjectMatrixLimits(schema)
  const totalItems = countObjectMatrixItems(rows)

  if (limits.maxRows != null && rows.length > limits.maxRows) {
    return `Слишком много рядов: ${rows.length}. Допустимо не больше ${limits.maxRows}.`
  }

  if (limits.maxItemsPerRow != null) {
    const invalidRowIndex = rows.findIndex(row => row.length > limits.maxItemsPerRow!)
    if (invalidRowIndex >= 0) {
      const invalidRow = rows[invalidRowIndex] ?? []
      return `В ряду ${invalidRowIndex + 1} слишком много ${itemLabel}: ${invalidRow.length}. Допустимо не больше ${limits.maxItemsPerRow}.`
    }
  }

  if (limits.maxItemsTotal != null && totalItems > limits.maxItemsTotal) {
    return `Слишком много ${itemLabel}: ${totalItems}. Допустимо не больше ${limits.maxItemsTotal}.`
  }

  return ''
}

export function countObjectMatrixItems(rows: readonly (readonly unknown[])[]): number {
  return rows.reduce((total, row) => total + row.length, 0)
}

function readPositiveInt(value: unknown): number | undefined {
  if (typeof value !== 'number' || !Number.isInteger(value) || value <= 0) return undefined
  return value
}
