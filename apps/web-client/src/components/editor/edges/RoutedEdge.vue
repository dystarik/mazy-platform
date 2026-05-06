<template>
  <BaseEdge
    :id="id"
    :path="path"
    :label="label"
    :label-x="labelPoint.x"
    :label-y="labelPoint.y"
    :marker-start="markerStart"
    :marker-end="markerEnd"
    :interaction-width="interactionWidth"
    :label-style="labelStyle"
    :label-bg-style="labelBgStyle"
    :label-bg-padding="labelBgPadding"
    :label-bg-border-radius="labelBgBorderRadius"
  />
  <path
    class="routed-edge__flow"
    :d="path"
    aria-hidden="true"
  />

  <g v-if="selected" class="routed-edge__editor">
    <path
      v-for="segment in draggableSegments"
      :key="segment.key"
      class="routed-edge__segment"
      :class="`routed-edge__segment--${segment.orientation}`"
      :d="segment.path"
      @pointerdown.stop.prevent="startSegmentDrag($event, segment.index, segment.orientation)"
      @dblclick.stop.prevent="addPointOnSegment(segment.index)"
    />
    <circle
      v-for="virtualBend in virtualBends"
      :key="virtualBend.key"
      class="routed-edge__virtual-handle"
      :cx="virtualBend.x"
      :cy="virtualBend.y"
      r="4"
      @pointerdown.stop.prevent="startVirtualBendDrag($event, virtualBend.index)"
    />
    <circle
      v-for="handle in bendHandles"
      :key="handle.index"
      class="routed-edge__handle"
      :cx="handle.x"
      :cy="handle.y"
      r="5"
      @pointerdown.stop.prevent="startPointDrag($event, handle.index)"
      @dblclick.stop.prevent="removePoint(handle.index)"
    />
  </g>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, type CSSProperties } from 'vue'
import { BaseEdge, Position, type EdgeProps, useVueFlow } from '@vue-flow/core'

export interface RoutedEdgePoint {
  x: number
  y: number
}

export interface RoutedEdgeData {
  route?: RoutedEdgePoint[]
  routeVersion?: number
}

type SegmentOrientation = 'horizontal' | 'vertical'

interface SegmentDragState {
  index: number
  orientation: SegmentOrientation
  startPointer: RoutedEdgePoint
  startRoute: RoutedEdgePoint[]
}

interface PointDragState {
  index: number
  startRoute: RoutedEdgePoint[]
}

const props = defineProps<EdgeProps<RoutedEdgeData>>()
const { screenToFlowCoordinate, updateEdgeData } = useVueFlow()

const segmentDrag = ref<SegmentDragState | null>(null)
const pointDrag = ref<PointDragState | null>(null)

const route = computed(() => createDisplayRoute(extractControlPoints(props.data?.route)))
const path = computed(() => createPolylinePath(route.value))

const labelPoint = computed(() => {
  const points = route.value
  if (points.length === 0) return sourcePoint()

  const middle = Math.floor(points.length / 2)
  return points[middle] ?? points[0] ?? sourcePoint()
})

const labelStyle = computed<CSSProperties | undefined>(() =>
  props.labelStyle as CSSProperties | undefined,
)

const labelBgStyle = computed<CSSProperties | undefined>(() =>
  props.labelBgStyle as CSSProperties | undefined,
)

const draggableSegments = computed(() => {
  const points = route.value
  const result: Array<{
    key: string
    index: number
    orientation: SegmentOrientation
    path: string
  }> = []

  for (let index = 0; index < points.length - 1; index += 1) {
    const from = points[index]
    const to = points[index + 1]
    const orientation = segmentOrientation(from, to)
    if (!from || !to || !orientation) continue

    result.push({
      key: `${index}-${orientation}`,
      index,
      orientation,
      path: `M ${round(from.x)} ${round(from.y)} L ${round(to.x)} ${round(to.y)}`,
    })
  }

  return result
})

const virtualBends = computed(() =>
  draggableSegments.value.map(segment => {
    const from = route.value[segment.index]
    const to = route.value[segment.index + 1]

    return {
      key: `virtual-${segment.key}`,
      index: segment.index,
      x: from && to ? (from.x + to.x) / 2 : 0,
      y: from && to ? (from.y + to.y) / 2 : 0,
    }
  }),
)

const bendHandles = computed(() =>
  route.value
    .slice(1, -1)
    .filter(point => !isCurrentEndpointStub(point))
    .map(point => ({ ...point, index: route.value.indexOf(point) })),
)

function startSegmentDrag(
  event: PointerEvent,
  index: number,
  orientation: SegmentOrientation,
): void {
  const editableRoute = materializeSegmentForDrag(route.value, index)

  segmentDrag.value = {
    index: editableRoute.index,
    orientation,
    startPointer: screenToFlowCoordinate({ x: event.clientX, y: event.clientY }),
    startRoute: editableRoute.route,
  }
  ;(event.currentTarget as SVGPathElement | null)?.setPointerCapture?.(event.pointerId)
  window.addEventListener('pointermove', handleSegmentDrag)
  window.addEventListener('pointerup', stopSegmentDrag, { once: true })
}

function handleSegmentDrag(event: PointerEvent): void {
  const drag = segmentDrag.value
  if (!drag) return

  const pointer = screenToFlowCoordinate({ x: event.clientX, y: event.clientY })
  const delta = drag.orientation === 'horizontal'
    ? pointer.y - drag.startPointer.y
    : pointer.x - drag.startPointer.x

  const nextRoute = drag.startRoute.map((point, index) => {
    if (index !== drag.index && index !== drag.index + 1) return point

    return drag.orientation === 'horizontal'
      ? { x: point.x, y: snap(point.y + delta) }
      : { x: snap(point.x + delta), y: point.y }
  })

  commitRoute(nextRoute)
}

function stopSegmentDrag(): void {
  segmentDrag.value = null
  window.removeEventListener('pointermove', handleSegmentDrag)
}

function startPointDrag(event: PointerEvent, index: number): void {
  pointDrag.value = {
    index,
    startRoute: cloneRoute(route.value),
  }
  ;(event.currentTarget as SVGCircleElement | null)?.setPointerCapture?.(event.pointerId)
  window.addEventListener('pointermove', handlePointDrag)
  window.addEventListener('pointerup', stopPointDrag, { once: true })
}

function startVirtualBendDrag(event: PointerEvent, segmentIndex: number): void {
  const routeWithPoint = insertPointAtSegment(route.value, segmentIndex)
  pointDrag.value = {
    index: segmentIndex + 1,
    startRoute: routeWithPoint,
  }
  ;(event.currentTarget as SVGCircleElement | null)?.setPointerCapture?.(event.pointerId)
  window.addEventListener('pointermove', handlePointDrag)
  window.addEventListener('pointerup', stopPointDrag, { once: true })
}

function handlePointDrag(event: PointerEvent): void {
  const drag = pointDrag.value
  if (!drag) return

  const pointer = screenToFlowCoordinate({ x: event.clientX, y: event.clientY })
  const nextRoute = drag.startRoute.map((point, index) =>
    index === drag.index
      ? { x: snap(pointer.x), y: snap(pointer.y) }
      : point,
  )

  commitRoute(nextRoute)
}

function stopPointDrag(): void {
  pointDrag.value = null
  window.removeEventListener('pointermove', handlePointDrag)
}

function addPointOnSegment(segmentIndex: number): void {
  commitRoute(insertPointAtSegment(route.value, segmentIndex))
}

function removePoint(index: number): void {
  const nextRoute = route.value.filter((_, pointIndex) => pointIndex !== index)
  commitRoute(nextRoute)
}

onBeforeUnmount(() => {
  window.removeEventListener('pointermove', handleSegmentDrag)
  window.removeEventListener('pointermove', handlePointDrag)
})

const SNAP = 12
const ENDPOINT_TOLERANCE = 4

function createDisplayRoute(controlPoints: RoutedEdgePoint[]): RoutedEdgePoint[] {
  return cleanRoute([
    sourcePoint(),
    sourceStubPoint(),
    ...controlPoints,
    targetStubPoint(),
    targetPoint(),
  ])
}

function commitRoute(nextRoute: RoutedEdgePoint[]): void {
  const displayRoute = cleanRoute([
    sourcePoint(),
    ...nextRoute.slice(1, -1).filter(point => !isCurrentEndpointStub(point)),
    targetPoint(),
  ])
  const controlPoints = displayRoute
    .slice(1, -1)
    .filter(point => !isCurrentEndpointStub(point))

  updateEdgeData<RoutedEdgeData>(props.id, { route: controlPoints, routeVersion: 2 })
}

function extractControlPoints(storedRoute: RoutedEdgePoint[] | undefined): RoutedEdgePoint[] {
  if (!Array.isArray(storedRoute)) return []

  const route = storedRoute.filter(point => isFinitePoint(point))
  const controlPoints = props.data?.routeVersion === 2
    ? route
    : route.length >= 5
    ? route.slice(2, -2)
    : route

  return controlPoints
    .filter(point => isFinitePoint(point))
    .filter(point => !isNear(point, sourcePoint()))
    .filter(point => !isNear(point, targetPoint()))
    .map(point => ({ x: snap(point.x), y: snap(point.y) }))
}

function sourcePoint(): RoutedEdgePoint {
  return { x: props.sourceX, y: props.sourceY }
}

function targetPoint(): RoutedEdgePoint {
  return { x: props.targetX, y: props.targetY }
}

function sourceStubPoint(): RoutedEdgePoint {
  return createPortStub(sourcePoint(), props.sourcePosition, getSourceFallbackDirection())
}

function targetStubPoint(): RoutedEdgePoint {
  return createPortStub(targetPoint(), props.targetPosition, getTargetFallbackDirection())
}

function createPortStub(
  point: RoutedEdgePoint,
  position: Position | undefined,
  fallback: RoutedEdgePoint,
): RoutedEdgePoint {
  const direction = getPortDirection(position) ?? fallback

  return {
    x: point.x + direction.x * SNAP,
    y: point.y + direction.y * SNAP,
  }
}

function getPortDirection(position: Position | undefined): RoutedEdgePoint | null {
  switch (position) {
    case Position.Left:
      return { x: -1, y: 0 }
    case Position.Right:
      return { x: 1, y: 0 }
    case Position.Top:
      return { x: 0, y: -1 }
    case Position.Bottom:
      return { x: 0, y: 1 }
    default:
      return null
  }
}

function getSourceFallbackDirection(): RoutedEdgePoint {
  return props.targetX >= props.sourceX ? { x: 1, y: 0 } : { x: -1, y: 0 }
}

function getTargetFallbackDirection(): RoutedEdgePoint {
  return props.targetX >= props.sourceX ? { x: -1, y: 0 } : { x: 1, y: 0 }
}

function isCurrentEndpointStub(point: RoutedEdgePoint): boolean {
  return isNear(point, sourcePoint())
    || isNear(point, sourceStubPoint())
    || isNear(point, targetStubPoint())
    || isNear(point, targetPoint())
}

function cleanRoute(points: RoutedEdgePoint[]): RoutedEdgePoint[] {
  return removeCollinearPoints(
    orthogonalizePoints(
      dedupePoints(points),
    ),
  )
}

function orthogonalizePoints(points: RoutedEdgePoint[]): RoutedEdgePoint[] {
  if (points.length <= 1) return points

  const result: RoutedEdgePoint[] = []

  points.forEach((point, index) => {
    let current = point

    if (index === 0) {
      result.push(current)
      return
    }

    const previous = result.at(-1)
    if (!previous) {
      result.push(current)
      return
    }

    const isCurrentProtected = isProtectedPoint(current)
    const xDiff = Math.abs(previous.x - current.x)
    const yDiff = Math.abs(previous.y - current.y)

    if (!isCurrentProtected && xDiff <= SNAP && yDiff > 0) {
      current = { ...current, x: previous.x }
    }

    if (!isCurrentProtected && yDiff <= SNAP && xDiff > 0) {
      current = { ...current, y: previous.y }
    }

    if (Math.abs(previous.x - current.x) >= 1 && Math.abs(previous.y - current.y) >= 1) {
      result.push(createOrthogonalBend(previous, current))
    }

    result.push(current)
  })

  return result
}

function createOrthogonalBend(from: RoutedEdgePoint, to: RoutedEdgePoint): RoutedEdgePoint {
  return { x: from.x, y: to.y }
}

function removeCollinearPoints(points: RoutedEdgePoint[]): RoutedEdgePoint[] {
  if (points.length <= 2) return points

  const result: RoutedEdgePoint[] = []

  for (let index = 0; index < points.length; index += 1) {
    const previous = result.at(-1)
    const current = points[index]
    const next = points[index + 1]
    if (!current) continue

    if (
      previous
      && next
      && !isProtectedPoint(current)
      && (
        (Math.abs(previous.x - current.x) < 1 && Math.abs(current.x - next.x) < 1)
        || (Math.abs(previous.y - current.y) < 1 && Math.abs(current.y - next.y) < 1)
      )
    ) {
      continue
    }

    result.push(current)
  }

  return dedupePoints(result)
}

function isProtectedPoint(point: RoutedEdgePoint): boolean {
  return isNear(point, sourcePoint())
    || isNear(point, sourceStubPoint())
    || isNear(point, targetStubPoint())
    || isNear(point, targetPoint())
}

function dedupePoints(points: RoutedEdgePoint[]): RoutedEdgePoint[] {
  const result: RoutedEdgePoint[] = []

  for (const point of points) {
    const previous = result.at(-1)
    if (previous && Math.abs(previous.x - point.x) < 1 && Math.abs(previous.y - point.y) < 1) {
      continue
    }
    result.push(point)
  }

  return result
}

function insertPointAtSegment(points: RoutedEdgePoint[], segmentIndex: number): RoutedEdgePoint[] {
  const from = points[segmentIndex]
  const to = points[segmentIndex + 1]
  if (!from || !to) return points

  const point = {
    x: snap((from.x + to.x) / 2),
    y: snap((from.y + to.y) / 2),
  }

  return [
    ...points.slice(0, segmentIndex + 1),
    point,
    ...points.slice(segmentIndex + 1),
  ]
}

function materializeSegmentForDrag(
  points: RoutedEdgePoint[],
  segmentIndex: number,
): { route: RoutedEdgePoint[]; index: number } {
  const from = points[segmentIndex]
  const to = points[segmentIndex + 1]
  if (!from || !to) return { route: cloneRoute(points), index: segmentIndex }

  const fromIsStub = isCurrentEndpointStub(from)
  const toIsStub = isCurrentEndpointStub(to)
  if (!fromIsStub && !toIsStub) return { route: cloneRoute(points), index: segmentIndex }

  const route = cloneRoute(points)
  let index = segmentIndex

  if (fromIsStub) {
    route.splice(index + 1, 0, { ...from })
    index += 1
  }

  if (toIsStub) {
    route.splice(index + 1, 0, { ...to })
  }

  return { route, index }
}

function createPolylinePath(points: RoutedEdgePoint[]): string {
  const first = points[0]
  if (!first) return ''

  let result = `M ${round(first.x)} ${round(first.y)}`

  for (let index = 1; index < points.length; index += 1) {
    const current = points[index]
    if (!current) continue

    result += ` L ${round(current.x)} ${round(current.y)}`
  }

  return result
}

function segmentOrientation(
  from: RoutedEdgePoint | undefined,
  to: RoutedEdgePoint | undefined,
): SegmentOrientation | null {
  if (!from || !to) return null
  if (Math.abs(from.y - to.y) < 1 && Math.abs(from.x - to.x) > 8) return 'horizontal'
  if (Math.abs(from.x - to.x) < 1 && Math.abs(from.y - to.y) > 8) return 'vertical'
  return null
}

function isNear(point: RoutedEdgePoint, target: RoutedEdgePoint): boolean {
  return Math.abs(point.x - target.x) <= ENDPOINT_TOLERANCE
    && Math.abs(point.y - target.y) <= ENDPOINT_TOLERANCE
}

function isFinitePoint(point: RoutedEdgePoint | undefined): point is RoutedEdgePoint {
  return typeof point?.x === 'number'
    && Number.isFinite(point.x)
    && typeof point.y === 'number'
    && Number.isFinite(point.y)
}

function cloneRoute(points: RoutedEdgePoint[]): RoutedEdgePoint[] {
  return points.map(point => ({ ...point }))
}

function snap(value: number): number {
  return Math.round(value / SNAP) * SNAP
}

function round(value: number): number {
  return Math.round(value * 10) / 10
}
</script>

<style scoped>
.routed-edge__flow {
  fill: none;
  stroke: color-mix(in srgb, var(--color-primary) 58%, white);
  stroke-width: 1.4;
  stroke-linecap: round;
  stroke-dasharray: 2 8;
  pointer-events: none;
  animation: routed-edge-flow 0.85s linear infinite;
}

.routed-edge__editor {
  pointer-events: all;
}

.routed-edge__segment {
  fill: none;
  stroke: transparent;
  stroke-width: 18;
  pointer-events: stroke;
}

.routed-edge__segment--horizontal {
  cursor: ns-resize;
}

.routed-edge__segment--vertical {
  cursor: ew-resize;
}

.routed-edge__handle,
.routed-edge__virtual-handle {
  stroke: var(--color-primary);
  stroke-width: 2;
  cursor: move;
}

.routed-edge__handle {
  fill: var(--color-bg-card);
}

.routed-edge__virtual-handle {
  fill: color-mix(in srgb, var(--color-primary) 18%, var(--color-bg-card));
  opacity: 0.78;
}

@keyframes routed-edge-flow {
  from {
    stroke-dashoffset: 10;
  }

  to {
    stroke-dashoffset: 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .routed-edge__flow {
    animation: none;
  }
}
</style>
