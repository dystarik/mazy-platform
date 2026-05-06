export type VariableHighlightSegment = {
  id: string
  text: string
  state: 'plain' | 'known' | 'future' | 'missing'
}

export interface VariableScope {
  available: string[]
  future: string[]
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
  const scope = normalizeVariableScope(variableScope)
  return [...new Set(scope.available.map(item => item.trim()).filter(isVisibleVariableSuggestion))]
    .sort((a, b) => a.localeCompare(b))
}

function isVisibleVariableSuggestion(variableName: string): boolean {
  if (!variableName) return false
  if (/^button_payload_/i.test(variableName)) return false
  if (/^message_.+_id$/i.test(variableName)) return false
  return true
}
