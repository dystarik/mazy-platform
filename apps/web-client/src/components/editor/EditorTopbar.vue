<template>
  <div class="editor__topbar">
    <div class="editor__topbar-left">
      <Button
        v-if="activeGroupTitle"
        class="btn-secondary editor__toolbar-btn editor__back"
        type="button"
        @click="$emit('exitGroup')"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
          <path d="M19 12H5M12 5l-7 7 7 7"/>
        </svg>
        К сценарию
      </Button>
      <router-link
        v-else
        :to="{ name: 'project', params: { id: projectId } }"
        class="btn-secondary editor__toolbar-btn editor__back"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
          <path d="M19 12H5M12 5l-7 7 7 7"/>
        </svg>
        К проекту
      </router-link>
      <span v-if="scenarioModeLabel" class="editor__mode-badge">
        {{ scenarioModeLabel }}
      </span>
      <span v-if="activeGroupTitle" class="editor__mode-badge">
        Группа: {{ activeGroupTitle }}
      </span>
      <div v-if="!isReadOnly" class="editor__history-actions" aria-label="История изменений">
        <Button
          class="btn-secondary editor__toolbar-btn editor__icon-btn editor__history-btn"
          type="button"
          :disabled="!canUndo"
          title="Отменить (Ctrl+Z)"
          @click="$emit('undo')"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
            <path d="M9 14 4 9l5-5"/>
            <path d="M4 9h11a5 5 0 0 1 0 10h-4"/>
          </svg>
        </Button>
        <Button
          class="btn-secondary editor__toolbar-btn editor__icon-btn editor__history-btn"
          type="button"
          :disabled="!canRedo"
          title="Повторить (Ctrl+Y)"
          @click="$emit('redo')"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
            <path d="m15 14 5-5-5-5"/>
            <path d="M20 9H9a5 5 0 0 0 0 10h4"/>
          </svg>
        </Button>
      </div>
    </div>

    <div class="editor__topbar-right">
      <span v-if="saveStatus === 'saved'" class="editor__save-status editor__save-status--ok">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 6L9 17l-5-5"/>
        </svg>
        Сохранено
      </span>
      <span v-else-if="saveStatus === 'imported'" class="editor__save-status editor__save-status--ok">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 6L9 17l-5-5"/>
        </svg>
        Импортировано
      </span>
      <span v-else-if="saveStatus === 'exported'" class="editor__save-status editor__save-status--ok">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 6L9 17l-5-5"/>
        </svg>
        {{ statusMessage || 'JSON скачан' }}
      </span>
      <span v-else-if="saveStatus === 'validated'" class="editor__save-status editor__save-status--ok">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 6L9 17l-5-5"/>
        </svg>
        {{ statusMessage || 'Ошибок не найдено' }}
      </span>
      <span v-else-if="saveStatus === 'error'" class="editor__save-status editor__save-status--err">
        {{ statusMessage || 'Ошибка сохранения' }}
      </span>

      <input
        ref="importInputRef"
        class="editor__file-input"
        type="file"
        accept="application/json,.json"
        @change="$emit('importJson', $event)"
      />
      <Button
        class="btn-secondary editor__toolbar-btn"
        type="button"
        :disabled="nodesCount < 2"
        title="Автоматически расположить узлы"
        @click="$emit('autoLayout')"
      >
        <span>Разложить</span>
      </Button>
      <Button
        class="btn-secondary editor__toolbar-btn"
        type="button"
        :disabled="nodesCount === 0"
        @click="$emit('exportJson')"
      >
        <span>Экспорт</span>
      </Button>
      <Button
        v-if="!isReadOnly"
        class="btn-secondary editor__toolbar-btn"
        type="button"
        @click="importInputRef?.click()"
      >
        <span>Импорт</span>
      </Button>
      <Button
        class="btn-secondary editor__toolbar-btn"
        type="button"
        @click="$emit('exportSchema')"
      >
        <span>Получить схему</span>
      </Button>
      <Button
        v-if="canEditCopy"
        class="btn-secondary editor__toolbar-btn"
        type="button"
        @click="$emit('editCopy')"
      >
        <span>Редактировать копию</span>
      </Button>
      <Button
        v-if="!isReadOnly"
        class="btn-secondary editor__toolbar-btn"
        type="button"
        :loading="checkingDraft"
        :disabled="saving || checkingDraft"
        @click="$emit('validateDraft')"
      >
        <span>{{ checkingDraft ? 'Проверяю...' : 'Проверить' }}</span>
      </Button>
      <Button
        v-if="!isReadOnly"
        class="btn-primary editor__save-btn"
        type="button"
        :loading="saving"
        :disabled="saving || checkingDraft"
        @click="$emit('save')"
      >
        <span>{{ saving ? 'Сохраняю...' : 'Сохранить' }}</span>
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'

defineProps<{
  projectId: string
  isReadOnly: boolean
  scenarioModeLabel: string
  saveStatus: 'idle' | 'saved' | 'imported' | 'exported' | 'validated' | 'error'
  statusMessage: string
  saving: boolean
  checkingDraft: boolean
  nodesCount: number
  canUndo: boolean
  canRedo: boolean
  canEditCopy: boolean
  activeGroupTitle: string
}>()

defineEmits<{
  importJson: [event: Event]
  exportJson: []
  exportSchema: []
  autoLayout: []
  undo: []
  redo: []
  exitGroup: []
  editCopy: []
  validateDraft: []
  save: []
}>()

const importInputRef = ref<HTMLInputElement | null>(null)
</script>

<style scoped>
.editor__topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  height: 52px;
  border-bottom: 0.5px solid var(--color-border);
  background: var(--color-bg-card);
  flex-shrink: 0;
  gap: 16px;
}

.editor__topbar-left,
.editor__topbar-right {
  display: flex;
  align-items: center;
}

.editor__topbar-left {
  gap: 10px;
}

.editor__topbar-right {
  gap: 12px;
}

.editor__back {
  text-decoration: none;
}

.editor__back svg,
.editor__icon-btn svg {
  width: 20px;
  height: 20px;
  flex: 0 0 20px;
}

.editor__mode-badge {
  display: inline-flex;
  align-items: center;
  min-height: 22px;
  padding: 3px 8px;
  border-radius: 999px;
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 500;
}

.editor__save-status {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
}

.editor__save-status--ok {
  color: #2ecc71;
}

.editor__save-status--err {
  color: var(--color-danger);
}

.editor__save-btn,
.editor__toolbar-btn {
  width: auto;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  font-size: 14px;
  white-space: nowrap;
}

.editor__history-actions {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.editor__icon-btn {
  width: 40px;
  min-width: 40px;
  min-height: 34px;
  padding: 0;
}

.editor__history-btn {
  border: 1px solid var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text-secondary);
  box-shadow: none;
}

.editor__history-btn:hover:not(:disabled) {
  border-color: var(--color-primary);
  background: var(--color-bg-card);
  color: var(--color-primary);
}

.editor__history-btn:disabled {
  border-color: var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text-muted);
  opacity: 1;
}

.editor__save-btn {
  padding: 8px 18px;
}

.editor__toolbar-btn {
  min-height: 34px;
  padding: 8px 14px;
}

.editor__toolbar-btn:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.editor__file-input {
  display: none;
}
</style>
