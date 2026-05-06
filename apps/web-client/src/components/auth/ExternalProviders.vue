<template>
  <div class="external-providers">
    <Button class="btn-yandex" outlined severity="secondary" @click="handleYandex">
      <img src="@/assets/icons/yandex.svg" alt="Yandex" width="16" height="16" />
      Войти через Яндекс
    </Button>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'

function handleYandex() {
  const clientId = import.meta.env.VITE_YANDEX_CLIENT_ID
  const redirectUri = `${window.location.origin}/auth/callback/yandex`
  const url = new URL('https://oauth.yandex.ru/authorize')
  url.searchParams.set('response_type', 'code')
  url.searchParams.set('client_id', clientId)
  url.searchParams.set('redirect_uri', redirectUri)
  window.location.href = url.toString()
}
</script>
