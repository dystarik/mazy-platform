<template>
  <AuthCard title="Регистрация">
    <template #subtitle>
      Уже есть аккаунт? <RouterLink to="/auth/login">Войти</RouterLink>
    </template>

    <form class="auth-card__form" @submit.prevent="handleSubmit">
        <div class="field">
          <label>Email</label>
          <InputText
            v-model="email"
            type="email"
            placeholder="example@mail.ru"
            autocomplete="email"
          />
        </div>

        <div class="field">
          <label>Пароль</label>
          <Password
            v-model="password"
            placeholder="••••••••"
            autocomplete="new-password"
            :feedback="false"
            toggle-mask
            fluid
          />
        </div>

        <FormErrorList :messages="errors" />

        <Button type="submit" :label="isLoading ? 'Регистрирую...' : 'Зарегистрироваться'" :loading="isLoading" />
    </form>

    <div class="divider"><span>или</span></div>
    <ExternalProviders />
  </AuthCard>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { parseApiError } from '@/composables/useApiError'
import { useAuthStore } from '@/stores/auth.store'
import { AuthErrorCodes } from '@/types/domain/error-codes'
import ExternalProviders from '@/components/auth/ExternalProviders.vue'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)

function isEmailVerificationPending(e: unknown): boolean {
  const err = e as { response?: { data?: { details?: { reason: string }[] } } }
  return err.response?.data?.details?.some(
    (d) => d.reason === AuthErrorCodes.EmailVerificationPending
  ) ?? false
}

async function handleSubmit() {
  errors.value = []
  isLoading.value = true

  try {
    await authStore.register(email.value, password.value)
    await router.push({ name: 'register-confirm' })
  } catch (e) {
    if (isEmailVerificationPending(e)) {
      authStore.pendingRegistrationEmail = email.value
      await router.push({ name: 'register-confirm' })
      return
    }
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}
</script>
