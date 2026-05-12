import type { NodeCatalogItem, NodeParamItem } from '@/types/api'
import {
  BUTTON_BRANCHING_NODE_TYPE,
  DATA_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GOTO_NODE_TYPE,
  GROUP_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
} from '@/components/editor/editorTypes'
import { CATEGORIES, NODE_META, type CategoryKey } from '@/components/editor/nodes/nodeMeta'

export const BUTTON_BRANCHING_NODE_IDS_PARAM = '__buttonBranchingNodeIds'
export const BUTTON_BRANCHING_PAYLOAD_VARIABLE_DEFAULT = 'button_payload'

export const DATA_NODE_BACKEND_TYPES = new Set([
  'create_record',
  'get_record',
  'query_records',
  'update_record',
  'delete_record',
])

export const HIDDEN_RUNTIME_CATALOG_NODE_TYPES = new Set([
  ...DATA_NODE_BACKEND_TYPES,
  'vk_remove_keyboard',
])
export const SMART_BUTTON_BRANCHING_EDITOR_NODE_TYPES = [
  MESSAGE_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  BUTTON_BRANCHING_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
] as const

export interface EditorNodeDefinition {
  type: string
  label: string
  category: CategoryKey
  runtimeTypes: string[]
  catalogItem: NodeCatalogItem
}

export const BUTTON_BRANCHING_CATALOG_ITEM: NodeCatalogItem = {
  type: BUTTON_BRANCHING_NODE_TYPE,
  schema: [
    {
      key: 'text',
      type: 'NODE_PARAM_TYPE_STRING',
      isRequired: true,
      description: 'Текст сообщения над кнопками.',
    },
    {
      key: 'messageIdVariable',
      type: 'NODE_PARAM_TYPE_STRING',
      isRequired: false,
      description: 'Имя переменной для сохранения ID отправленного сообщения.',
    },
    {
      key: 'buttonPayloadVariable',
      type: 'NODE_PARAM_TYPE_STRING',
      isRequired: true,
      description: 'Имя переменной для сохранения выбранной кнопки.',
    },
    {
      key: 'buttons',
      type: 'NODE_PARAM_TYPE_OBJECT_MATRIX',
      isRequired: true,
      description: 'Ряды кнопок и ветки, по которым пойдёт сценарий.',
      fields: [
        {
          key: 'label',
          type: 'NODE_PARAM_TYPE_STRING',
          isRequired: true,
          description: 'Текст на кнопке, видимый пользователю.',
        },
        {
          key: 'payload',
          type: 'NODE_PARAM_TYPE_STRING',
          isRequired: true,
          description: 'Внутренний идентификатор для ветки.',
        },
        {
          key: 'style',
          type: 'NODE_PARAM_TYPE_ENUM',
          isRequired: false,
          description: 'Семантический стиль кнопки.',
          enumValues: ['primary', 'secondary', 'success', 'danger'],
        },
      ],
    },
  ],
}

export const GOTO_CATALOG_ITEM: NodeCatalogItem = {
  type: GOTO_NODE_TYPE,
  schema: [
    {
      key: 'targetNodeId',
      type: 'NODE_PARAM_TYPE_STRING',
      isRequired: true,
      description: 'Узел, к которому сценарий перейдёт без видимой длинной связи.',
    },
  ],
}

export const GROUP_CATALOG_ITEM: NodeCatalogItem = {
  type: GROUP_NODE_TYPE,
  schema: [
    {
      key: 'title',
      type: 'NODE_PARAM_TYPE_STRING',
      isRequired: true,
      description: 'Название группы, которое будет видно на схеме.',
    },
  ],
}

export const EDITOR_NODE_DEFINITIONS: Readonly<Record<string, EditorNodeDefinition>> = {
  [BUTTON_BRANCHING_NODE_TYPE]: {
    type: BUTTON_BRANCHING_NODE_TYPE,
    label: NODE_META[BUTTON_BRANCHING_NODE_TYPE]?.label ?? 'Сообщение с кнопками',
    category: NODE_META[BUTTON_BRANCHING_NODE_TYPE]?.category ?? 'messageSending',
    runtimeTypes: ['send_buttons', 'receive_button_press', 'switch'],
    catalogItem: BUTTON_BRANCHING_CATALOG_ITEM,
  },
  [MESSAGE_NODE_TYPE]: {
    type: MESSAGE_NODE_TYPE,
    label: NODE_META[MESSAGE_NODE_TYPE]?.label ?? 'Сообщение',
    category: NODE_META[MESSAGE_NODE_TYPE]?.category ?? 'messageSending',
    runtimeTypes: ['send_message'],
    catalogItem: { type: MESSAGE_NODE_TYPE, schema: [] },
  },
  [EDIT_MESSAGE_NODE_TYPE]: {
    type: EDIT_MESSAGE_NODE_TYPE,
    label: NODE_META[EDIT_MESSAGE_NODE_TYPE]?.label ?? 'Редактировать сообщение',
    category: NODE_META[EDIT_MESSAGE_NODE_TYPE]?.category ?? 'messageManagement',
    runtimeTypes: ['edit_message', 'receive_button_press', 'switch'],
    catalogItem: { type: EDIT_MESSAGE_NODE_TYPE, schema: [] },
  },
  [DATA_NODE_TYPE]: {
    type: DATA_NODE_TYPE,
    label: NODE_META[DATA_NODE_TYPE]?.label ?? 'Данные',
    category: NODE_META[DATA_NODE_TYPE]?.category ?? 'data',
    runtimeTypes: [...DATA_NODE_BACKEND_TYPES],
    catalogItem: { type: DATA_NODE_TYPE, schema: [] },
  },
  [GOTO_NODE_TYPE]: {
    type: GOTO_NODE_TYPE,
    label: NODE_META[GOTO_NODE_TYPE]?.label ?? 'Переход',
    category: NODE_META[GOTO_NODE_TYPE]?.category ?? 'logic',
    runtimeTypes: [],
    catalogItem: GOTO_CATALOG_ITEM,
  },
}

export function withEditorCatalogItems(runtimeCatalog: NodeCatalogItem[]): NodeCatalogItem[] {
  const byType = new Map<string, NodeCatalogItem>()

  for (const item of runtimeCatalog) {
    if (!item.type || HIDDEN_RUNTIME_CATALOG_NODE_TYPES.has(item.type)) continue
    byType.set(item.type, item)
  }

  for (const definition of Object.values(EDITOR_NODE_DEFINITIONS)) {
    const runtimeItem = definition.runtimeTypes
      .map(type => runtimeCatalog.find(item => item.type === type))
      .find((item): item is NodeCatalogItem => Boolean(item))

    byType.set(definition.type, runtimeItem
      ? mergeEditorCatalogItem(definition.catalogItem, runtimeItem, definition.type, runtimeCatalog)
      : definition.catalogItem)
  }

  return [...byType.values()]
}

function mergeEditorCatalogItem(
  editorItem: NodeCatalogItem,
  runtimeItem: NodeCatalogItem,
  editorType: string,
  runtimeCatalog: NodeCatalogItem[],
): NodeCatalogItem {
  if (editorType === MESSAGE_NODE_TYPE) {
    return withSendButtonsSchema({ ...runtimeItem, type: editorType }, runtimeCatalog)
  }

  if (!editorItem.schema?.length) {
    return { ...runtimeItem, type: editorType }
  }

  return {
    ...editorItem,
    schema: editorItem.schema.map(param => mergeParamLimits(param, runtimeItem.schema ?? [])),
  }
}

function withSendButtonsSchema(item: NodeCatalogItem, runtimeCatalog: NodeCatalogItem[]): NodeCatalogItem {
  const schema = item.schema ?? []
  if (schema.some(param => param.key === 'buttons')) return item

  const buttonsParam = runtimeCatalog
    .find(candidate => candidate.type === 'send_buttons')
    ?.schema
    ?.find(param => param.key === 'buttons')

  if (!buttonsParam) return item

  return {
    ...item,
    schema: [...schema, buttonsParam],
  }
}

function mergeParamLimits(editorParam: NodeParamItem, runtimeSchema: NodeParamItem[]): NodeParamItem {
  const runtimeParam = runtimeSchema.find(param => param.key === editorParam.key)
  if (!runtimeParam) return editorParam

  return {
    ...editorParam,
    ...pickParamLimits(runtimeParam),
    ...(editorParam.fields?.length
      ? { fields: editorParam.fields.map(field => mergeParamLimits(field, runtimeParam.fields ?? [])) }
      : {}),
  }
}

function pickParamLimits(param: NodeParamItem): Partial<NodeParamItem> {
  const source = param as NodeParamItem & {
    maxRows?: number
    maxItemsPerRow?: number
    maxItemsTotal?: number
  }

  return {
    ...(source.maxRows != null ? { maxRows: source.maxRows } : {}),
    ...(source.maxItemsPerRow != null ? { maxItemsPerRow: source.maxItemsPerRow } : {}),
    ...(source.maxItemsTotal != null ? { maxItemsTotal: source.maxItemsTotal } : {}),
  } as Partial<NodeParamItem>
}

export function buildEditorCatalogCategories(items: NodeCatalogItem[]) {
  const matches = items.filter(item => item.type && Boolean(NODE_META[item.type]))
  const metaOrder = Object.keys(NODE_META)

  return Object.values(CATEGORIES)
    .map(cat => ({
      label: cat.label,
      color: cat.color,
      iconSrc: cat.iconSrc,
      nodes: matches
        .filter(item => item.type && NODE_META[item.type]?.category === cat.key)
        .sort((a, b) => metaOrder.indexOf(a.type ?? '') - metaOrder.indexOf(b.type ?? '')),
    }))
    .filter(cat => cat.nodes.length > 0)
}

export function isRuntimeDataNodeType(type: string): boolean {
  return DATA_NODE_BACKEND_TYPES.has(type)
}

export function backendNodeTypeFromDataAction(actionValue: unknown): string {
  switch (actionValue) {
    case 'get':
      return 'get_record'
    case 'query':
      return 'query_records'
    case 'update':
      return 'update_record'
    case 'delete':
      return 'delete_record'
    case 'create':
    default:
      return 'create_record'
  }
}

export function dataActionFromBackendNodeType(nodeType: string): string {
  switch (nodeType) {
    case 'get_record':
      return 'get'
    case 'query_records':
      return 'query'
    case 'update_record':
      return 'update'
    case 'delete_record':
      return 'delete'
    case 'create_record':
    default:
      return 'create'
  }
}

export function createButtonPayloadVariable(nodeId: string): string {
  return `${BUTTON_BRANCHING_PAYLOAD_VARIABLE_DEFAULT}_${shortNodeId(nodeId)}`
}

export function createButtonBranchingCompiledNodeIds(blockId: string): import('./editorScenario.types').ButtonBranchingCompiledNodeIds {
  return {
    sendButtons: createDerivedGraphId(blockId, 'send_buttons'),
    receiveButtonPress: createDerivedGraphId(blockId, 'receive_button_press'),
    switch: createDerivedGraphId(blockId, 'switch'),
    deleteMessage: createDerivedGraphId(blockId, 'delete_message_after_press'),
    removeKeyboard: createDerivedGraphId(blockId, 'vk_remove_keyboard_after_press'),
    removeKeyboardDeleteMessage: createDerivedGraphId(blockId, 'delete_vk_remove_keyboard_message_after_press'),
  }
}

export function normalizeButtonBranchingCompiledNodeIds(value: unknown): import('./editorScenario.types').ButtonBranchingCompiledNodeIds | null {
  if (!isRecord(value)) return null

  const sendButtons = readString(value.sendButtons)
  const receiveButtonPress = readString(value.receiveButtonPress)
  const switchNode = readString(value.switch)
  const deleteMessage = readString(value.deleteMessage)
  const removeKeyboard = readString(value.removeKeyboard)
  const removeKeyboardDeleteMessage = readString(value.removeKeyboardDeleteMessage)

  if (!sendButtons || !receiveButtonPress || !switchNode) return null

  return {
    sendButtons,
    receiveButtonPress,
    switch: switchNode,
    ...(deleteMessage ? { deleteMessage } : {}),
    ...(removeKeyboard ? { removeKeyboard } : {}),
    ...(removeKeyboardDeleteMessage ? { removeKeyboardDeleteMessage } : {}),
  }
}

export function createDerivedGraphId(baseId: string, salt: string): string {
  const compact = baseId.replaceAll('-', '')
  if (!/^[\da-f]{32}$/i.test(compact)) return createGraphId()

  const tail = hashToHex(`${baseId}:${salt}`, 12)
  return [
    compact.slice(0, 8),
    compact.slice(8, 12),
    compact.slice(12, 16),
    compact.slice(16, 20),
    tail,
  ].join('-')
}

function hashToHex(value: string, length: number): string {
  let hash = 0x811c9dc5
  for (let index = 0; index < value.length; index += 1) {
    hash ^= value.charCodeAt(index)
    hash = Math.imul(hash, 0x01000193)
  }

  let hex = ''
  let next = hash >>> 0
  while (hex.length < length) {
    next = Math.imul(next ^ 0x9e3779b9, 0x01000193) >>> 0
    hex += next.toString(16).padStart(8, '0')
  }

  return hex.slice(0, length)
}

function shortNodeId(id: string): string {
  return id.replace(/-/g, '').slice(0, 8)
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function createGraphId(): string {
  return crypto.randomUUID?.() ?? `node-${Date.now()}-${Math.random().toString(16).slice(2)}`
}
