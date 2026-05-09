import { nextTick, ref, watch, type Ref } from 'vue'

interface TextHistoryEntry {
  value: string
  selectionStart: number
  selectionEnd: number
}

export function useTextInputHistory(
  value: () => string,
  getElement: () => HTMLInputElement | HTMLTextAreaElement | null,
  updateValue: (nextValue: string) => void,
): {
  handleHistoryKeydown: (event: KeyboardEvent) => boolean
  rememberHistoryValue: (element: HTMLInputElement | HTMLTextAreaElement) => void
} {
  const history = ref<TextHistoryEntry[]>([createEntry(value())]) as Ref<TextHistoryEntry[]>
  const historyIndex = ref(0)
  let isApplyingHistory = false

  watch(value, (nextValue) => {
    if (isApplyingHistory) {
      isApplyingHistory = false
      return
    }

    const current = history.value[historyIndex.value]
    if (current?.value === nextValue) return

    history.value = [createEntry(nextValue)]
    historyIndex.value = 0
  })

  function rememberHistoryValue(element: HTMLInputElement | HTMLTextAreaElement): void {
    const entry = createEntry(
      element.value,
      element.selectionStart ?? element.value.length,
      element.selectionEnd ?? element.value.length,
    )
    const current = history.value[historyIndex.value]
    if (current?.value === entry.value) {
      history.value[historyIndex.value] = entry
      return
    }

    history.value = [...history.value.slice(0, historyIndex.value + 1), entry].slice(-100)
    historyIndex.value = history.value.length - 1
  }

  function handleHistoryKeydown(event: KeyboardEvent): boolean {
    if (!(event.ctrlKey || event.metaKey) || event.altKey) return false

    const key = event.key.toLowerCase()
    if (key === 'z') {
      applyHistory(event.shiftKey ? 1 : -1)
      event.preventDefault()
      event.stopPropagation()
      return true
    }

    if (key === 'y') {
      applyHistory(1)
      event.preventDefault()
      event.stopPropagation()
      return true
    }

    return false
  }

  function applyHistory(delta: -1 | 1): void {
    const nextIndex = Math.max(0, Math.min(history.value.length - 1, historyIndex.value + delta))
    const entry = history.value[nextIndex]
    if (!entry) return

    historyIndex.value = nextIndex
    isApplyingHistory = true
    updateValue(entry.value)

    void nextTick(() => {
      const element = getElement()
      element?.focus()
      element?.setSelectionRange(entry.selectionStart, entry.selectionEnd)
    })
  }

  return {
    handleHistoryKeydown,
    rememberHistoryValue,
  }
}

function createEntry(value: string, selectionStart = value.length, selectionEnd = selectionStart): TextHistoryEntry {
  return {
    value,
    selectionStart,
    selectionEnd,
  }
}
