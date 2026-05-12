import type { NodeCatalogItem, NodeParamItem } from '@/types/api'

export const BUTTON_BRANCHING_NODE_TYPE = 'button_branching'
export const MESSAGE_NODE_TYPE = 'send_message'
export const EDIT_MESSAGE_NODE_TYPE = 'edit_message'
export const DATA_NODE_TYPE = 'data_record'
export const GOTO_NODE_TYPE = 'goto_node'
export const GROUP_NODE_TYPE = 'group_node'
export const GROUP_ENTRY_NODE_TYPE = 'group_entry_marker'
export const GROUP_EXIT_NODE_TYPE = 'group_exit_marker'
export const VK_SEND_KEYBOARD_NODE_TYPE = 'vk_send_keyboard'
export const VK_SEND_CAROUSEL_NODE_TYPE = 'vk_send_carousel'

export interface EditorNodeUiState {
  textHeight?: number
}

export interface EditorNodeData {
  type: string
  params: Record<string, unknown>
  ui?: EditorNodeUiState
}

export interface EditorFlowNode {
  id: string
  type: string
  position: { x: number; y: number }
  data: EditorNodeData
  selected?: boolean
}

export interface SelectedInspector {
  node: EditorFlowNode
  data: EditorNodeData
  params: NodeParamItem[]
}

/**
 * Возвращает параметры узла из каталога. Прозрачная функция-обёртка —
 * единая точка входа на случай если в будущем понадобятся UI-преобразования
 * каталога перед рендером.
 */
export function getUiParams(
  catalog: NodeCatalogItem[],
  nodeType: string,
): NodeParamItem[] {
  return catalog.find(c => c.type === nodeType)?.schema ?? []
}

export function normalizeNodeParamType(type?: string): string {
  switch (type) {
    case 'NODE_PARAM_TYPE_STRING':
      return 'string'
    case 'NODE_PARAM_TYPE_INT':
      return 'int'
    case 'NODE_PARAM_TYPE_BOOL':
      return 'bool'
    case 'NODE_PARAM_TYPE_STRING_DICTIONARY':
      return 'stringdictionary'
    case 'NODE_PARAM_TYPE_STRING_LIST':
      return 'stringlist'
    case 'NODE_PARAM_TYPE_OBJECT':
      return 'object'
    case 'NODE_PARAM_TYPE_ENUM':
      return 'enum'
    case 'NODE_PARAM_TYPE_OBJECT_LIST':
      return 'objectlist'
    case 'NODE_PARAM_TYPE_OBJECT_MATRIX':
      return 'objectmatrix'
    default:
      return (type ?? '').toLowerCase()
  }
}

export interface NodeOutputPort {
  id: string
  label: string
}

export interface ButtonBranchingButton {
  label: string
  payload: string
  style?: string
}

export type ButtonBranchingButtonRow = ButtonBranchingButton[]

export const BUTTON_STYLE_VALUES = ['primary', 'secondary', 'success', 'danger'] as const

export type ButtonStyleValue = typeof BUTTON_STYLE_VALUES[number]

export function isButtonStyleValue(value: unknown): value is ButtonStyleValue {
  return typeof value === 'string' && (BUTTON_STYLE_VALUES as readonly string[]).includes(value)
}

export function canProvideMessageId(type: string): boolean {
  return [
    BUTTON_BRANCHING_NODE_TYPE,
    MESSAGE_NODE_TYPE,
    'send_message',
    'send_buttons',
    'send_image',
    'receive_message',
    'vk_send_keyboard',
    'vk_send_carousel',
    'vk_remove_keyboard',
  ].includes(type)
}

export function messageConsumersFor(
  nodes: EditorFlowNode[],
  inspector: SelectedInspector,
): EditorFlowNode[] {
  if (!canProvideMessageId(inspector.data.type)) return []

  const variable = inspector.data.params.messageIdVariable
  if (typeof variable !== 'string' || !variable.trim()) return []

  return nodes.filter(node =>
    ['edit_message', 'delete_message'].includes(node.data.type)
    && node.data.params.messageIdVariable === variable,
  )
}

export function getNodeOutputPorts(
  nodeType: string,
  params: Record<string, unknown>,
): NodeOutputPort[] {
  if (isSmartButtonBranchingNodeType(nodeType)) {
    const ports = readSmartButtonBranchingButtons(nodeType, params).map(button => ({
      id: button.payload,
      label: button.label,
    }))

    return dedupePorts(ports)
  }

  if (nodeType === 'switch') {
    const cases = Array.isArray(params.cases) ? params.cases : []
    const ports = cases
      .map((item) => {
        const candidate = item as { branchKey?: unknown; value?: unknown }
        const branchKey =
          typeof candidate.branchKey === 'string' ? candidate.branchKey.trim() : ''
        const value =
          typeof candidate.value === 'string' ? candidate.value.trim() : branchKey

        if (!branchKey) return null

        return {
          id: branchKey,
          label: value || branchKey,
        }
      })
      .filter((item): item is NodeOutputPort => item !== null)

    ports.push({ id: 'default', label: 'default' })

    return dedupePorts(ports)
  }

  if (nodeType === 'condition') {
    return [
      { id: 'true', label: 'true' },
      { id: 'false', label: 'false' },
    ]
  }

  return []
}

export function isSmartButtonBranchingNodeType(nodeType: string): boolean {
  return [
    BUTTON_BRANCHING_NODE_TYPE,
    MESSAGE_NODE_TYPE,
    EDIT_MESSAGE_NODE_TYPE,
    VK_SEND_KEYBOARD_NODE_TYPE,
    VK_SEND_CAROUSEL_NODE_TYPE,
  ].includes(nodeType)
}

export function readSmartButtonBranchingButtons(
  nodeType: string,
  params: Record<string, unknown>,
): ButtonBranchingButton[] {
  if (nodeType === VK_SEND_KEYBOARD_NODE_TYPE) {
    return readVkKeyboardButtons(params.buttons)
  }

  if (nodeType === VK_SEND_CAROUSEL_NODE_TYPE) {
    return readVkCarouselButtons(params.cards)
  }

  return readButtonBranchingButtons(params.buttons)
}

export function readButtonBranchingButtons(value: unknown): ButtonBranchingButton[] {
  return flattenButtonBranchingRows(readButtonBranchingButtonRows(value))
}

export function readButtonBranchingButtonRows(value: unknown): ButtonBranchingButtonRow[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  const rawRows = isButtonMatrix(value) ? value : value.map(item => [item])
  const rows: ButtonBranchingButtonRow[] = []

  let buttonIndex = 0
  for (const row of rawRows) {
    const nextRow: ButtonBranchingButtonRow = []
    for (const item of row) {
      if (!isRecord(item)) continue
      const label = rawStringValue(item.label) || stringValue(item.payload)
      if (!label) continue

      const existingPayload = stringValue(item.payload)
      if (existingPayload && !usedPayloads.has(existingPayload)) {
        usedPayloads.add(existingPayload)
        nextRow.push({
          label,
          payload: existingPayload,
          ...(isButtonStyleValue(item.style) ? { style: item.style } : {}),
        })
        buttonIndex += 1
        continue
      }

      nextRow.push({
        label,
        payload: createUniqueButtonPayload(label, buttonIndex, usedPayloads),
        ...(isButtonStyleValue(item.style) ? { style: item.style } : {}),
      })
      buttonIndex += 1
    }

    if (nextRow.length) rows.push(nextRow)
  }

  return rows
}

export function flattenButtonBranchingRows(rows: ButtonBranchingButtonRow[]): ButtonBranchingButton[] {
  return rows.flatMap(row => row)
}

export function normalizeButtonBranchingButtonRows(value: unknown): ButtonBranchingButtonRow[] {
  return readButtonBranchingButtonRows(value).map(row =>
    row.map(button => ({
      label: button.label,
      payload: button.payload,
      ...(button.style ? { style: button.style } : {}),
    })),
  )
}

function isButtonMatrix(value: unknown[]): value is unknown[][] {
  return value.every(item => Array.isArray(item))
}

function readVkKeyboardButtons(value: unknown): ButtonBranchingButton[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  const rawRows = isButtonMatrix(value) ? value : value.map(item => [item])
  const rows: ButtonBranchingButtonRow[] = []
  let buttonIndex = 0

  for (const row of rawRows) {
    const nextRow: ButtonBranchingButtonRow = []
    for (const item of row) {
      if (!isRecord(item)) continue
      const label = rawStringValue(item.label) || stringValue(item.payload)
      if (!label) continue

      const existingPayload = stringValue(item.payload)
      const payload = existingPayload
        ? reserveUniqueButtonPayload(existingPayload, usedPayloads)
        : createUniqueButtonPayload(label, buttonIndex, usedPayloads)

      nextRow.push({ label, payload })
      buttonIndex += 1
    }

    if (nextRow.length) rows.push(nextRow)
  }

  return flattenButtonBranchingRows(rows)
}

function readVkCarouselButtons(value: unknown): ButtonBranchingButton[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  const buttons: ButtonBranchingButton[] = []
  let buttonIndex = 0

  for (const card of value) {
    if (!isRecord(card) || !Array.isArray(card.buttons)) continue

    for (const item of card.buttons) {
      if (!isRecord(item)) continue
      if (stringValue(item.link)) continue

      const label = rawStringValue(item.label) || stringValue(item.payload) || stringValue(item.link)
      const payload = stringValue(item.payload)
      if (!label) continue

      const stablePayload = payload
        ? reserveUniqueButtonPayload(payload, usedPayloads)
        : createUniqueButtonPayload(label, buttonIndex, usedPayloads)

      buttons.push({
        label,
        payload: stablePayload,
        ...(isButtonStyleValue(item.style) ? { style: item.style } : {}),
      })
      buttonIndex += 1
    }
  }

  return buttons
}

function createUniqueButtonPayload(label: string, index: number, usedPayloads: Set<string>): string {
  const base = createButtonPayload(label, index)
  let candidate = base
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${base}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function reserveUniqueButtonPayload(payload: string, usedPayloads: Set<string>): string {
  let candidate = payload
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${payload}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function createButtonPayload(label: string, index: number): string {
  return label.trim()
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^\p{L}\p{N}_-]+/gu, '_')
    .replace(/^_+|_+$/g, '')
    || `button_${index + 1}`
}

function dedupePorts(ports: NodeOutputPort[]): NodeOutputPort[] {
  const seen = new Set<string>()
  const result: NodeOutputPort[] = []

  for (const port of ports) {
    if (seen.has(port.id)) continue
    seen.add(port.id)
    result.push(port)
  }

  return result
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value.trim() : ''
}

function rawStringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
