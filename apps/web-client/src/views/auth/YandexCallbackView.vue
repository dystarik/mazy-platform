<template>
  <AuthCard>
      <template v-if="errors.length">
        <FormErrorList :messages="errors" />
        <RouterLink
          to="/auth/login"
          class="btn-primary callback__back"
        >
          Вернуться к входу
        </RouterLink>
      </template>

      <p v-else class="callback__status">Выполняется вход...</p>
  </AuthCard>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/ui/AuthCard.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import { useAuthStore } from '@/stores/auth.store'
import { linkedProvidersApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'

const router = useRouter()
const authStore = useAuthStore()
const errors = ref<string[]>([])

onMounted(async () => {
  const params = new URLSearchParams(window.location.search)
  const code = params.get('code')
  const state = params.get('state')

  if (!code) {
    errors.value = ['Код авторизации не получен']
    return
  }

  try {
    if (state === 'link') {
      await linkedProvidersApi.linkProvider({
        provider: 'EXTERNAL_PROVIDER_YANDEX',
        code,
      })
      await router.replace({ name: 'settings' })
    } else {
      await authStore.loginByYandex(code)
      await router.replace({ name: 'projects' })
    }
  } catch (e) {
    errors.value = parseApiError(e)
  }
})
</script>

<style scoped>
.callback__status {
  color: var(--color-text-secondary);
  font-size: 14px;
  text-align: center;
}

.callback__back {
  display: block;
  margin-top: 12px;
  text-align: center;
  text-decoration: none;
}
</style>
