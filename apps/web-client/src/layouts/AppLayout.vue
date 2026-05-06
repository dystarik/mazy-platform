<template>
  <div class="app-layout">
    <LandingNav />
    <div class="app-layout__body">
      <main class="content">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import LandingNav from '@/components/ui/LandingNav.vue'
</script>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.app-layout__body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.content {
  flex: 1;
  overflow-x: hidden;
  overflow-y: auto;
}
</style>
