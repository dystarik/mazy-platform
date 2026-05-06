<template>
  <AuthCard title="Подтверждение email">
    <template #subtitle>
      Мы отправили код на вашу почту. Введите его ниже.
    </template>

    <form class="auth-card__form" @submit.prevent="handleSubmit">
        <div class="field">
          <label>Код подтверждения</label>
          <InputOtp v-model="code" :length="6" />
          <Button
            type="button"
            class="confirm__resend"
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
import { ref } from 'vue'
import Button from 'primevue/button'
import InputOtp from 'primevue/inputotp'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { parseApiError } from '@/composables/useApiError'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const code = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)
const isResending = ref(false)

async function handleSubmit() {
  errors.value = []
  isLoading.value = true

  try {
    await authStore.completeRegistration(code.value)
    await router.push({ name: 'projects' })
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isLoading.value = false
  }
}

async function handleResend() {
  isResending.value = true
  errors.value = []

  try {
    // email берётся из store (сохранился при register или EMAIL_VERIFICATION_PENDING)
    await authStore.resendConfirmationCode()
    code.value = ''
  } catch (e) {
    errors.value = parseApiError(e)
  } finally {
    isResending.value = false
  }
}
</script>

<style scoped>
.confirm__resend {
  align-self: center;
  margin-top: 2px;
}

</style>
