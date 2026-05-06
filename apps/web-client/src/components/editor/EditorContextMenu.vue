<template>
  <div
    v-if="menu"
    class="editor-menu"
    :style="{ left: `${menu.x}px`, top: `${menu.y}px` }"
    @click.stop
    @contextmenu.prevent.stop
  >
    <template v-if="menu.type === 'node'">
      <Button v-if="canOpenGroup" class="editor-menu__item" label="Открыть группу" text @click="$emit('openGroup')" />
      <Button class="editor-menu__item" label="Сделать стартовым" text @click="$emit('setNodeStart')" />
      <Button class="editor-menu__item" label="Скопировать" text @click="$emit('copyNode')" />
      <Button class="editor-menu__item" label="Удалить" text severity="danger" @click="$emit('deleteNode')" />
    </template>

    <template v-else-if="menu.type === 'selection'">
      <Button class="editor-menu__item" label="Сгруппировать" text @click="$emit('groupSelection')" />
      <Button class="editor-menu__item" label="Скопировать" text @click="$emit('copySelection')" />
      <Button class="editor-menu__item" label="Удалить" text severity="danger" @click="$emit('deleteSelection')" />
    </template>

    <template v-else-if="menu.type === 'pane'">
      <div class="editor-menu__item editor-menu__item--submenu">
        <span>Добавить</span>
        <span class="editor-menu__chevron">›</span>

        <div class="editor-menu__submenu editor-menu__submenu--categories">
          <div
            v-for="category in categories"
            :key="category.label"
            class="editor-menu__item editor-menu__item--submenu"
          >
            <span class="editor-menu__category">
              <span
                class="editor-menu__category-icon"
                :style="{ background: category.color + '22', color: category.color }"
              >
                <img class="editor-menu__category-icon-img" :src="category.iconSrc" alt="" />
              </span>
              <span>{{ category.label }}</span>
            </span>
            <span class="editor-menu__chevron">›</span>

            <div class="editor-menu__submenu editor-menu__submenu--nodes">
              <Button
                v-for="node in category.nodes"
                :key="node.type"
                class="editor-menu__item"
                :label="getNodeLabel(node.type ?? '')"
                text
                @click="$emit('addNode', node.type ?? '')"
              />
            </div>
          </div>
        </div>
      </div>
      <Button class="editor-menu__item" label="Вставить" text :disabled="!hasClipboard" @click="$emit('paste')" />
    </template>

    <template v-else-if="menu.type === 'edge'">
      <Button class="editor-menu__item" label="Удалить связь" text severity="danger" @click="$emit('deleteEdge')" />
    </template>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import type { NodeCatalogItem } from '@/types/api'
import { getNodeLabel } from '@/components/editor/nodes/nodeMeta'

interface MenuCategory {
  label: string
  color: string
  iconSrc: string
  nodes: NodeCatalogItem[]
}

export type EditorMenu =
  | { type: 'node'; x: number; y: number; nodeId: string }
  | { type: 'selection'; x: number; y: number }
  | { type: 'pane'; x: number; y: number; position: { x: number; y: number } }
  | { type: 'edge'; x: number; y: number; edgeId: string }

defineProps<{
  menu: EditorMenu | null
  hasClipboard: boolean
  canOpenGroup: boolean
  categories: MenuCategory[]
}>()

defineEmits<{
  openGroup: []
  setNodeStart: []
  copyNode: []
  deleteNode: []
  groupSelection: []
  copySelection: []
  deleteSelection: []
  addNode: [type: string]
  paste: []
  deleteEdge: []
}>()
</script>

<style scoped>
.editor-menu {
  position: fixed;
  z-index: 2000;
  min-width: 178px;
  padding: 6px;
  border: 0.5px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-card);
  box-shadow: 0 14px 36px rgba(0, 0, 0, 0.18);
}

.editor-menu::before {
  content: '';
  position: absolute;
  inset: -8px;
  z-index: -1;
  background: transparent;
}

.editor-menu__item {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  width: 100%;
  min-height: 32px;
  padding: 7px 10px;
  border: none;
  border-radius: 7px;
  background: none;
  color: var(--color-text);
  cursor: pointer;
  font-family: inherit;
  font-size: 13px;
  text-align: left;
  transition: background 0.15s, color 0.15s, opacity 0.15s;
}

.editor-menu__item--submenu {
  justify-content: space-between;
}

.editor-menu__item:hover:not(:disabled) {
  background: var(--color-bg-secondary);
}

.editor-menu__item:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.editor-menu__item--danger {
  color: var(--color-danger);
}

.editor-menu__item--danger:hover {
  background: color-mix(in srgb, var(--color-danger) 10%, transparent);
}

.editor-menu__chevron {
  margin-left: 14px;
  color: var(--color-text-muted);
  font-size: 17px;
  line-height: 1;
}

.editor-menu__submenu {
  position: absolute;
  top: -6px;
  left: calc(100% - 1px);
  display: none;
  min-width: 204px;
  overflow: visible;
  padding: 6px;
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-bg-card);
  box-shadow: 0 14px 36px rgba(0, 0, 0, 0.18);
}

.editor-menu__submenu::before {
  content: '';
  position: absolute;
  inset: -8px;
  z-index: -1;
  background: transparent;
}

.editor-menu__item--submenu:hover > .editor-menu__submenu {
  display: block;
}

.editor-menu__submenu--nodes {
  min-width: 230px;
  width: max-content;
  max-width: min(320px, calc(100vw - 32px));
  max-height: none;
  overflow: visible;
}

.editor-menu__submenu--nodes .editor-menu__item {
  min-width: 0;
  white-space: nowrap;
}

.editor-menu__category {
  display: inline-flex;
  min-width: 0;
  align-items: center;
  gap: 8px;
}

.editor-menu__category-icon {
  width: 22px;
  height: 22px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.editor-menu__category-icon-img {
  width: 13px;
  height: 13px;
  display: block;
  opacity: 0.84;
}
</style>
