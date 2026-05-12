import {
  EDITOR_NODE_GRID_SIZE,
  describeEditorNodeLayout,
  toEditorNodeLayoutMetrics,
} from '@/components/editor/editorNodeLayoutContract'
import type { EditorNodeData } from '@/components/editor/editorTypes'

export const EDITOR_GRID_SIZE = EDITOR_NODE_GRID_SIZE

export interface EditorNodeLayoutMetrics {
  width: number
  height: number
  inputPortY: number
  outputPortYs: number[]
}

export function getEditorNodeLayoutMetrics(data: EditorNodeData): EditorNodeLayoutMetrics {
  return toEditorNodeLayoutMetrics(describeEditorNodeLayout(data))
}
