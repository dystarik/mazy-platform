import { parseApiError } from '@/composables/useApiError'

const SCENARIO_NODE_ERRORS_HEADER = 'x-scenario-node-validation-errors'

export interface ScenarioNodeHeaderError {
  code: string
  message: string
  nodeId?: string
  path?: string
}

export interface ScenarioApiError {
  messages: string[]
  nodeErrors: ScenarioNodeHeaderError[]
}

export function parseScenarioError(error: unknown): ScenarioApiError {
  return {
    messages: parseApiError(error),
    nodeErrors: parseScenarioNodeErrorsFromHeader(error),
  }
}

export function parseScenarioNodeErrorsFromHeader(error: unknown): ScenarioNodeHeaderError[] {
  const headerValue = readScenarioNodeErrorsHeader(error)
  if (!headerValue) return []

  return parseScenarioNodeErrorsPayload(headerValue)
}

function readScenarioNodeErrorsHeader(error: unknown): string | null {
  if (!(error instanceof Error)) return null

  const headers = (error as { response?: { headers?: unknown } }).response?.headers
  if (!headers) return null

  if (hasHeaderGetter(headers)) {
    const value = headers.get(SCENARIO_NODE_ERRORS_HEADER)
    return normalizeHeaderValue(value)
  }

  if (!isRecord(headers)) return null

  for (const [key, value] of Object.entries(headers)) {
    if (key.toLowerCase() === SCENARIO_NODE_ERRORS_HEADER) {
      return normalizeHeaderValue(value)
    }
  }

  return null
}

function parseScenarioNodeErrorsPayload(value: string): ScenarioNodeHeaderError[] {
  for (const candidate of payloadCandidates(value)) {
    const parsed = parseJson(candidate)
    if (Array.isArray(parsed)) {
      return parsed.map(normalizeScenarioNodeError).filter(isScenarioNodeHeaderError)
    }
  }

  return []
}

function payloadCandidates(value: string): string[] {
  const trimmed = value.trim()
  const candidates = [trimmed]
  const decoded = decodeBase64Json(trimmed)

  if (decoded && decoded !== trimmed) {
    candidates.push(decoded)
  }

  return candidates
}

function decodeBase64Json(value: string): string | null {
  try {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')
    const bytes = Uint8Array.from(atob(padded), char => char.charCodeAt(0))

    return new TextDecoder().decode(bytes)
  } catch {
    return null
  }
}

function normalizeScenarioNodeError(value: unknown): ScenarioNodeHeaderError | null {
  if (!isRecord(value)) return null

  const message = readString(value.message) ?? readString(value.code)
  if (!message) return null

  return {
    code: readString(value.code) ?? '',
    message,
    nodeId: readString(value.nodeId),
    path: readString(value.path),
  }
}

function isScenarioNodeHeaderError(value: ScenarioNodeHeaderError | null): value is ScenarioNodeHeaderError {
  return value !== null
}

function parseJson(value: string): unknown {
  try {
    return JSON.parse(value)
  } catch {
    return null
  }
}

function normalizeHeaderValue(value: unknown): string | null {
  if (typeof value === 'string') return value
  if (Array.isArray(value)) return value.filter(item => typeof item === 'string').join(',')
  return null
}

function hasHeaderGetter(value: unknown): value is { get: (name: string) => unknown } {
  return isRecord(value) && typeof value.get === 'function'
}

function readString(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value.trim() : undefined
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}
