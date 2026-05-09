export type VariableHighlightSegment = {
  id: string
  text: string
  state: 'plain' | 'known' | 'future' | 'missing'
}

export interface VariableScope {
  available: string[]
  future: string[]
}

export interface VariableSuggestion {
  value: string
  detail?: string
  insertValue?: string
  closeBrace?: boolean
}

const VARIABLE_PATTERN = /\{([^{}]+)\}/g

export function buildVariableHighlightSegments(
  value: string,
  variableScope: VariableScope | string[] = [],
): VariableHighlightSegment[] {
  const scope = normalizeVariableScope(variableScope)
  const known = new Set(scope.available.map(variable => variable.trim()).filter(Boolean))
  const future = new Set(scope.future.map(variable => variable.trim()).filter(Boolean))
  const segments: VariableHighlightSegment[] = []
  let lastIndex = 0
  let segmentIndex = 0

  VARIABLE_PATTERN.lastIndex = 0
  let match: RegExpExecArray | null
  while ((match = VARIABLE_PATTERN.exec(value)) !== null) {
    if (match.index > lastIndex) {
      segments.push({
        id: `plain-${segmentIndex++}`,
        text: value.slice(lastIndex, match.index),
        state: 'plain',
      })
    }

    const variableName = match[1]?.trim() ?? ''
    segments.push({
      id: `variable-${segmentIndex++}`,
      text: match[0],
      state: known.has(variableName)
        ? 'known'
        : future.has(variableName)
          ? 'future'
          : 'missing',
    })
    lastIndex = match.index + match[0].length
  }

  if (lastIndex < value.length || segments.length === 0) {
    segments.push({
      id: `plain-${segmentIndex}`,
      text: value.slice(lastIndex),
      state: 'plain',
    })
  }

  return segments
}

export function normalizeVariableScope(variableScope: VariableScope | string[]): VariableScope {
  if (Array.isArray(variableScope)) {
    return {
      available: variableScope,
      future: [],
    }
  }

  return {
    available: variableScope.available ?? [],
    future: variableScope.future ?? [],
  }
}

export function variableSuggestions(variableScope: VariableScope | string[]): string[] {
  return variableSuggestionItems(variableScope).map(suggestion => suggestion.value)
}

export function variableSuggestionItems(variableScope: VariableScope | string[], query = ''): VariableSuggestion[] {
  const scope = normalizeVariableScope(variableScope)
  const variables = [...new Set(scope.available.map(item => item.trim()).filter(isVisibleVariableSuggestion))]
  const objectVariableNames = new Set(
    variables
      .map(variable => variable.split('.')[0]?.trim() ?? '')
      .filter(variable => variable && variables.some(item => item.startsWith(`${variable}.`))),
  )
  const normalizedQuery = query.trim().toLowerCase()
  const dotIndex = query.lastIndexOf('.')

  if (dotIndex >= 0) {
    const objectName = query.slice(0, dotIndex).trim()
    if (!objectName) return []
    const closeSuggestion = variables.includes(objectName)
      ? [{
          value: objectName,
          detail: 'закрыть }',
          insertValue: objectName,
          closeBrace: true,
        }]
      : []

    const fieldSuggestions = variables
      .filter(variable => variable.startsWith(`${objectName}.`))
      .filter(variable => variable.toLowerCase().includes(normalizedQuery))
      .map(variable => ({ value: variable }))
      .sort(compareVariableSuggestions)

    return [...closeSuggestion, ...fieldSuggestions]
  }

  return variables
    .filter(variable => !variable.includes('.'))
    .filter(variable => variable.toLowerCase().includes(normalizedQuery))
    .map(variable => ({
      value: variable,
      ...(objectVariableNames.has(variable)
        ? {
            detail: 'объект данных',
            insertValue: `${variable}.`,
            closeBrace: false,
          }
        : {}),
    }))
    .sort(compareVariableSuggestions)
}

function isVisibleVariableSuggestion(variableName: string): boolean {
  if (!variableName) return false
  if (/^button_payload_/i.test(variableName)) return false
  if (/^message_.+_id$/i.test(variableName)) return false
  return true
}

function compareVariableSuggestions(a: VariableSuggestion, b: VariableSuggestion): number {
  return a.value.localeCompare(b.value)
}
