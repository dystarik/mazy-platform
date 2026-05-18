<template>
  <AuthCard :title="title">
    <template #subtitle>
      {{ subtitle }}
    </template>

      <!-- Выбор фактора -->
      <div v-if="mfaStore.step === 'select_factor'" class="mfa__factors">
        <Button
          v-for="factor in mfaStore.availableFactors"
          :key="factor"
          :label="factorLabel(factor)"
          severity="secondary"
          outlined
          @click="handleSelectFactor(factor)"
        />
        <div class="divider"><span>или</span></div>
        <Button
          label="Использовать резервный код"
          severity="secondary"
          text
          @click="handleSelectFactor('MFA_FACTOR_TYPE_BACKUP_CODE')"
        />
      </div>

      <!-- Ввод кода -->
      <form v-if="mfaStore.step === 'enter_code'" class="auth-card__form" @submit.prevent="handleVerify">
        <p class="mfa__hint">{{ factorHint(mfaStore.selectedFactor) }}</p>

        <div class="field">
          <label>Код</label>
          <InputOtp
            v-if="mfaStore.selectedFactor !== 'MFA_FACTOR_TYPE_BACKUP_CODE'"
            v-model="code"
            :length="6"
          />
          <InputText
            v-else
            v-model="code"
            type="text"
            class="code"
            placeholder="ea64b3"
            autocomplete="off"
          />
          <Button
            v-if="mfaStore.selectedFactor === 'MFA_FACTOR_TYPE_EMAIL'"
            type="button"
            class="mfa__resend"
            :label="isResending ? 'Отправляю...' : 'Отправить код повторно'"
            text
            :disabled="isResending"
            :loading="isResending"
            @click="handleResend"
          />
        </div>

        <FormErrorList :messages="errors" />

        <Button type="submit" :label="isLoading ? 'Проверяю...' : 'Подтвердить'" :loading="isLoading" />
      </form>
  </AuthCard>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputOtp from 'primevue/inputotp'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { parseApiError } from '@/composables/useApiError'
import { useMfaStore } from '@/stores/mfa.store'
import { useAuthStore } from '@/stores/auth.store'
import type { MfaFactorType } from '@/types/api'

const router = useRouter()
const mfaStore = useMfaStore()

const code = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)
const isResending = ref(false)

const title = computed(() => {
  switch (mfaStore.context) {
    case 'reset_password': return 'Подтверждение сброса пароля'
    default: return 'Подтверждение входа'
  }
})

const subtitle = computed(() => {
  switch (mfaStore.context) {
    case 'reset_password': return 'Выберите способ подтверждения для смены пароля'
    default: return 'Выберите способ подтверждения'
  }
})

function factorLabel(factor: MfaFactorType): string {
  switch (factor) {
    case 'MFA_FACTOR_TYPE_TOTP': return 'Приложение-аутентификатор'
    case 'MFA_FACTOR_TYPE_EMAIL': return 'Отправить код на почту'
    case 'MFA_FACTOR_TYPE_BACKUP_CODE': return 'Резервный код'
    default: return factor
  }
}

function factorHint(factor: MfaFactorType | null): string {
  switch (factor) {
    case 'MFA_FACTOR_TYPE_TOTP': return 'Введите код из приложения-аутентификатора'
    case 'MFA_FACTOR_TYPE_EMAIL': return 'Мы отправили код на вашу почту'
    case 'MFA_FACTOR_TYPE_BACKUP_CODE': return 'Введите один из резервных кодов'
    default: return ''
  }
}

async function handleSelectFactor(factor: MfaFactorType) {
  try {
    await mfaStore.selectFactor(factor)
  } catch (e) {
    errors.value = parseApiError(e)
  }
}

async function handleVerify() {
  errors.value = []
  isLoading.value = true

  try {
    const completed = await mfaStore.verifyCode(code.value)

    if (completed && mfaStore.context === 'login') {
      const authStore = useAuthStore()
      await authStore.loginByPassword(
        mfaStore.pendingEmail!,
        mfaStore.pendingPassword!,
        mfaStore.mfaSessionId!
      )
      mfaStore.reset()
      await router.push({ name: 'projects' })
    } else if (completed && mfaStore.context === 'reset_password') {
      const mfaSessionId = mfaStore.mfaSessionId!
      mfaStore.reset()
      await router.push({
        name: 'password-reset-confirm',
        query: { mfaSessionId },
      })
    }
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}

async function handleResend() {
  isResending.value = true
  try {
    await mfaStore.sendEmailCode()
  } finally {
    isResending.value = false
  }
}
</script>

<style scoped>
.mfa__factors {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.factor-btn {
  background: var(--color-bg-secondary);
  border: 0.5px solid var(--color-border-input);
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 13px;
  font-family: 'Inter', sans-serif;
  color: var(--color-text);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.2s, background 0.2s;
}

.factor-btn:hover {
  border-color: var(--color-primary);
}

.factor-btn--secondary {
  color: var(--color-text-secondary);
  font-size: 12px;
}

.factor-btn--secondary:hover {
  color: var(--color-text);
}

.mfa__hint {
  font-size: 13px;
  color: var(--color-text-secondary);
  margin: 0;
}

.mfa__resend {
  align-self: center;
  margin-top: 2px;
}
</style>
