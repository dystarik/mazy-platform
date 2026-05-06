<template>
  <Teleport to="body">
    <div v-if="pageState !== null" class="modal-overlay" @click.self="$emit('cancel')">
      <div class="modal modal--mfa">
        <div v-if="pageState === 'add-email-form'" class="settings-card settings-card--modal settings-card--modal-left">
          <div class="settings-card__header settings-card__header--modal settings-card__header--modal-left">
            <button class="modal-close" type="button" @click="$emit('cancel')">
              <img class="modal-close__icon" :class="{ 'modal-close__icon--dark': theme === 'dark' }" :src="xIcon" alt="" />
            </button>
            <span class="settings-card__title">Почта</span>
            <span class="settings-card__desc">Можно использовать другой email для получения кодов</span>
          </div>
          <div class="settings-card__body settings-card__body--form settings-card__body--modal-centered">
            <div class="field field--modal-left">
              <label>Email для кодов (необязательно)</label>
              <InputText
                v-model="emailModel"
                type="email"
                placeholder="Оставьте пустым для основного email"
              />
            </div>
            <div v-if="errors.length" class="settings-error">
              <p v-for="(msg, i) in errors" :key="i">{{ msg }}</p>
            </div>
            <div class="mfa-actions mfa-actions--single">
              <Button :label="loading ? 'Отправляю...' : 'Подключить'" :loading="loading" @click="$emit('addEmail')" />
            </div>
          </div>
        </div>

        <div v-else-if="pageState === 'mfa-challenge'" class="settings-card settings-card--modal">
          <div class="settings-card__header settings-card__header--modal">
            <button class="modal-close" type="button" @click="$emit('cancel')">
              <img class="modal-close__icon" :class="{ 'modal-close__icon--dark': theme === 'dark' }" :src="xIcon" alt="" />
            </button>
            <span class="settings-card__title">Подтвердите действие</span>
            <span class="settings-card__desc">Для безопасности нужно подтвердить вашу личность</span>
          </div>
          <div class="settings-card__body settings-card__body--form settings-card__body--modal-centered">
            <template v-if="step === 'select_factor'">
              <div class="mfa-challenge-panel">
                <p v-if="usedFactors.length > 0" class="mfa-progress">
                  ✓ Фактор подтверждён, требуется ещё один
                </p>
                <div class="mfa-factors">
                  <Button
                    v-for="factor in remainingFactors"
                    :key="factor"
                    :label="factorLabel(factor)"
                    severity="secondary"
                    outlined
                    :disabled="loading"
                    @click="$emit('selectFactor', factor)"
                  />
                </div>
                <template v-if="!usedFactors.includes('MFA_FACTOR_TYPE_BACKUP_CODE')">
                  <div class="divider"><span>или</span></div>
                  <Button
                    label="Использовать резервный код"
                    severity="secondary"
                    text
                    :disabled="loading"
                    @click="$emit('selectFactor', 'MFA_FACTOR_TYPE_BACKUP_CODE')"
                  />
                </template>
              </div>
            </template>

            <template v-else-if="step === 'enter_code'">
              <div
                class="mfa-challenge-panel"
                :class="{ 'mfa-challenge-panel--email': selectedFactor === 'MFA_FACTOR_TYPE_EMAIL' }"
              >
                <p class="mfa-hint">{{ factorHint(selectedFactor) }}</p>
                <div class="field field--otp-centered">
                  <InputOtp
                    v-if="selectedFactor !== 'MFA_FACTOR_TYPE_BACKUP_CODE'"
                    v-model="codeModel"
                    :length="6"
                  />
                  <InputText
                    v-else
                    v-model="codeModel"
                    type="text"
                    class="code"
                    placeholder="ea64b3"
                    autocomplete="off"
                  />
                </div>
                <div v-if="errors.length" class="settings-error">
                  <p v-for="(msg, i) in errors" :key="i">{{ msg }}</p>
                </div>
                <Button
                  v-if="selectedFactor === 'MFA_FACTOR_TYPE_EMAIL'"
                  label="Отправить повторно"
                  text
                  class="settings-inline-link--centered"
                  :disabled="loading"
                  @click="$emit('resendEmail')"
                />
                <div class="mfa-actions mfa-actions--single">
                  <Button :label="loading ? 'Проверяю...' : 'Подтвердить'" :loading="loading" @click="$emit('verifyChallenge')" />
                </div>
              </div>
            </template>
          </div>
        </div>

        <div v-else-if="pageState === 'add-totp-qr'" class="settings-card settings-card--modal settings-card--modal-centered">
          <div class="settings-card__header settings-card__header--modal settings-card__header--modal-left">
            <button class="modal-close" type="button" @click="$emit('cancel')">
              <img class="modal-close__icon" :class="{ 'modal-close__icon--dark': theme === 'dark' }" :src="xIcon" alt="" />
            </button>
            <span class="settings-card__title">Приложение-аутентификатор</span>
            <span class="settings-card__desc">Отсканируйте QR-код в приложении, затем введите код</span>
          </div>
          <div class="settings-card__body settings-card__body--form settings-card__body--modal-centered">
            <div v-if="qrDataUrl" class="totp-qr">
              <img :src="qrDataUrl" alt="QR-код" width="180" height="180" />
            </div>
            <div class="field field--otp-centered">
              <label>Код из приложения</label>
              <InputOtp v-model="codeModel" :length="6" />
            </div>
            <div v-if="errors.length" class="settings-error">
              <p v-for="(msg, i) in errors" :key="i">{{ msg }}</p>
            </div>
            <div class="mfa-actions mfa-actions--single">
              <Button :label="loading ? 'Проверяю...' : 'Подтвердить'" :loading="loading" @click="$emit('verifyTotp')" />
            </div>
          </div>
        </div>

        <div v-else-if="pageState === 'add-email-code'" class="settings-card settings-card--modal settings-card--modal-left">
          <div class="settings-card__header settings-card__header--modal settings-card__header--modal-left">
            <button class="modal-close" type="button" @click="$emit('cancel')">
              <img class="modal-close__icon" :class="{ 'modal-close__icon--dark': theme === 'dark' }" :src="xIcon" alt="" />
            </button>
            <span class="settings-card__title">Почта</span>
            <span class="settings-card__desc">Мы отправили код на {{ emailForFactor || 'ваш email' }}</span>
          </div>
          <div class="settings-card__body settings-card__body--form settings-card__body--modal-centered">
            <div class="field field--modal-left">
              <label>Код из письма</label>
              <InputOtp v-model="codeModel" :length="6" />
            </div>
            <div v-if="errors.length" class="settings-error">
              <p v-for="(msg, i) in errors" :key="i">{{ msg }}</p>
            </div>
            <div class="mfa-actions mfa-actions--single">
              <Button :label="loading ? 'Проверяю...' : 'Подтвердить'" :loading="loading" @click="$emit('verifyEmail')" />
            </div>
          </div>
        </div>

        <div v-else-if="pageState === 'backup-codes'" class="settings-card settings-card--modal">
          <div class="settings-card__header settings-card__header--modal">
            <button class="modal-close" type="button" @click="$emit('cancel')">
              <img class="modal-close__icon" :class="{ 'modal-close__icon--dark': theme === 'dark' }" :src="xIcon" alt="" />
            </button>
            <div class="settings-card__header-copy">
              <span class="settings-card__title">Резервные коды</span>
              <span class="settings-card__desc">Сохраните их в надёжном месте — каждый код одноразовый</span>
            </div>
          </div>
          <div class="settings-card__body">
            <div class="backup-codes-list">
              <code
                v-for="(backupCode, i) in backupCodes"
                :key="i"
                class="backup-code"
              >{{ backupCode }}</code>
            </div>
            <div class="backup-codes-actions">
              <Button
                :label="copied ? 'Скопировано' : 'Скопировать'"
                severity="secondary"
                outlined
                class="settings-card__action-btn"
                @click="$emit('copyBackupCodes')"
              />
              <Button
                label="Готово"
                severity="secondary"
                outlined
                class="settings-card__action-btn"
                @click="$emit('cancel')"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputOtp from 'primevue/inputotp'
import type { MfaFactorType } from '@/types/api'
import type { MfaPageState } from '@/composables/useSettingsMfaFlow'
import xIcon from '@/assets/icons/x.svg'

type MfaStep = 'select_factor' | 'enter_code' | 'completed'

const props = defineProps<{
  pageState: MfaPageState
  code: string
  emailForFactor: string
  errors: string[]
  loading: boolean
  usedFactors: MfaFactorType[]
  remainingFactors: MfaFactorType[]
  qrDataUrl: string | null
  backupCodes: string[]
  copied: boolean
  step: MfaStep
  selectedFactor: MfaFactorType | null
  theme: string
  factorLabel: (factor: MfaFactorType) => string
  factorHint: (factor: MfaFactorType | null) => string
}>()

const emit = defineEmits<{
  'update:code': [value: string]
  'update:emailForFactor': [value: string]
  addEmail: []
  verifyTotp: []
  verifyEmail: []
  selectFactor: [factor: MfaFactorType]
  verifyChallenge: []
  resendEmail: []
  copyBackupCodes: []
  cancel: []
}>()

const codeModel = computed({
  get: () => props.code,
  set: (value: string) => emit('update:code', value),
})

const emailModel = computed({
  get: () => props.emailForFactor,
  set: (value: string) => emit('update:emailForFactor', value),
})
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: var(--color-bg-card);
  border: 0.5px solid var(--color-border);
  border-radius: 14px;
  padding: 28px;
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.modal--mfa {
  max-width: 460px;
  padding: 0;
  background: transparent;
  border: none;
}

.settings-card--modal {
  width: 100%;
}

.settings-card--modal-centered .settings-card__header {
  align-items: center;
  text-align: center;
}

.settings-card__header--modal-left {
  align-items: flex-start;
  text-align: left;
}

.settings-card--modal-centered .settings-card__header--modal-left {
  align-items: flex-start;
  text-align: left;
}

.settings-card__header--modal {
  position: relative;
}

.settings-card__header-copy {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.settings-card__body--modal-centered {
  align-items: center;
  max-width: 100%;
}

.settings-card__body--modal-centered .field {
  width: 100%;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.settings-card__body--modal-centered .field label {
  align-self: center;
  text-align: center;
}

.settings-card__body--modal-centered .field--modal-left {
  align-items: stretch;
}

.settings-card__body--modal-centered .field--modal-left label {
  align-self: flex-start;
  text-align: left;
}

.settings-card__body--modal-centered .field--otp-centered {
  align-items: center;
}

.settings-card__body--modal-centered .field input,
.settings-card__body--modal-centered .field :deep(.p-inputotp),
.settings-card__body--modal-centered .field :deep(.p-inputotp-input) {
  width: 100%;
}

.settings-card__body--modal-centered .field :deep(.p-inputotp) {
  justify-content: center;
  gap: 8px;
}

.settings-card__body--modal-centered .field :deep(.p-inputotp-input) {
  width: 42px;
  min-height: 42px;
}

.settings-card__body--modal-centered .mfa-actions {
  width: 100%;
  max-width: 100%;
}

.settings-card__body--modal-centered .mfa-factors,
.settings-card__body--modal-centered .mfa-progress,
.settings-card__body--modal-centered .mfa-hint,
.settings-card__body--modal-centered .settings-error {
  width: 100%;
  max-width: 320px;
}

.mfa-challenge-panel {
  width: 100%;
  max-width: 360px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.mfa-challenge-panel > .factor-btn,
.mfa-challenge-panel > .divider,
.mfa-challenge-panel > .mfa-actions,
.mfa-challenge-panel > .settings-inline-link--centered,
.mfa-challenge-panel .mfa-hint,
.mfa-challenge-panel .settings-error,
.mfa-challenge-panel .field,
.mfa-challenge-panel .mfa-factors {
  width: 100%;
  max-width: 100%;
}

.mfa-challenge-panel .mfa-hint {
  text-align: left;
}

.mfa-challenge-panel .field label {
  align-self: flex-start;
  text-align: left;
}

.mfa-challenge-panel .mfa-actions {
  width: 100%;
  max-width: 100%;
}

.mfa-challenge-panel--email .mfa-hint {
  text-align: center;
}

.mfa-challenge-panel--email .field label {
  align-self: center;
  text-align: center;
}

.mfa-challenge-panel .mfa-factors {
  align-items: stretch;
}

.mfa-challenge-panel .factor-btn {
  text-align: center;
  width: 100%;
}

.mfa-actions--single {
  grid-template-columns: 1fr;
}

.backup-codes-actions {
  margin-top: 16px;
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.modal-close {
  position: absolute;
  top: 14px;
  right: 14px;
  width: 28px;
  height: 28px;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.15s ease;
}

.modal-close:hover {
  background: var(--color-bg-secondary);
}

.modal-close__icon {
  width: 14px;
  height: 14px;
  display: block;
}

.modal-close__icon--dark {
  filter: invert(1) brightness(1.35);
}

.settings-inline-link--centered {
  align-self: center;
}
</style>
