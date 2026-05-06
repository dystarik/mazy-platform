<template>
  <Tag
    class="platform-badge"
    :class="{ 'platform-badge--with-label': shouldShowLabel }"
    severity="secondary"
  >
    <img
      v-if="isVk"
      class="platform-badge__logo platform-badge__logo--vk"
      :src="vkLogo"
      :alt="label"
    />
    <template v-else>
      <img
        v-if="isTelegram"
        class="platform-badge__logo platform-badge__logo--telegram"
        :src="telegramIcon"
        :alt="label"
      />
      <img
        v-else
        class="platform-badge__icon"
        :class="{ 'platform-badge__icon--dark': theme === 'dark' }"
        :src="icon"
        alt=""
      />
    </template>
    <span v-if="shouldShowLabel">{{ label }}</span>
  </Tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Tag from 'primevue/tag'
import { useTheme } from '@/composables/useTheme'
import globeIcon from '@/assets/icons/globe.svg'
import telegramIcon from '@/assets/icons/TG Logo.svg'
import vkLogoIcon from '@/assets/icons/VK Logo.svg'

const props = withDefaults(defineProps<{
  platform?: string | null
  label?: string
  showLabel?: boolean
}>(), {
  platform: null,
  label: 'Универсальный',
  showLabel: false,
})

const { theme } = useTheme()
const isVk = computed(() => props.platform?.includes('VK') ?? false)
const isTelegram = computed(() => props.platform?.includes('TELEGRAM') ?? false)
const shouldShowLabel = computed(() => props.showLabel || (!isVk.value && !isTelegram.value))
const icon = computed(() => isTelegram.value ? telegramIcon : globeIcon)
const vkLogo = computed(() => vkLogoIcon)
</script>

<style scoped>
.platform-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  padding: 5px;
  border-radius: 10px;
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  color: var(--color-text-secondary);
  font-size: 12px;
  font-weight: 500;
  line-height: 1.2;
  white-space: nowrap;
}

.platform-badge--with-label {
  width: auto;
  min-width: 132px;
  gap: 7px;
  padding: 5px 10px;
  justify-content: flex-start;
}

.platform-badge__logo--vk {
  width: 28px;
  height: 28px;
  object-fit: contain;
  display: block;
}

.platform-badge__logo--telegram {
  width: 28px;
  height: 28px;
  object-fit: contain;
  display: block;
}

.platform-badge__icon {
  width: 22px;
  height: 22px;
  display: block;
  flex-shrink: 0;
}

.platform-badge__icon--dark {
  filter: invert(1) brightness(1.35);
}
</style>
