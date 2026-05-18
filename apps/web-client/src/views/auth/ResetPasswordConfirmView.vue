<template>
  <AuthCard title="Новый пароль">
    <template #subtitle>
      {{ subtitle }}
    </template>

    <form class="auth-card__form" @submit.prevent="handleSubmit">
        <div v-if="isOtpReset" class="field">
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
          :disabled="isLoading || !newPassword.trim() || (isOtpReset && !code.trim())"
        />
    </form>

    <RouterLink to="/auth/password/reset" class="confirm__back">← Начать сброс заново</RouterLink>
  </AuthCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
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

const otpId = computed(() => queryValue(route.query.otpId))
const mfaSessionId = computed(() => queryValue(route.query.mfaSessionId))
const isOtpReset = computed(() => Boolean(otpId.value))
const isMfaReset = computed(() => Boolean(mfaSessionId.value))
const subtitle = computed(() => (
  isOtpReset.value
    ? 'Введите код из письма и задайте новый пароль'
    : 'Задайте новый пароль после дополнительной проверки'
))

function queryValue(value: unknown): string {
  if (Array.isArray(value)) return String(value[0] ?? '')
  return String(value ?? '')
}

onMounted(async () => {
  if (!isOtpReset.value && !isMfaReset.value) {
    await router.replace({ name: 'password-reset' })
  }
})

async function handleSubmit(): Promise<void> {
  errors.value = []
  isLoading.value = true

  try {
    if (isOtpReset.value) {
      await passwordStore.confirmResetPasswordByOtp(newPassword.value, otpId.value, code.value)
    } else if (isMfaReset.value) {
      await passwordStore.confirmResetPasswordByMfa(newPassword.value, mfaSessionId.value)
    } else {
      await router.replace({ name: 'password-reset' })
      return
    }

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
