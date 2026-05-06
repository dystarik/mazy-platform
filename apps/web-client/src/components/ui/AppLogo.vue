<template>
  <div class="logo">
    <img src="@/assets/logo-dark.svg" alt="MazyPlatform" class="logo__icon" />
    <span class="logo__text">MazyPlatform</span>
  </div>
</template>

<style scoped>
.logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.logo__icon {
  width: 44px;
  height: 44px;
}

.logo__text {
  font-size: 22px;
  font-weight: 500;
  color: var(--color-text);
  font-family: 'Inter', sans-serif;
}
</style>
