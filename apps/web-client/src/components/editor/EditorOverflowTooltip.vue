<template>
  <span ref="rootRef" class="editor-overflow-tooltip">
    <slot />
  </span>

  <Teleport to="body">
    <div
      v-if="isOpen"
      ref="tooltipRef"
      class="editor-overflow-tooltip__popup nodrag"
      :style="tooltipStyle"
      role="tooltip"
      @mouseenter="handleTooltipEnter"
      @mouseleave="handleTooltipLeave"
    >
      <span
        v-for="segment in highlightSegments"
        :key="segment.id"
        :class="`editor-overflow-tooltip__segment editor-overflow-tooltip__segment--${segment.state}`"
      >{{ segment.text }}</span>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, onUpdated, ref, watch, type CSSProperties } from 'vue'
import {
  buildVariableHighlightSegments,
  type VariableScope,
} from '@/components/editor/variableHighlight'

const props = withDefaults(defineProps<{
  value: string
  disabled?: boolean
  targetSelector?: string
  knownVariables?: string[]
  variableScope?: VariableScope
}>(), {
  disabled: false,
  targetSelector: 'input, textarea',
  knownVariables: () => [],
  variableScope: undefined,
})

const rootRef = ref<HTMLElement | null>(null)
const tooltipRef = ref<HTMLElement | null>(null)
const isOpen = ref(false)
const tooltipStyle = ref<CSSProperties>({})
const currentTarget = ref<HTMLInputElement | HTMLTextAreaElement | null>(null)
const pointerOnTarget = ref(false)
const pointerOnTooltip = ref(false)
const focusWithin = ref(false)
let hideTimer: number | null = null

const highlightSegments = computed(() =>
  buildVariableHighlightSegments(props.value, props.variableScope ?? props.knownVariables),
)

watch(() => [props.value, props.disabled] as const, () => {
  if (props.disabled || !props.value) {
    hideTooltip()
    return
  }

  if (isOpen.value) {
    void nextTick(showTooltip)
  }
})

onMounted(() => {
  void nextTick(attachTargetListeners)
})

onUpdated(() => {
  void nextTick(attachTargetListeners)
})

onBeforeUnmount(() => {
  detachTargetListeners()
  clearHideTimer()
  removeWindowListeners()
})

function attachTargetListeners(): void {
  const target = findTarget()
  if (target === currentTarget.value) return

  detachTargetListeners()
  currentTarget.value = target

  if (!target) return
  target.addEventListener('mouseenter', handleTargetEnter)
  target.addEventListener('mouseleave', handleTargetLeave)
  target.addEventListener('focus', handleTargetFocus)
  target.addEventListener('blur', handleTargetBlur)
  target.addEventListener('keydown', handleTargetKeydown)
}

function detachTargetListeners(): void {
  const target = currentTarget.value
  if (!target) return

  target.removeEventListener('mouseenter', handleTargetEnter)
  target.removeEventListener('mouseleave', handleTargetLeave)
  target.removeEventListener('focus', handleTargetFocus)
  target.removeEventListener('blur', handleTargetBlur)
  target.removeEventListener('keydown', handleTargetKeydown)
  currentTarget.value = null
}

function findTarget(): HTMLInputElement | HTMLTextAreaElement | null {
  const root = rootRef.value
  if (!root) return null
  const target = root.querySelector(props.targetSelector)
  return target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement ? target : null
}

function handleTargetEnter(): void {
  pointerOnTarget.value = true
  clearHideTimer()
  void showTooltip()
}

function handleTargetLeave(): void {
  pointerOnTarget.value = false
  scheduleHide()
}

function handleTargetFocus(): void {
  focusWithin.value = true
  clearHideTimer()
  void showTooltip()
}

function handleTargetBlur(): void {
  focusWithin.value = false
  scheduleHide()
}

function handleTargetKeydown(event: Event): void {
  if (event instanceof KeyboardEvent && event.key === 'Escape') {
    hideTooltip()
  }
}

function handleTooltipEnter(): void {
  pointerOnTooltip.value = true
  clearHideTimer()
}

function handleTooltipLeave(): void {
  pointerOnTooltip.value = false
  scheduleHide()
}

async function showTooltip(): Promise<void> {
  const target = currentTarget.value ?? findTarget()
  if (!target || props.disabled || !props.value || !isTextOverflowing(target)) {
    hideTooltip()
    return
  }

  currentTarget.value = target
  setBasePosition(target)
  isOpen.value = true
  addWindowListeners()

  await nextTick()
  adjustPosition(target)
}

function hideTooltip(): void {
  clearHideTimer()
  isOpen.value = false
  pointerOnTooltip.value = false
  removeWindowListeners()
}

function scheduleHide(): void {
  clearHideTimer()
  hideTimer = window.setTimeout(() => {
    if (!pointerOnTarget.value && !pointerOnTooltip.value && !focusWithin.value) {
      hideTooltip()
    }
  }, 80)
}

function clearHideTimer(): void {
  if (hideTimer === null) return
  window.clearTimeout(hideTimer)
  hideTimer = null
}

function isTextOverflowing(target: HTMLInputElement | HTMLTextAreaElement): boolean {
  const tolerance = 1
  return target.scrollWidth > target.clientWidth + tolerance
    || target.scrollHeight > target.clientHeight + tolerance
}

function setBasePosition(target: HTMLElement): void {
  const rect = target.getBoundingClientRect()
  const maxWidth = Math.min(420, window.innerWidth - 16)
  const left = clamp(rect.left, 8, window.innerWidth - maxWidth - 8)
  const top = clamp(rect.bottom + 6, 8, window.innerHeight - 8)

  tooltipStyle.value = {
    left: `${left}px`,
    top: `${top}px`,
    maxWidth: `${maxWidth}px`,
  }
}

function adjustPosition(target: HTMLElement): void {
  const tooltip = tooltipRef.value
  if (!tooltip) return

  const rect = target.getBoundingClientRect()
  const tooltipRect = tooltip.getBoundingClientRect()
  const left = clamp(rect.left, 8, window.innerWidth - tooltipRect.width - 8)
  const preferredTop = rect.bottom + 6
  const top = preferredTop + tooltipRect.height <= window.innerHeight - 8
    ? preferredTop
    : Math.max(8, rect.top - tooltipRect.height - 6)

  tooltipStyle.value = {
    ...tooltipStyle.value,
    left: `${left}px`,
    top: `${top}px`,
  }
}

function addWindowListeners(): void {
  window.addEventListener('resize', handleViewportChange)
  window.addEventListener('scroll', handleViewportChange, true)
}

function removeWindowListeners(): void {
  window.removeEventListener('resize', handleViewportChange)
  window.removeEventListener('scroll', handleViewportChange, true)
}

function handleViewportChange(): void {
  if (!isOpen.value) return
  void showTooltip()
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value))
}
</script>

<style scoped>
.editor-overflow-tooltip {
  display: contents;
}

.editor-overflow-tooltip__popup {
  box-sizing: border-box;
  position: fixed;
  z-index: 10050;
  overflow: auto;
  max-height: 280px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-bg-card);
  color: var(--color-text);
  font-size: 11px;
  line-height: 16px;
  padding: 7px 9px;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
  box-shadow: 0 10px 28px rgba(15, 23, 42, 0.18);
}

.editor-overflow-tooltip__segment--known {
  color: var(--color-primary);
  font-weight: 600;
}

.editor-overflow-tooltip__segment--future {
  color: #b7791f;
  font-weight: 600;
}

.editor-overflow-tooltip__segment--missing {
  color: var(--color-danger);
  font-weight: 600;
}
</style>
