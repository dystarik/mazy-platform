<template>
  <main class="home">
    <section class="home-hero">
      <div class="home-hero__content">
        <h1 class="home-hero__title">Визуальная платформа для чат-ботов</h1>
        <p class="home-hero__subtitle">
          Создайте один проект со сценарием и запускайте его на нескольких площадках: VK и Telegram.
          Версии, токены и привязки ботов остаются под контролем.
        </p>

        <div class="home-hero__actions">
          <RouterLink v-slot="{ navigate }" :to="primaryAction.to" custom>
            <Button
              :label="primaryAction.label"
              class="home-hero__primary"
              @click="navigate"
            />
          </RouterLink>
          <RouterLink v-slot="{ navigate }" to="/auth/login" custom>
            <Button
              label="Войти"
              severity="secondary"
              outlined
              class="home-hero__secondary"
              @click="navigate"
            />
          </RouterLink>
        </div>

        <div class="home-hero__chips" aria-label="Возможности платформы">
          <Tag value="Один проект для VK и Telegram" severity="info" />
          <Tag value="Общий сценарий" severity="secondary" />
          <Tag value="Версии и релизы" severity="secondary" />
        </div>
      </div>

      <div class="home-preview">
        <LandingEditorPreview />
      </div>
    </section>

    <section id="features" class="home-section">
      <div class="home-section__header">
        <p>Рабочий контур</p>
        <h2>От схемы до запущенного бота</h2>
      </div>

      <div class="home-cards">
        <Card class="home-card">
          <template #content>
          <div class="home-card__icon home-card__icon--blue">
            <img :src="botIcon" alt="" />
          </div>
          <h3>Сценарии как карта</h3>
          <p>
            Узлы, ветвления, сообщения и действия видны на одном полотне. Команда быстрее понимает, что произойдёт с пользователем.
          </p>
          </template>
        </Card>

        <Card class="home-card">
          <template #content>
          <div class="home-card__icon home-card__icon--green">
            <img :src="folderIcon" alt="" />
          </div>
          <h3>Проекты и релизы</h3>
          <p>
            Работайте в draft, публикуйте стабильную версию и переключайте ботов без ручного копирования сценариев.
          </p>
          </template>
        </Card>

        <Card class="home-card">
          <template #content>
          <div class="home-card__icon home-card__icon--amber home-card__icon--platforms">
            <img class="home-card__brand-logo home-card__brand-logo--vk" :src="vkLogo" alt="" />
            <img class="home-card__brand-logo home-card__brand-logo--tg" :src="telegramLogo" alt="" />
          </div>
          <h3>Один проект на несколько площадок</h3>
          <p>
            Подключайте отдельные экземпляры ботов для VK и Telegram к одному проекту, храните токены и управляйте активностью.
          </p>
          </template>
        </Card>
      </div>
    </section>

    <section class="home-steps" aria-label="Процесс работы">
      <div class="home-step">
        <span>01</span>
        <strong>Опишите сценарий</strong>
      </div>
      <div class="home-step">
        <span>02</span>
        <strong>Проверьте draft</strong>
      </div>
      <div class="home-step">
        <span>03</span>
        <strong>Выпустите релиз</strong>
      </div>
      <div class="home-step">
        <span>04</span>
        <strong>Подключите VK или Telegram</strong>
      </div>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Tag from 'primevue/tag'
import LandingEditorPreview from '@/components/editor/LandingEditorPreview.vue'
import { useAuthStore } from '@/stores/auth.store'
import botIcon from '@/assets/icons/bot.svg'
import folderIcon from '@/assets/icons/folder.svg'
import telegramLogo from '@/assets/icons/TG Logo.svg'
import vkLogo from '@/assets/icons/VK Logo.svg'

const authStore = useAuthStore()

const primaryAction = computed(() =>
  authStore.isAuthenticated
    ? { to: '/app/projects', label: 'Открыть проекты' }
    : { to: '/auth/register', label: 'Начать работу' },
)
</script>

<style scoped>
.home {
  width: 100%;
  min-height: 100%;
  display: block;
  overflow: hidden;
  font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  color: var(--color-text);
}

.home-hero {
  position: relative;
  min-height: min(720px, calc(100vh - 104px));
  display: grid;
  grid-template-columns: minmax(420px, 0.78fr) minmax(520px, 1.22fr);
  align-items: center;
  gap: 44px;
  padding: 54px clamp(28px, 5vw, 76px);
  border-bottom: 0.5px solid var(--color-border);
  background:
    linear-gradient(110deg, var(--color-bg) 0%, var(--color-bg) 48%, color-mix(in srgb, var(--color-bg-secondary) 82%, var(--color-bg)) 100%);
}

.home-hero__content {
  position: relative;
  z-index: 2;
  max-width: 660px;
}

.home-hero__eyebrow {
  margin: 0 0 14px;
  color: var(--color-primary);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.home-hero__title {
  max-width: 690px;
  margin: 0;
  color: var(--color-text);
  font-size: clamp(44px, 5vw, 74px);
  font-weight: 760;
  line-height: 0.98;
}

.home-hero__subtitle {
  max-width: 540px;
  margin: 24px 0 0;
  color: var(--color-text-secondary);
  font-size: 18px;
  line-height: 1.65;
}

.home-hero__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 30px;
  flex-wrap: wrap;
}

.home-hero__primary,
.home-hero__secondary {
  width: auto;
  min-height: 44px;
  white-space: nowrap;
}

.home-hero__secondary {
  min-width: 96px;
}

.home-hero__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 28px;
}

.home-hero__chips :deep(.p-tag) {
  min-height: 28px;
  font-size: 12px;
  font-weight: 600;
}

.home-preview {
  min-width: 0;
  height: clamp(460px, 48vw, 560px);
}

.home-section {
  width: min(1180px, calc(100% - 48px));
  margin: 0 auto;
  padding: 56px 0 28px;
}

.home-section__header {
  margin-bottom: 22px;
}

.home-section__header p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.home-section__header h2 {
  max-width: 540px;
  margin: 6px 0 0;
  color: var(--color-text);
  font-size: clamp(26px, 3vw, 36px);
  line-height: 1.15;
}

.home-cards {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.home-card {
  min-height: 220px;
  border-radius: 12px;
}

.home-card :deep(.p-card-body) {
  height: 100%;
  padding: 24px;
}

.home-card :deep(.p-card-content) {
  padding: 0;
}

.home-card__icon {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 22px;
  border-radius: 10px;
  border: 1px solid color-mix(in srgb, var(--color-primary) 28%, transparent);
}

.home-card__icon img {
  width: 21px;
  height: 21px;
  display: block;
}

.home-card__icon--blue {
  background: color-mix(in srgb, var(--color-primary) 12%, transparent);
}

.home-card__icon--green {
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
}

.home-card__icon--amber {
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
}

.home-card__icon--platforms {
  width: 82px;
  justify-content: flex-start;
  gap: 9px;
  padding: 0 10px;
}

.home-card__icon .home-card__brand-logo--vk {
  width: 24px;
  height: 24px;
}

.home-card__icon .home-card__brand-logo--tg {
  width: 26px;
  height: 26px;
}

.home-card h3 {
  margin: 0 0 10px;
  color: var(--color-text);
  font-size: 17px;
  font-weight: 700;
}

.home-card p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 13px;
  line-height: 1.7;
}

.home-steps {
  width: min(1180px, calc(100% - 48px));
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 1px;
  margin: 20px auto 72px;
  border: 0.5px solid var(--color-border);
  border-radius: 14px;
  overflow: hidden;
  background: var(--color-border);
}

.home-step {
  min-height: 94px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 8px;
  padding: 18px;
  background: var(--color-bg-card);
}

.home-step span {
  color: var(--color-text-secondary);
  font-size: 12px;
  font-weight: 800;
}

.home-step strong {
  color: var(--color-text);
  font-size: 14px;
}

.dark .home-card__icon img {
  filter: invert(1) brightness(1.25);
}

.dark .home-card__icon img.home-card__brand-logo {
  filter: none;
}

@media (max-width: 1120px) {
  .home-hero {
    grid-template-columns: 1fr;
    min-height: auto;
  }

  .home-hero__content {
    max-width: 720px;
  }
}

@media (max-width: 760px) {
  .home-hero {
    padding: 42px 16px 34px;
    gap: 28px;
  }

  .home-hero__title {
    font-size: 38px;
  }

  .home-hero__subtitle {
    font-size: 16px;
  }

  .home-hero__primary,
  .home-hero__secondary {
    width: 100%;
  }

  .home-preview {
    height: 410px;
  }

  .home-section,
  .home-steps {
    width: calc(100% - 32px);
  }

  .home-cards,
  .home-steps {
    grid-template-columns: 1fr;
  }
}
</style>
