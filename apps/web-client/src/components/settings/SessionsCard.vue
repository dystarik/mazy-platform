<template>
  <div class="settings-card">
    <div class="settings-card__header settings-card__header--row">
      <span class="settings-card__title">Активные сессии</span>
      <Button
        label="Завершить все"
        severity="secondary"
        outlined
        class="settings-card__action-btn"
        :disabled="loading"
        @click="$emit('openLogoutAll')"
      />
    </div>

    <div v-if="loading" class="settings-card__body sessions-loading">
      Загрузка...
    </div>

    <div v-else-if="error" class="settings-card__body">
      <p class="settings-error">{{ error }}</p>
    </div>

    <div v-else class="settings-card__body settings-card__body--list">
      <div
        v-for="session in sessions"
        :key="session.refreshTokenId"
        class="session-row"
      >
        <div class="session-row__icon" :class="{ 'session-row__icon--current': session.isCurrent }">
          <img
            class="session-row__icon-image"
            :class="{ 'session-row__icon-image--dark': theme === 'dark' }"
            :src="monitorIcon"
            alt=""
          />
        </div>
        <div class="session-row__info">
          <span class="session-row__name" :class="{ 'session-row__name--current': session.isCurrent }">
            {{ session.isCurrent ? 'Текущий сеанс' : 'Сеанс' }}
          </span>
          <span class="session-row__date">Создан {{ formatDate(session.createdAt) }}</span>
        </div>
        <Button
          v-if="session.isCurrent"
          label="Завершить"
          text
          severity="danger"
          class="session-row__logout"
          :disabled="loading || !session.refreshTokenId"
          @click="session.refreshTokenId && $emit('logoutCurrent', session.refreshTokenId)"
        />
        <Button
          v-else
          label="Завершить"
          text
          class="session-row__logout"
          :disabled="loading || !session.refreshTokenId"
          @click="session.refreshTokenId && $emit('logoutSession', session.refreshTokenId)"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import type { SessionInfo } from '@/types/api'
import monitorIcon from '@/assets/icons/monitor.svg'

defineProps<{
  sessions: SessionInfo[]
  loading: boolean
  error: string | null
  theme: string
}>()

defineEmits<{
  openLogoutAll: []
  logoutSession: [refreshTokenId: string]
  logoutCurrent: [refreshTokenId: string]
}>()

function formatDate(dateStr?: string): string {
  if (!dateStr) return '—'
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(dateStr))
}
</script>

<style scoped>
.sessions-loading {
  color: var(--color-text-secondary);
  font-size: 13px;
}

.session-row__icon--current {
  color: var(--color-primary);
}

.session-row__icon-image {
  width: 18px;
  height: 18px;
  display: block;
}

.session-row__icon-image--dark {
  filter: invert(1) brightness(1.35);
}

.session-row__name--current {
  color: var(--color-primary);
}

.session-row__logout--danger {
  color: var(--color-danger);
}

.session-row__logout--danger:hover:not(:disabled) {
  color: var(--color-danger-hover);
}
</style>
