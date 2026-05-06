<template>
  <div class="settings-subsection">
    <div class="settings-subsection__header">
      <h3 class="settings-subsection__title">Многофакторная аутентификация</h3>
      <p class="settings-subsection__desc">
        Выберите способы подтверждения входа и управляйте резервными кодами
      </p>
    </div>

    <div class="settings-stack">
      <div class="settings-card">
        <div class="settings-card__header settings-card__header--row">
          <div>
            <span class="settings-card__title">Приложение-аутентификатор</span>
            <span class="settings-card__desc">Google Authenticator, Yandex Key и другие</span>
          </div>
          <Tag :value="hasTotp ? 'Вкл' : 'Выкл'" :severity="hasTotp ? 'success' : 'secondary'" />
        </div>
        <div class="settings-card__body">
          <Button
            v-if="!hasTotp"
            label="Подключить"
            severity="secondary"
            outlined
            :disabled="loading"
            @click="$emit('addTotp')"
          />
          <Button
            v-else
            label="Отключить"
            severity="danger"
            outlined
            :disabled="loading"
            @click="$emit('removeFactor', 'MFA_FACTOR_TYPE_TOTP')"
          />
        </div>
      </div>

      <div class="settings-card">
        <div class="settings-card__header settings-card__header--row">
          <div>
            <span class="settings-card__title">Почта</span>
            <span class="settings-card__desc">Одноразовый код будет отправлен на ваш email</span>
          </div>
          <Tag :value="hasEmail ? 'Вкл' : 'Выкл'" :severity="hasEmail ? 'success' : 'secondary'" />
        </div>
        <div class="settings-card__body">
          <Button
            v-if="!hasEmail"
            label="Подключить"
            severity="secondary"
            outlined
            :disabled="loading"
            @click="$emit('addEmail')"
          />
          <Button
            v-else
            label="Отключить"
            severity="danger"
            outlined
            :disabled="loading"
            @click="$emit('removeFactor', 'MFA_FACTOR_TYPE_EMAIL')"
          />
        </div>
      </div>

      <div class="settings-card">
        <div class="settings-card__header settings-card__header--row">
          <div>
            <span class="settings-card__title">Резервные коды</span>
            <span class="settings-card__desc">Одноразовые коды для входа при потере доступа к факторам</span>
          </div>
        </div>
        <div class="settings-card__body">
          <Button
            label="Перегенерировать"
            severity="secondary"
            outlined
            :disabled="loading || !hasAnyFactor"
            @click="$emit('regenerate')"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import type { MfaFactorType } from '@/types/api'

defineProps<{
  hasTotp: boolean
  hasEmail: boolean
  hasAnyFactor: boolean
  loading: boolean
}>()

defineEmits<{
  addTotp: []
  addEmail: []
  removeFactor: [factor: MfaFactorType]
  regenerate: []
}>()
</script>

<style scoped>
.settings-subsection {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.settings-subsection__header {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.settings-subsection__title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text);
}

.settings-subsection__desc {
  margin: 0;
  font-size: 12px;
  line-height: 1.45;
  color: var(--color-text-secondary);
}

.settings-stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
