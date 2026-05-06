<template>
  <AuthCard title="Новый пароль">
    <template #subtitle>
      Введите код из письма и задайте новый пароль
    </template>

    <form class="auth-card__form" @submit.prevent="handleSubmit">
        <div class="field">
          <label>Код из письма</label>
          <InputText
            v-model="code"
            type="text"
            placeholder="123456"
            autocomplete="one-time-code"
            maxlength="6"
            autofocus
          />
        </div>

        <div class="field">
          <label>Новый пароль</label>
          <Password
            v-model="newPassword"
            placeholder="••••••••"
            autocomplete="new-password"
            :feedback="false"
            toggle-mask
            fluid
          />
        </div>

        <FormErrorList :messages="errors" />

        <Button
          type="submit"
          :label="isLoading ? 'Сохраняю...' : 'Сохранить пароль'"
          :loading="isLoading"
          :disabled="isLoading || !code.trim() || !newPassword.trim()"
        />
    </form>

    <RouterLink to="/auth/password/reset" class="confirm__back">← Отправить код повторно</RouterLink>
  </AuthCard>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import { useRouter, useRoute } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { usePasswordStore } from '@/stores/password.store'
import { parseApiError } from '@/composables/useApiError'

const router = useRouter()
const route = useRoute()
const passwordStore = usePasswordStore()

const code = ref('')
const newPassword = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)

const otpId = route.query.otpId as string

async function handleSubmit(): Promise<void> {
  errors.value = []
  isLoading.value = true

  try {
    await passwordStore.confirmResetPasswordByOtp(newPassword.value, otpId, code.value)
    await router.push({ name: 'login' })
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.confirm__back {
  font-size: 13px;
  color: var(--color-text-secondary);
  text-decoration: none;
  text-align: center;
  transition: color 0.15s;
}

.confirm__back:hover {
  color: var(--color-primary);
}
</style>
