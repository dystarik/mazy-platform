<template>
  <AuthCard title="Сброс пароля">
    <template #subtitle>
      Введите email — мы пришлём код для сброса пароля
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

        <p v-if="success" class="reset__success">
          Код отправлен на {{ email }}. Проверьте почту.
        </p>

        <FormErrorList :messages="errors" />

        <Button
          type="submit"
          :label="isLoading ? 'Отправляю...' : 'Отправить код'"
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
import { parseApiError } from '@/composables/useApiError'

const router = useRouter()
const passwordStore = usePasswordStore()

const email = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)
const success = ref(false)

async function handleSubmit(): Promise<void> {
  errors.value = []
  isLoading.value = true
  success.value = false

  try {
    const result = await passwordStore.resetPassword(email.value.trim())

    if ('otp' in result) {
      await router.push({
        name: 'password-reset-confirm',
        query: { otpId: result.otp.otpId, email: email.value.trim() },
      })
    } else {
      // MFA — пока показываем успех, полный флоу можно добавить позже
      success.value = true
    }
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.reset__success {
  font-size: 13px;
  color: var(--color-primary);
  margin: 0;
}

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
