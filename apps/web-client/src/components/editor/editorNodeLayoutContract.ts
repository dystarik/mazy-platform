import {
  BUTTON_BRANCHING_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
  getNodeOutputPorts,
  readButtonBranchingButtonRows,
  type EditorNodeData,
  type NodeOutputPort,
} from '@/components/editor/editorTypes'

export const EDITOR_NODE_GRID_SIZE = 12
export const EDITOR_NODE_HALF_GRID_SIZE = EDITOR_NODE_GRID_SIZE / 2
export const EDITOR_NODE_PORT_ROW_HEIGHT = EDITOR_NODE_GRID_SIZE * 2
export const EDITOR_NODE_HEADER_ROW_HEIGHT = EDITOR_NODE_GRID_SIZE * 4
export const EDITOR_NODE_DEFAULT_WIDTH = EDITOR_NODE_GRID_SIZE * 17
export const EDITOR_NODE_DEFAULT_HEIGHT = EDITOR_NODE_GRID_SIZE * 7
export const EDITOR_NODE_DEFAULT_PORT_Y = EDITOR_NODE_GRID_SIZE * 6
export const EDITOR_NODE_GROUP_BOUNDARY_WIDTH = EDITOR_NODE_GRID_SIZE * 10
export const EDITOR_NODE_GROUP_BOUNDARY_HEIGHT = EDITOR_NODE_GRID_SIZE * 4
export const EDITOR_NODE_GROUP_BOUNDARY_PORT_Y = EDITOR_NODE_GRID_SIZE * 3
export const EDITOR_NODE_VK_MESSAGE_ID_FIELD_HEIGHT = EDITOR_NODE_GRID_SIZE * 5
export const EDITOR_NODE_VK_HIDE_AFTER_PRESS_FIELD_HEIGHT = EDITOR_NODE_GRID_SIZE * 5

const COMPACT_SERVICE_HEIGHT = EDITOR_NODE_GRID_SIZE * 6
const COMPACT_SERVICE_PORT_Y = EDITOR_NODE_GRID_SIZE * 5
const RECEIVE_MESSAGE_BASE_HEIGHT = EDITOR_NODE_GRID_SIZE * 13
const RECEIVE_MESSAGE_INPUT_PORT_Y = EDITOR_NODE_GRID_SIZE * 6
const RECEIVE_MESSAGE_VALIDATION_HEIGHT = EDITOR_NODE_GRID_SIZE * 4
const RECEIVE_MESSAGE_PATTERN_HEIGHT = EDITOR_NODE_GRID_SIZE * 4
const VK_CAROUSEL_FIRST_CARD_TOP = EDITOR_NODE_GRID_SIZE * 12
const VK_CAROUSEL_BUTTON_CENTER_OFFSET = EDITOR_NODE_GRID_SIZE * 19
const VK_CAROUSEL_BUTTON_ROW_HEIGHT = EDITOR_NODE_PORT_ROW_HEIGHT
const VK_CAROUSEL_BUTTON_ROW_GAP = EDITOR_NODE_GRID_SIZE
const VK_CAROUSEL_CARD_GAP = EDITOR_NODE_GRID_SIZE
const VK_CAROUSEL_CARD_BASE_HEIGHT = EDITOR_NODE_GRID_SIZE * 19

interface ButtonPortPosition {
  id: string
  y: number
}

export interface EditorNodeLayoutPort extends NodeOutputPort {
  y: number
}

export interface EditorNodeLayoutRow {
  id: string
  kind: 'header' | 'input' | 'output'
  y: number
  height: number
  portId?: string
}

export interface EditorNodeLayoutSection {
  id: string
  y: number
  height: number
  rows: EditorNodeLayoutRow[]
}

export interface EditorNodeLayoutContract {
  width: number
  height: number
  inputPortY: number
  outputPortYs: number[]
  outputPorts: EditorNodeLayoutPort[]
  sections: EditorNodeLayoutSection[]
  rows: EditorNodeLayoutRow[]
}

interface EditorNodeLayoutCoordinates {
  width: number
  height: number
  inputPortY: number
  outputPortYs: number[]
}

export function describeEditorNodeLayout(data: EditorNodeData): EditorNodeLayoutContract {
  const outputPorts = getNodeOutputPorts(data.type, data.params)
  const coordinates = describeEditorNodeCoordinates(data, outputPorts)

  return createEditorNodeLayoutContract(coordinates, outputPorts)
}

export function toEditorNodeLayoutMetrics(contract: EditorNodeLayoutContract): EditorNodeLayoutCoordinates {
  return {
    width: contract.width,
    height: contract.height,
    inputPortY: contract.inputPortY,
    outputPortYs: contract.outputPortYs,
  }
}

export function isEditorNodeGridAligned(value: number): boolean {
  return Number.isFinite(value) && value % EDITOR_NODE_GRID_SIZE === 0
}

function describeEditorNodeCoordinates(
  data: EditorNodeData,
  outputPorts: NodeOutputPort[],
): EditorNodeLayoutCoordinates {
  switch (data.type) {
    case 'condition':
      return {
        width: EDITOR_NODE_GRID_SIZE * 29,
        height: EDITOR_NODE_GRID_SIZE * 9,
        inputPortY: EDITOR_NODE_GRID_SIZE * 5,
        outputPortYs: outputPorts.map((_, index) => EDITOR_NODE_GRID_SIZE * 5 + index * EDITOR_NODE_PORT_ROW_HEIGHT),
      }

    case 'switch': {
      const outputPortYs = outputPorts.map((_, index) => EDITOR_NODE_GRID_SIZE * 12 + index * EDITOR_NODE_PORT_ROW_HEIGHT)
      return {
        width: EDITOR_NODE_DEFAULT_WIDTH,
        height: Math.max(
          EDITOR_NODE_GRID_SIZE * 14,
          lastValue(outputPortYs, EDITOR_NODE_DEFAULT_PORT_Y) + EDITOR_NODE_GRID_SIZE * 2,
        ),
        inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
        outputPortYs,
      }
    }

    case BUTTON_BRANCHING_NODE_TYPE:
    case MESSAGE_NODE_TYPE:
      return createButtonMatrixCoordinates(data, EDITOR_NODE_GRID_SIZE * 19, EDITOR_NODE_GRID_SIZE * 14, EDITOR_NODE_GRID_SIZE * 13, false)

    case EDIT_MESSAGE_NODE_TYPE:
      return createButtonMatrixCoordinates(data, EDITOR_NODE_GRID_SIZE * 19, EDITOR_NODE_GRID_SIZE * 18, EDITOR_NODE_GRID_SIZE * 17, false)

    case VK_SEND_KEYBOARD_NODE_TYPE:
      return createButtonMatrixCoordinates(
        data,
        EDITOR_NODE_GRID_SIZE * 21,
        EDITOR_NODE_GRID_SIZE * 23 - EDITOR_NODE_VK_MESSAGE_ID_FIELD_HEIGHT,
        EDITOR_NODE_GRID_SIZE * 14,
        true,
        vkHideAfterPressFieldHeight(data.params),
      )

    case VK_SEND_CAROUSEL_NODE_TYPE:
      return createVkCarouselCoordinates(data)

    case 'vk_remove_keyboard':
      return {
        width: EDITOR_NODE_GRID_SIZE * 19,
        height: EDITOR_NODE_GRID_SIZE * 13,
        inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
        outputPortYs: [],
      }

    case 'group_entry_marker':
    case 'group_exit_marker':
      return {
        width: EDITOR_NODE_GROUP_BOUNDARY_WIDTH,
        height: EDITOR_NODE_GROUP_BOUNDARY_HEIGHT,
        inputPortY: EDITOR_NODE_GROUP_BOUNDARY_PORT_Y,
        outputPortYs: [],
      }

    case 'goto_node':
    case 'delete_message':
      return createSimpleServiceCoordinates(EDITOR_NODE_GRID_SIZE * 8)

    case 'send_image':
    case 'receive_image':
      return createSimpleServiceCoordinates(EDITOR_NODE_GRID_SIZE * 14)

    case 'get_user_info':
      return createSimpleServiceCoordinates(EDITOR_NODE_GRID_SIZE * 19)

    case 'delay':
    case 'set_variable':
      return createCompactServiceCoordinates(EDITOR_NODE_GRID_SIZE)

    case 'typing_indicator':
      return createCompactServiceCoordinates()

    case 'receive_message':
      return createReceiveMessageCoordinates(data.params)

    case 'http_request':
      return createFixedWidthGenericCoordinates(EDITOR_NODE_GRID_SIZE * 25, outputPorts.length)

    default:
      return createGenericCoordinates(outputPorts.length)
  }
}

function createEditorNodeLayoutContract(
  coordinates: EditorNodeLayoutCoordinates,
  outputPorts: NodeOutputPort[],
): EditorNodeLayoutContract {
  const layoutPorts = outputPorts.map((port, index) => ({
    ...port,
    y: coordinates.outputPortYs[index] ?? EDITOR_NODE_DEFAULT_PORT_Y + index * EDITOR_NODE_PORT_ROW_HEIGHT,
  }))
  const rows: EditorNodeLayoutRow[] = [
    {
      id: 'header',
      kind: 'header',
      y: 0,
      height: EDITOR_NODE_HEADER_ROW_HEIGHT,
    },
    {
      id: 'input',
      kind: 'input',
      y: coordinates.inputPortY,
      height: EDITOR_NODE_PORT_ROW_HEIGHT,
    },
    ...layoutPorts.map(port => ({
      id: `output:${port.id}`,
      kind: 'output' as const,
      y: port.y,
      height: EDITOR_NODE_PORT_ROW_HEIGHT,
      portId: port.id,
    })),
  ]

  return {
    ...coordinates,
    outputPortYs: layoutPorts.map(port => port.y),
    outputPorts: layoutPorts,
    rows,
    sections: [{
      id: 'node',
      y: 0,
      height: coordinates.height,
      rows,
    }],
  }
}

function createGenericCoordinates(outputPortCount: number): EditorNodeLayoutCoordinates {
  return createFixedWidthGenericCoordinates(
    outputPortCount > 0 ? EDITOR_NODE_GRID_SIZE * 36 : EDITOR_NODE_DEFAULT_WIDTH,
    outputPortCount,
  )
}

function createFixedWidthGenericCoordinates(width: number, outputPortCount: number): EditorNodeLayoutCoordinates {
  const outputPortYs = outputPortCount > 0
    ? createLinearPortYs(EDITOR_NODE_DEFAULT_PORT_Y, outputPortCount)
    : []

  if (outputPortYs.length === 0) {
    return {
      width,
      height: EDITOR_NODE_DEFAULT_HEIGHT,
      inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
      outputPortYs,
    }
  }

  return {
    width,
    height: Math.max(
      EDITOR_NODE_GRID_SIZE * 13,
      lastValue(outputPortYs, EDITOR_NODE_DEFAULT_PORT_Y) + EDITOR_NODE_GRID_SIZE * 7,
    ),
    inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
    outputPortYs,
  }
}

function createReceiveMessageCoordinates(params: Record<string, unknown>): EditorNodeLayoutCoordinates {
  const validatorType = stringValue(params.validatorType)
  const validationExtraHeight = validatorType
    ? RECEIVE_MESSAGE_VALIDATION_HEIGHT + (
        validatorType === 'regex' || validatorType === 'number'
          ? RECEIVE_MESSAGE_PATTERN_HEIGHT
          : 0
      )
    : 0

  return {
    width: EDITOR_NODE_GRID_SIZE * 25,
    height: RECEIVE_MESSAGE_BASE_HEIGHT + validationExtraHeight,
    inputPortY: RECEIVE_MESSAGE_INPUT_PORT_Y,
    outputPortYs: [],
  }
}

function createCompactServiceCoordinates(extraHeight = 0): EditorNodeLayoutCoordinates {
  return {
    width: EDITOR_NODE_DEFAULT_WIDTH,
    height: COMPACT_SERVICE_HEIGHT + extraHeight,
    inputPortY: COMPACT_SERVICE_PORT_Y,
    outputPortYs: [],
  }
}

function createSimpleServiceCoordinates(height: number): EditorNodeLayoutCoordinates {
  return {
    width: EDITOR_NODE_DEFAULT_WIDTH,
    height,
    inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
    outputPortYs: [],
  }
}

function createButtonMatrixCoordinates(
  data: EditorNodeData,
  width: number,
  minHeight: number,
  firstButtonPortY: number,
  hasRowGap: boolean,
  extraHeight = 0,
): EditorNodeLayoutCoordinates {
  const outputPorts = getNodeOutputPorts(data.type, data.params)
  const outputPortYs = buttonPortYsFromPositions(
    outputPorts.map(port => port.id),
    data.type === VK_SEND_KEYBOARD_NODE_TYPE
      ? readVkKeyboardButtonPortPositions(data.params.buttons, firstButtonPortY, hasRowGap)
      : readEditorButtonPortPositions(data.params.buttons, firstButtonPortY),
  )
  const lastPortY = lastValue(outputPortYs, EDITOR_NODE_DEFAULT_PORT_Y)

  const baseHeight = Math.max(
    minHeight,
    outputPortYs.length ? lastPortY + EDITOR_NODE_GRID_SIZE * 5 : minHeight,
  )

  return {
    width,
    height: baseHeight + extraHeight,
    inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
    outputPortYs,
  }
}

function createVkCarouselCoordinates(data: EditorNodeData): EditorNodeLayoutCoordinates {
  const outputPorts = getNodeOutputPorts(data.type, data.params)
  const outputPortYs = buttonPortYsFromPositions(
    outputPorts.map(port => port.id),
    readVkCarouselButtonPortPositions(data.params.cards),
  )

  return {
    width: EDITOR_NODE_GRID_SIZE * 28,
    height: Math.max(
      EDITOR_NODE_GRID_SIZE * 18 - EDITOR_NODE_VK_MESSAGE_ID_FIELD_HEIGHT,
      lastValue(outputPortYs, EDITOR_NODE_DEFAULT_PORT_Y) + EDITOR_NODE_GRID_SIZE * 5,
    ),
    inputPortY: EDITOR_NODE_DEFAULT_PORT_Y,
    outputPortYs,
  }
}

function readEditorButtonPortPositions(value: unknown, firstButtonPortY: number): ButtonPortPosition[] {
  const rows = readButtonBranchingButtonRows(value)
  const result: ButtonPortPosition[] = []
  let rowTop = firstButtonPortY - EDITOR_NODE_HALF_GRID_SIZE

  for (const row of rows) {
    for (const button of row) {
      result.push({ id: button.payload, y: rowTop + EDITOR_NODE_HALF_GRID_SIZE })
      rowTop += EDITOR_NODE_PORT_ROW_HEIGHT
    }

    rowTop += EDITOR_NODE_PORT_ROW_HEIGHT
  }

  return result
}

function readVkKeyboardButtonPortPositions(
  value: unknown,
  firstButtonPortY: number,
  hasRowGap: boolean,
): ButtonPortPosition[] {
  const rows = readVkKeyboardRows(value)
  const result: ButtonPortPosition[] = []
  let rowTop = firstButtonPortY - EDITOR_NODE_HALF_GRID_SIZE

  for (const row of rows) {
    for (const button of row) {
      result.push({ id: button.payload, y: rowTop + EDITOR_NODE_HALF_GRID_SIZE })
      rowTop += EDITOR_NODE_PORT_ROW_HEIGHT
    }

    rowTop += EDITOR_NODE_PORT_ROW_HEIGHT + (hasRowGap ? EDITOR_NODE_GRID_SIZE : 0)
  }

  return result
}

function readVkCarouselButtonPortPositions(value: unknown): ButtonPortPosition[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  const result: ButtonPortPosition[] = []
  let cardTop = VK_CAROUSEL_FIRST_CARD_TOP
  let buttonIndex = 0

  for (const card of value) {
    if (!isRecord(card)) continue

    const buttons = readVkCarouselButtons(card.buttons, usedPayloads, buttonIndex)
    const firstButtonCenterY = cardTop + VK_CAROUSEL_BUTTON_CENTER_OFFSET
    buttons.forEach((button, index) => {
      if (!button.link) {
        result.push({
          id: button.payload,
          y: firstButtonCenterY + index * (VK_CAROUSEL_BUTTON_ROW_HEIGHT + VK_CAROUSEL_BUTTON_ROW_GAP),
        })
      }
    })
    buttonIndex += buttons.filter(button => !button.link).length

    if (buttons.length) {
      const buttonRowsHeight =
        buttons.length * VK_CAROUSEL_BUTTON_ROW_HEIGHT
        + (buttons.length - 1) * VK_CAROUSEL_BUTTON_ROW_GAP
      cardTop += VK_CAROUSEL_CARD_BASE_HEIGHT + buttonRowsHeight + VK_CAROUSEL_CARD_GAP
    } else {
      cardTop += VK_CAROUSEL_CARD_BASE_HEIGHT + VK_CAROUSEL_BUTTON_ROW_HEIGHT + VK_CAROUSEL_CARD_GAP
    }
  }

  return result
}

function readVkKeyboardRows(value: unknown): Array<Array<{ payload: string }>> {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  let buttonIndex = 0
  const rawRows = value.every(item => Array.isArray(item)) ? value : value.map(item => [item])
  const rows: Array<Array<{ payload: string }>> = []

  for (const row of rawRows) {
    if (!Array.isArray(row)) continue

    const nextRow: Array<{ payload: string }> = []
    for (const item of row) {
      if (!isRecord(item)) continue
      const label = rawStringValue(item.label) || stringValue(item.payload)
      const payload = stringValue(item.payload)
      if (!label) continue

      nextRow.push({
        payload: payload
          ? reserveUniquePayload(payload, usedPayloads)
          : createUniqueStablePayload(label, buttonIndex, usedPayloads),
      })
      buttonIndex += 1
    }

    if (nextRow.length) rows.push(nextRow)
  }

  return rows
}

function readVkCarouselButtons(
  value: unknown,
  usedPayloads: Set<string>,
  firstButtonIndex = 0,
): Array<{ payload: string, link: boolean }> {
  if (!Array.isArray(value)) return []

  const sourceRows = value.every(item => Array.isArray(item)) ? value : [value]
  const result: Array<{ payload: string, link: boolean }> = []
  let buttonIndex = firstButtonIndex

  for (const row of sourceRows) {
    if (!Array.isArray(row)) continue

    for (const item of row) {
      if (!isRecord(item)) continue

      const label = stringValue(item.label)
      const payload = stringValue(item.payload)
      const link = stringValue(item.link)
      if (!label && !payload && !link) continue

      result.push({
        payload: payload
          ? link ? payload : reserveUniquePayload(payload, usedPayloads)
          : link ? '' : createUniqueStablePayload(label || link, buttonIndex, usedPayloads),
        link: Boolean(link),
      })
      if (!link) buttonIndex += 1
    }
  }

  return result
}

function buttonPortYsFromPositions(portIds: string[], positions: ButtonPortPosition[]): number[] {
  const positionById = new Map<string, number>()

  for (const position of positions) {
    if (!positionById.has(position.id)) {
      positionById.set(position.id, position.y)
    }
  }

  return portIds.map((id, index) => positionById.get(id) ?? EDITOR_NODE_DEFAULT_PORT_Y + index * EDITOR_NODE_PORT_ROW_HEIGHT)
}

function createLinearPortYs(firstPortY: number, count: number): number[] {
  return Array.from({ length: count }, (_, index) => firstPortY + index * EDITOR_NODE_PORT_ROW_HEIGHT)
}

function reserveUniquePayload(payload: string, usedPayloads: Set<string>): string {
  let candidate = payload
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${payload}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function createUniqueStablePayload(label: string, index: number, usedPayloads: Set<string>): string {
  const base = label.trim()
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^\p{L}\p{N}_-]+/gu, '_')
    .replace(/^_+|_+$/g, '')
    || `button_${index + 1}`

  return reserveUniquePayload(base, usedPayloads)
}

function lastValue(values: number[], fallback: number): number {
  return values.at(-1) ?? fallback
}

function vkHideAfterPressFieldHeight(params: Record<string, unknown>): number {
  return params.deleteAfterButtonPress === true ? EDITOR_NODE_VK_HIDE_AFTER_PRESS_FIELD_HEIGHT : 0
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
