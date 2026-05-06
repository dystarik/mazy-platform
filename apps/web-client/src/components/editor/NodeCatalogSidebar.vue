<template>
  <aside class="editor__sidebar">
    <div v-if="loading" class="sidebar__loading">Загрузка...</div>

    <div v-else class="sidebar__nodes">
      <section
        v-for="category in categories"
        :key="category.label"
        class="sidebar__category"
      >
        <div class="sidebar__category-header">
          <span
            class="sidebar__category-icon"
            :style="{ background: category.color + '22', color: category.color }"
          >
            <img
              class="sidebar__category-icon-img"
              :class="{ 'sidebar__node-icon-img--dark': theme === 'dark' }"
              :src="category.iconSrc"
              alt=""
            />
          </span>
          <span class="sidebar__category-title">{{ category.label }}</span>
          <span class="sidebar__category-count">{{ category.nodes.length }}</span>
        </div>

        <div
          v-for="node in category.nodes"
          :key="node.type"
          class="sidebar__node"
          draggable="true"
          @dragstart="$emit('dragStart', $event, node.type ?? '')"
        >
          <span
            class="sidebar__node-icon"
            :style="{ background: category.color + '22', color: category.color }"
          >
            <img
              class="sidebar__node-icon-img"
              :class="{ 'sidebar__node-icon-img--dark': theme === 'dark' }"
              :src="category.iconSrc"
              alt=""
            />
          </span>
          <span class="sidebar__node-label">{{ getNodeLabel(node.type ?? '') }}</span>
        </div>
      </section>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { useTheme } from '@/composables/useTheme'
import type { NodeCatalogItem } from '@/types/api'
import { getNodeLabel } from '@/components/editor/nodes/nodeMeta'

interface SidebarCategory {
  label: string
  color: string
  iconSrc: string
  nodes: NodeCatalogItem[]
}

defineProps<{
  loading: boolean
  categories: SidebarCategory[]
}>()

defineEmits<{
  dragStart: [event: DragEvent, type: string]
}>()

const { theme } = useTheme()
</script>

<style scoped>
.editor__sidebar {
  width: 286px;
  flex-shrink: 0;
  border-right: 0.5px solid var(--color-border);
  background: var(--color-bg-card);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar__loading {
  padding: 20px 16px;
  font-size: 14px;
  color: var(--color-text-secondary);
}

.sidebar__nodes {
  flex: 1;
  overflow-y: auto;
  padding: 10px 0 14px;
}

.sidebar__category + .sidebar__category {
  margin-top: 10px;
}

.sidebar__category-header {
  display: grid;
  grid-template-columns: 24px minmax(0, 1fr) auto;
  align-items: center;
  gap: 8px;
  padding: 6px 16px 7px;
}

.sidebar__category-icon {
  width: 24px;
  height: 24px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.sidebar__category-icon-img {
  width: 13px;
  height: 13px;
  display: block;
  opacity: 0.78;
}

.sidebar__category-title {
  min-width: 0;
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar__category-count {
  color: var(--color-text-muted);
  font-size: 11px;
  font-weight: 700;
}

.sidebar__node {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 24px;
  cursor: grab;
  transition: background 0.15s;
  user-select: none;
}

.sidebar__node:hover {
  background: var(--color-bg-secondary);
}

.sidebar__node:active {
  cursor: grabbing;
}

.sidebar__node-icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.sidebar__node-icon-img {
  width: 15px;
  height: 15px;
  display: block;
  opacity: 0.82;
}

.sidebar__node-icon-img--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.9;
}

.sidebar__node-label {
  font-size: 14px;
  color: var(--color-text);
  line-height: 1.3;
}
</style>
