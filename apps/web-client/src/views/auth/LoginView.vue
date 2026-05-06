<template>
  <AuthCard title="Войти">
    <template #subtitle>
      Нет аккаунта? <RouterLink to="/auth/register">Зарегистрироваться</RouterLink>
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
            autocomplete="current-password"
            :feedback="false"
            toggle-mask
            fluid
          />
          <RouterLink to="/auth/password/reset" class="field__forgot">Забыли пароль?</RouterLink>
        </div>

        <FormErrorList :messages="errors" />

        <Button type="submit" :label="isLoading ? 'Вхожу...' : 'Войти'" :loading="isLoading" />
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
import { parseLoginError } from '@/composables/useApiError'
import { useAuthStore } from '@/stores/auth.store'
import { useMfaStore } from '@/stores/mfa.store'
import { AuthErrorCodes } from '@/types/domain/error-codes'
import ExternalProviders from '@/components/auth/ExternalProviders.vue'

const router = useRouter()
const authStore = useAuthStore()
const mfaStore = useMfaStore()

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
    const result = await authStore.loginByPassword(email.value, password.value)

    if (result.success) {
      await router.push({ name: 'projects' })
    } else {
      mfaStore.startFromChallenge(result.challenge, 'login')
      await router.push({ name: 'mfa-verify' })
    }
  } catch (e) {
    if (isEmailVerificationPending(e)) {
      // Сохраняем email и переходим на страницу подтверждения
      authStore.pendingRegistrationEmail = email.value
      await router.push({ name: 'register-confirm' })
      return
    }
    errors.value = parseLoginError(e)
  } finally {
    isLoading.value = false
  }
}

</script>

<style scoped>
.field__forgot {
  font-size: 12px;
  color: var(--color-primary);
  text-decoration: none;
  align-self: flex-end;
}

</style>
