<template>
  <RouterView v-slot="{ Component, route }">
    <component :is="getLayout(route)" >
      <component :is="Component" />
    </component>
  </RouterView>
</template>

<script setup lang="ts">
import { RouterView } from 'vue-router'
import { defineAsyncComponent } from 'vue'
import type { RouteLocationNormalizedLoaded } from 'vue-router'

const GuestLayout = defineAsyncComponent(() => import('@/layouts/GuestLayout.vue'))
const AppLayout = defineAsyncComponent(() => import('@/layouts/AppLayout.vue'))

function getLayout(route: RouteLocationNormalizedLoaded) {
  if (route.meta.guard === 'guest') return GuestLayout
  if (route.meta.guard === 'auth') return AppLayout
  return GuestLayout
}
</script>
