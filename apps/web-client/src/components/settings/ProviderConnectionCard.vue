<template>
  <div class="settings-card">
    <div class="settings-card__header settings-card__header--row">
      <div>
        <span class="settings-card__title">Вход через Яндекс</span>
        <span class="settings-card__desc">Подключите внешний вход, чтобы быстрее авторизоваться в системе</span>
      </div>
    </div>
    <div class="settings-card__body">
      <p v-if="error" class="settings-error">{{ error }}</p>
      <div class="settings-provider-row">
        <div class="settings-provider-row__info">
          <img src="@/assets/icons/yandex.svg" width="20" height="20" alt="Яндекс" />
          <div class="settings-provider-row__copy">
            <span class="settings-provider-row__name">Яндекс</span>
            <span class="settings-provider-row__status">
              {{ linked ? 'Подключен' : 'Не подключен' }}
            </span>
          </div>
        </div>
        <Button
          v-if="!linked"
          label="Привязать"
          severity="secondary"
          outlined
          class="settings-provider-row__btn"
          :disabled="loading"
          @click="$emit('link')"
        />
        <Button
          v-else
          label="Отвязать"
          severity="danger"
          outlined
          class="settings-provider-row__btn"
          :disabled="loading"
          @click="$emit('unlink')"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'

defineProps<{
  linked: boolean
  loading: boolean
  error: string | null
}>()

defineEmits<{
  link: []
  unlink: []
}>()
</script>

<style scoped>
.settings-provider-row__copy {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.settings-provider-row__name {
  font-size: 13px;
  color: var(--color-text);
}

.settings-provider-row__status {
  font-size: 12px;
  color: var(--color-text-secondary);
}

.settings-provider-row__btn--danger {
  color: var(--color-danger);
  border-color: var(--color-danger);
}

.settings-provider-row__btn--danger:hover:not(:disabled) {
  background: color-mix(in srgb, var(--color-danger) 8%, transparent);
}
</style>
