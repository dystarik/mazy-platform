<template>
  <AuthCard title="Сброс пароля">
    <template #subtitle>
      Введите email, чтобы начать сброс пароля
    </template>

    <form class="auth-card__form" @submit.prevent="handleSubmit">
        <div class="field">
          <label>Email</label>
          <InputText
            v-model="email"
            type="email"
            placeholder="example@mail.ru"
            autocomplete="email"
            autofocus
          />
        </div>

        <FormErrorList :messages="errors" />

        <Button
          type="submit"
          :label="isLoading ? 'Проверяю...' : 'Продолжить'"
          :loading="isLoading"
          :disabled="!email.trim()"
        />
    </form>

    <RouterLink to="/auth/login" class="reset__back">← Вернуться ко входу</RouterLink>
  </AuthCard>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { usePasswordStore } from '@/stores/password.store'
import { useMfaStore } from '@/stores/mfa.store'
import { parseApiError } from '@/composables/useApiError'

const router = useRouter()
const passwordStore = usePasswordStore()
const mfaStore = useMfaStore()

const email = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)

async function handleSubmit(): Promise<void> {
  errors.value = []
  isLoading.value = true

  try {
    const normalizedEmail = email.value.trim()
    const result = await passwordStore.resetPassword(normalizedEmail)

    if ('otp' in result) {
      await router.push({
        name: 'password-reset-confirm',
        query: { otpId: result.otp.otpId, email: normalizedEmail },
      })
    } else {
      mfaStore.startFromChallenge(result.mfa, 'reset_password')
      mfaStore.pendingEmail = normalizedEmail
      await router.push({ name: 'mfa-verify' })
    }
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.reset__back {
  font-size: 13px;
  color: var(--color-text-secondary);
  text-decoration: none;
  text-align: center;
  transition: color 0.15s;
}

.reset__back:hover {
  color: var(--color-primary);
}
</style>
