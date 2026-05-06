<template>
  <div class="guest-layout">
    <LandingNav />
    <div class="guest-layout__content">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import LandingNav from '@/components/ui/LandingNav.vue'
</script>

<style scoped>
.guest-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.guest-layout__content {
  flex: 1;
  width: 100%;
  min-width: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
