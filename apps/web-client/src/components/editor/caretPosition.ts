export interface CaretClientPosition {
  left: number
  top: number
  height: number
}

const MIRRORED_STYLE_PROPERTIES = [
  'boxSizing',
  'width',
  'height',
  'overflow',
  'borderTopWidth',
  'borderRightWidth',
  'borderBottomWidth',
  'borderLeftWidth',
  'paddingTop',
  'paddingRight',
  'paddingBottom',
  'paddingLeft',
  'fontStyle',
  'fontVariant',
  'fontWeight',
  'fontStretch',
  'fontSize',
  'fontSizeAdjust',
  'lineHeight',
  'fontFamily',
  'textAlign',
  'textTransform',
  'textIndent',
  'textDecoration',
  'letterSpacing',
  'wordSpacing',
  'tabSize',
] as const

export function getTextControlCaretPosition(
  element: HTMLInputElement | HTMLTextAreaElement,
): CaretClientPosition {
  const selectionStart = element.selectionStart ?? element.value.length
  const rect = element.getBoundingClientRect()
  const style = window.getComputedStyle(element)
  const mirror = document.createElement('div')
  const marker = document.createElement('span')

  mirror.style.position = 'fixed'
  mirror.style.visibility = 'hidden'
  mirror.style.pointerEvents = 'none'
  mirror.style.left = `${rect.left}px`
  mirror.style.top = `${rect.top}px`
  mirror.style.whiteSpace = element instanceof HTMLTextAreaElement ? 'pre-wrap' : 'pre'
  mirror.style.overflowWrap = element instanceof HTMLTextAreaElement ? 'break-word' : 'normal'

  for (const property of MIRRORED_STYLE_PROPERTIES) {
    mirror.style[property] = style[property]
  }

  mirror.textContent = element.value.slice(0, selectionStart)
  marker.textContent = element.value.slice(selectionStart, selectionStart + 1) || '\u200b'
  mirror.append(marker)
  document.body.append(mirror)

  mirror.scrollTop = element.scrollTop
  mirror.scrollLeft = element.scrollLeft

  const markerRect = marker.getBoundingClientRect()
  const position = {
    left: markerRect.left - element.scrollLeft,
    top: markerRect.top - element.scrollTop,
    height: markerRect.height || parseFloat(style.lineHeight) || 16,
  }

  mirror.remove()
  return position
}
