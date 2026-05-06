<template>
  <div class="bots-page">
    <PageHeader title="Боты">
      <template #actions>
      <Button label="Добавить бота" type="button" class="bots-page__add" @click="openCreateModal" />
      </template>
    </PageHeader>

    <div class="bots-summary">
      <div class="bots-summary__item">
        <span class="bots-summary__label">Всего</span>
        <span class="bots-summary__value">{{ bots.length }}</span>
      </div>
      <div class="bots-summary__item">
        <span class="bots-summary__label">Активные</span>
        <span class="bots-summary__value">{{ activeBotsCount }}</span>
      </div>
      <div class="bots-summary__item">
        <span class="bots-summary__label">Без проекта</span>
        <span class="bots-summary__value">{{ unboundBotsCount }}</span>
      </div>
    </div>

    <div v-if="loading" class="bots-state">
      <div class="bots-state__spinner" />
      <span>Загрузка ботов...</span>
    </div>

    <EmptyState v-else-if="bots.length === 0" title="Ботов пока нет" :icon="botIcon">
      <Button label="Добавить бота" type="button" @click="openCreateModal" />
    </EmptyState>

    <div v-else class="bots-table-wrap">
      <DataTable
        :value="bots"
        data-key="botInstanceId"
        class="bots-table"
        @row-reorder="handleBotRowReorder"
      >
        <Column row-reorder class="bots-table__reorder-col" />
        <Column header="Бот">
          <template #body="{ data: bot }">
              <div class="bots-table__bot">
                <div
                  class="bots-table__bot-icon"
                  :class="bot.status === 'BOT_STATUS_ACTIVE'
                    ? 'bots-table__bot-icon--active'
                    : 'bots-table__bot-icon--inactive'"
                >
                  <img
                    class="bots-table__bot-img"
                    :class="{ 'bots-table__bot-img--dark': theme === 'dark' }"
                    :src="bot.status === 'BOT_STATUS_ACTIVE' ? botIcon : botOffIcon"
                    alt=""
                  />
                </div>
                <PlatformBadge :platform="bot.platformType" :label="botPlatformLabel(bot.platformType)" />
                <div class="bots-table__bot-copy">
                  <div class="bots-table__bot-top">
                    <span class="bots-table__bot-name">{{ bot.name || 'Без названия' }}</span>
                  </div>
                </div>
              </div>
          </template>
        </Column>
        <Column header="Привязка" class="bots-table__binding-col">
          <template #body="{ data: bot }">
              <div v-if="bot.projectId" class="bots-table__binding">
                <span class="bots-table__project">{{ projectName(bot.projectId) }}</span>
                <span class="bots-table__version">v{{ bot.scenarioVersion ?? '—' }}</span>
                <span class="bots-table__version-mode">
                  {{ scenarioVersionUpdateModeLabel(bot.scenarioVersionUpdateMode) }}
                </span>
              </div>
              <span v-else class="bots-table__muted">Не привязан</span>
          </template>
        </Column>
        <Column header="Статус" class="bots-table__status-col">
          <template #body="{ data: bot }">
              <StatusPill
                :label="statusLabel(bot.status)"
                :variant="bot.status === 'BOT_STATUS_ACTIVE' ? 'success' : 'inactive'"
              />
          </template>
        </Column>
        <Column header="Токен" class="bots-table__token-col">
          <template #body="{ data: bot }">
              <span class="bots-table__token">{{ bot.maskedAccessToken || 'Скрыт' }}</span>
          </template>
        </Column>
        <Column header="Действия" class="bots-table__actions-col">
          <template #body="{ data: bot }">
              <div class="bots-table__actions-inner">
                <IconButton
                  v-if="bot.projectId && bot.scenarioVersion"
                  :icon="bot.status === 'BOT_STATUS_ACTIVE' ? powerOffIcon : powerIcon"
                  :label="bot.status === 'BOT_STATUS_ACTIVE' ? 'Остановить' : 'Запустить'"
                  :disabled="busyBotId === bot.botInstanceId"
                  @click="handleToggleStatus(bot)"
                />
                <Button
                  label="..."
                  severity="secondary"
                  outlined
                  class="bots-table__menu-button"
                  aria-label="Открыть действия бота"
                  :disabled="busyBotId === bot.botInstanceId"
                  @click="openActionsMenu($event, bot)"
                />
              </div>
          </template>
        </Column>
      </DataTable>
      <Menu
        ref="actionsMenu"
        :model="actionsMenuItems"
        popup
        class="bots-actions-menu"
      />
    </div>

    <div v-if="pageErrors.length" class="page-errors">
      <p v-for="(msg, index) in pageErrors" :key="index">{{ msg }}</p>
    </div>

    <AppModal
      :open="createModalOpen"
      title="Новый бот"
      @update:open="$event ? (createModalOpen = true) : closeCreateModal()"
    >
            <div class="field">
              <label>Название</label>
              <InputText
                v-model="createForm.name"
                type="text"
                placeholder="Например: Бот поддержки"
                autofocus
                @keydown.enter="handleCreateBot"
              />
            </div>

            <div class="field">
              <label>Платформа</label>
              <div class="platform-picker">
                <button
                  class="platform-option"
                  :class="{ 'platform-option--active': createForm.platformType === 'Bot_PLATFORM_TYPE_VK' }"
                  type="button"
                  @click="createForm.platformType = 'Bot_PLATFORM_TYPE_VK'"
                >
                  <img class="platform-option__logo platform-option__logo--vk" :src="vkIconLogo" alt="ВКонтакте" />
                </button>
                <button
                  class="platform-option"
                  :class="{ 'platform-option--active': createForm.platformType === 'Bot_PLATFORM_TYPE_TELEGRAM' }"
                  type="button"
                  @click="createForm.platformType = 'Bot_PLATFORM_TYPE_TELEGRAM'"
                >
                  <img
                    class="platform-option__logo platform-option__logo--telegram"
                    :src="telegramIcon"
                    alt="Telegram"
                  />
                </button>
              </div>
            </div>

            <div class="field">
              <label>Токен доступа</label>
              <Password
                v-model="createForm.accessToken"
                :placeholder="createTokenPlaceholder"
                :feedback="false"
                toggle-mask
                fluid
              />
            </div>

            <div v-if="createForm.platformType === 'Bot_PLATFORM_TYPE_VK'" class="field">
              <label>Community ID</label>
              <InputText
                v-model="createForm.communityId"
                type="text"
                placeholder="Например: 123456789"
                @keydown.enter="handleCreateBot"
              />
            </div>

            <FormErrorList :messages="createErrors" />

      <template #footer>
            <Button
              label="Создать"
              type="button"
              :loading="creating"
              :disabled="creating"
              @click="handleCreateBot"
            />
      </template>
    </AppModal>

    <AppModal
      :open="Boolean(bindingTarget)"
      :title="bindingTarget?.projectId ? 'Версия сценария' : 'Привязать к проекту'"
      @update:open="$event ? undefined : closeBindingModal()"
    >
            <div v-if="!bindingTarget?.projectId" class="field">
              <label>Проект</label>
              <Select
                v-model="bindingForm.projectId"
                :options="compatibleProjectOptions"
                option-label="label"
                option-value="value"
                placeholder="Выберите проект"
                :disabled="versionsLoading"
              />
            </div>

            <div class="field">
              <label>Версия сценария</label>
              <Select
                v-model.number="bindingForm.scenarioVersion"
                :options="availableVersionOptions"
                option-label="label"
                option-value="value"
                :placeholder="versionsLoading ? 'Загружаю версии...' : 'Выберите версию'"
                :disabled="versionsLoading || !bindingForm.projectId || !availableVersions.length"
              />
            </div>

            <div class="field-checkbox">
              <Checkbox
                input-id="binding-auto-upgrade"
                v-model="bindingForm.autoUpgrade"
                binary
              />
              <label for="binding-auto-upgrade">Переходить на новую версию автоматически</label>
            </div>

            <FormErrorList :messages="bindingErrors" />

      <template #footer>
            <Button
              type="button"
              :label="bindingTarget?.projectId ? 'Обновить версию' : 'Привязать'"
              :loading="bindingSaving"
              :disabled="bindingSaving || !canSubmitBinding"
              @click="handleSubmitBinding"
            />
      </template>
    </AppModal>

    <AppModal
      :open="Boolean(tokenTarget)"
      title="Обновить токен"
      @update:open="$event ? undefined : closeTokenModal()"
    >
            <div class="field">
              <label>Бот</label>
              <InputText :model-value="tokenTarget?.name || 'Без названия'" type="text" disabled />
            </div>
            <div class="field">
              <label>Новый токен</label>
              <Password
                v-model="tokenForm.accessToken"
                :placeholder="tokenPlaceholder"
                :feedback="false"
                toggle-mask
                fluid
                autofocus
                @keydown.enter="handleUpdateToken"
              />
            </div>
            <div v-if="isVkBot(tokenTarget)" class="field">
              <label>Community ID</label>
              <InputText
                v-model="tokenForm.communityId"
                type="text"
                placeholder="Например: 123456789"
                @keydown.enter="handleUpdateToken"
              />
            </div>
            <FormErrorList :messages="tokenErrors" />

      <template #footer>
            <Button
              label="Сохранить"
              type="button"
              :loading="tokenSaving"
              :disabled="tokenSaving"
              @click="handleUpdateToken"
            />
      </template>
    </AppModal>

    <ConfirmModal
      :open="Boolean(unbindTarget)"
      title="Отвязать бота?"
      :message="`Бот ${unbindTarget?.name || 'Без названия'} потеряет привязку к проекту и станет неактивным.`"
      confirm-label="Отвязать"
      loading-label="Отвязываю..."
      danger
      :loading="unbinding"
      :errors="unbindErrors"
      @update:open="!$event && (unbindTarget = null)"
      @confirm="handleUnbind"
    />

    <ConfirmModal
      :open="Boolean(deleteTarget)"
      title="Удалить бота?"
      :message="`Бот ${deleteTarget?.name || 'Без названия'} будет удалён безвозвратно.`"
      confirm-label="Удалить"
      loading-label="Удаляю..."
      danger
      :loading="deleting"
      :errors="deleteErrors"
      @update:open="!$event && (deleteTarget = null)"
      @confirm="handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputText from 'primevue/inputtext'
import Menu from 'primevue/menu'
import Password from 'primevue/password'
import Select from 'primevue/select'
import type { MenuItem } from 'primevue/menuitem'
import { botsApi, projectsApi, scenarioApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'
import { useTheme } from '@/composables/useTheme'
import AppModal from '@/components/ui/AppModal.vue'
import ConfirmModal from '@/components/ui/ConfirmModal.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import IconButton from '@/components/ui/IconButton.vue'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlatformBadge from '@/components/ui/PlatformBadge.vue'
import StatusPill from '@/components/ui/StatusPill.vue'
import type {
  BotListItem,
  BotPlatformType,
  BotScenarioVersionUpdateMode,
  BotStatus,
  PlatformType,
  ProjectListItem,
  VersionItem,
} from '@/types/api'
import botIcon from '@/assets/icons/bot.svg'
import botOffIcon from '@/assets/icons/bot-off.svg'
import powerIcon from '@/assets/icons/power.svg'
import powerOffIcon from '@/assets/icons/power-off.svg'
import telegramIcon from '@/assets/icons/TG Logo.svg'
import vkIconLogo from '@/assets/icons/VK Logo.svg'
import vkTextLogoBlackWhite from '@/assets/icons/VK Text Logo Black&white.svg'
import vkTextLogoWhite from '@/assets/icons/VK Text Logo White.svg'

const { theme } = useTheme()

const loading = ref(true)
const bots = ref<BotListItem[]>([])
const projects = ref<ProjectListItem[]>([])
const pageErrors = ref<string[]>([])
const busyBotId = ref<string | null>(null)
const actionsMenu = ref<InstanceType<typeof Menu> | null>(null)
const actionsMenuBot = ref<BotListItem | null>(null)
const botOrderStorageKey = 'mazy-platform:bot-order'

interface BotRowReorderEvent {
  value: BotListItem[]
}

const createModalOpen = ref(false)
const creating = ref(false)
const createErrors = ref<string[]>([])
const createForm = reactive({
  name: '',
  accessToken: '',
  communityId: '',
  platformType: 'Bot_PLATFORM_TYPE_VK' as BotPlatformType,
})

const bindingTarget = ref<BotListItem | null>(null)
const bindingSaving = ref(false)
const bindingErrors = ref<string[]>([])
const versionsLoading = ref(false)
const availableVersions = ref<VersionItem[]>([])
const bindingForm = reactive({
  projectId: '',
  scenarioVersion: null as number | null,
  autoUpgrade: true,
})

const tokenTarget = ref<BotListItem | null>(null)
const tokenSaving = ref(false)
const tokenErrors = ref<string[]>([])
const tokenForm = reactive({
  accessToken: '',
  communityId: '',
})

const unbindTarget = ref<BotListItem | null>(null)
const unbinding = ref(false)
const unbindErrors = ref<string[]>([])

const deleteTarget = ref<BotListItem | null>(null)
const deleting = ref(false)
const deleteErrors = ref<string[]>([])

const vkPlatformLogo = computed(() =>
  theme.value === 'dark'
    ? vkTextLogoWhite
    : vkTextLogoBlackWhite,
)
const createTokenPlaceholder = computed(() =>
  createForm.platformType === 'Bot_PLATFORM_TYPE_TELEGRAM'
    ? '123456789:AA...'
    : 'vk1.a.AbCdEfGh...',
)
const tokenPlaceholder = computed(() =>
  isTelegramBot(tokenTarget.value)
    ? '123456789:AA...'
    : 'vk1.a.AbCdEfGh...',
)

const compatibleProjects = computed(() =>
  projects.value.filter(project => isProjectCompatible(project.platformType, bindingTarget.value?.platformType)),
)

const compatibleProjectOptions = computed(() =>
  compatibleProjects.value.map(project => ({
    label: project.name ?? 'Без названия',
    value: project.projectId ?? '',
  })),
)

const availableVersionOptions = computed(() =>
  availableVersions.value.map(version => ({
    label: `Версия ${version.version}`,
    value: version.version ?? null,
  })),
)

const activeBotsCount = computed(() =>
  bots.value.filter(bot => bot.status === 'BOT_STATUS_ACTIVE').length,
)

const unboundBotsCount = computed(() =>
  bots.value.filter(bot => !bot.projectId).length,
)

const actionsMenuItems = computed<MenuItem[]>(() => {
  const bot = actionsMenuBot.value
  if (!bot) return []

  const isBusy = busyBotId.value === bot.botInstanceId
  const items: MenuItem[] = [
    {
      label: bot.projectId ? 'Сменить версию' : 'Привязать к проекту',
      disabled: isBusy,
      command: () => openBindingModal(bot),
    },
    {
      label: 'Обновить токен',
      disabled: isBusy,
      command: () => openTokenModal(bot),
    },
  ]

  if (bot.projectId) {
    items.push({
      label: 'Отвязать от проекта',
      disabled: isBusy,
      class: 'bots-actions-menu__item--danger',
      command: () => openUnbindModal(bot),
    })
  }

  items.push({
    label: 'Удалить бота',
    disabled: isBusy,
    class: 'bots-actions-menu__item--danger',
    command: () => openDeleteModal(bot),
  })

  return items
})

const canSubmitBinding = computed(
  () => Boolean(bindingForm.projectId) && typeof bindingForm.scenarioVersion === 'number',
)

onMounted(() => {
  void loadInitialData()
})

watch(
  () => bindingForm.projectId,
  (projectId, previousProjectId) => {
    if (!bindingTarget.value) return

    if (!projectId) {
      availableVersions.value = []
      bindingForm.scenarioVersion = null
      return
    }

    if (projectId === previousProjectId) return

    bindingForm.scenarioVersion = null
    void loadVersions(projectId)
  },
)

watch(
  () => createForm.platformType,
  (platformType) => {
    if (platformType !== 'Bot_PLATFORM_TYPE_VK') {
      createForm.communityId = ''
    }
  },
)

function botPlatformLabel(type?: BotPlatformType): string {
  if (type === 'Bot_PLATFORM_TYPE_VK') return 'VK'
  if (type === 'Bot_PLATFORM_TYPE_TELEGRAM') return 'Telegram'
  return 'Неизвестно'
}

function statusLabel(status?: BotStatus): string {
  return status === 'BOT_STATUS_ACTIVE' ? 'Активен' : 'Неактивен'
}

function scenarioVersionUpdateModeLabel(mode?: BotScenarioVersionUpdateMode): string {
  return isAutoUpgradeMode(mode) ? 'Автопереход' : 'Ручной режим'
}

function isAutoUpgradeMode(mode?: BotScenarioVersionUpdateMode): boolean {
  return mode !== 'BOT_SCENARIO_VERSION_UPDATE_MODE_MANUAL'
}

function toScenarioVersionUpdateMode(autoUpgrade: boolean): BotScenarioVersionUpdateMode {
  return autoUpgrade
    ? 'BOT_SCENARIO_VERSION_UPDATE_MODE_AUTO'
    : 'BOT_SCENARIO_VERSION_UPDATE_MODE_MANUAL'
}

function openActionsMenu(event: MouseEvent, bot: BotListItem): void {
  actionsMenuBot.value = bot
  actionsMenu.value?.toggle(event)
}

function isProjectCompatible(platformType?: PlatformType, botPlatformType?: BotPlatformType): boolean {
  const requiredPlatform = toProjectPlatform(botPlatformType)
  return (
    platformType === undefined
    || platformType === 'PLATFORM_TYPE_UNSPECIFIED'
    || platformType === 'PLATFORM_TYPE_UNIVERSAL'
    || platformType === requiredPlatform
  )
}

function toProjectPlatform(botPlatformType?: BotPlatformType): PlatformType | null {
  if (botPlatformType === 'Bot_PLATFORM_TYPE_VK') return 'PLATFORM_TYPE_VK'
  if (botPlatformType === 'Bot_PLATFORM_TYPE_TELEGRAM') return 'PLATFORM_TYPE_TELEGRAM'
  return null
}

function isVkBot(bot?: BotListItem | null): boolean {
  return bot?.platformType === 'Bot_PLATFORM_TYPE_VK'
}

function isTelegramBot(bot?: BotListItem | null): boolean {
  return bot?.platformType === 'Bot_PLATFORM_TYPE_TELEGRAM'
}

function projectName(projectId?: string): string {
  if (!projectId) return 'Проект не найден'

  return projects.value.find(project => project.projectId === projectId)?.name ?? projectId
}

async function loadInitialData(): Promise<void> {
  loading.value = true
  pageErrors.value = []

  try {
    const [botsResponse, projectsResponse] = await Promise.all([
      botsApi.list(),
      projectsApi.list(),
    ])
    bots.value = applyStoredBotOrder(botsResponse.items ?? [])
    projects.value = projectsResponse.items ?? []
  } catch (error) {
    pageErrors.value = parseApiError(error)
  } finally {
    loading.value = false
  }
}

async function refreshBots(): Promise<void> {
  const response = await botsApi.list()
  bots.value = applyStoredBotOrder(response.items ?? [])
}

function handleBotRowReorder(event: BotRowReorderEvent): void {
  bots.value = event.value
  saveBotOrder(event.value)
}

function applyStoredBotOrder(items: BotListItem[]): BotListItem[] {
  const order = readBotOrder()
  if (!order.length) return items

  const orderMap = new Map(order.map((id, index) => [id, index]))
  return [...items].sort((left, right) => {
    const leftOrder = orderMap.get(botOrderKey(left))
    const rightOrder = orderMap.get(botOrderKey(right))

    if (leftOrder === undefined && rightOrder === undefined) return 0
    if (leftOrder === undefined) return 1
    if (rightOrder === undefined) return -1
    return leftOrder - rightOrder
  })
}

function saveBotOrder(items: BotListItem[]): void {
  localStorage.setItem(
    botOrderStorageKey,
    JSON.stringify(items.map(botOrderKey).filter(Boolean)),
  )
}

function readBotOrder(): string[] {
  try {
    const raw = localStorage.getItem(botOrderStorageKey)
    const parsed = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string') : []
  } catch {
    return []
  }
}

function botOrderKey(bot: BotListItem): string {
  return bot.botInstanceId ?? ''
}

function openCreateModal(): void {
  createForm.name = ''
  createForm.accessToken = ''
  createForm.communityId = ''
  createForm.platformType = 'Bot_PLATFORM_TYPE_VK'
  createErrors.value = []
  createModalOpen.value = true
}

function closeCreateModal(): void {
  if (creating.value) return
  createModalOpen.value = false
}

async function handleCreateBot(): Promise<void> {
  if (creating.value) return

  creating.value = true
  createErrors.value = []

  try {
    await botsApi.createStandalone({
      name: createForm.name.trim(),
      accessToken: createForm.accessToken.trim(),
      communityId: createForm.platformType === 'Bot_PLATFORM_TYPE_VK'
        ? createForm.communityId.trim()
        : undefined,
      platformType: createForm.platformType,
    })

    createModalOpen.value = false
    await refreshBots()
  } catch (error) {
    createErrors.value = parseApiError(error)
  } finally {
    creating.value = false
  }
}

function openBindingModal(bot: BotListItem): void {
  bindingTarget.value = bot
  bindingErrors.value = []
  availableVersions.value = []
  bindingForm.projectId = bot.projectId ?? ''
  bindingForm.scenarioVersion = bot.scenarioVersion ?? null
  bindingForm.autoUpgrade = isAutoUpgradeMode(bot.scenarioVersionUpdateMode)

  if (bindingForm.projectId) {
    void loadVersions(bindingForm.projectId)
  }
}

function closeBindingModal(): void {
  if (bindingSaving.value) return

  bindingTarget.value = null
  bindingErrors.value = []
  availableVersions.value = []
  bindingForm.projectId = ''
  bindingForm.scenarioVersion = null
  bindingForm.autoUpgrade = true
}

async function loadVersions(projectId: string): Promise<void> {
  versionsLoading.value = true

  try {
    const response = await scenarioApi.getVersions(projectId)
    availableVersions.value = (response.versions ?? []).slice().reverse()

    if (
      bindingTarget.value?.projectId
      && bindingTarget.value.projectId === projectId
      && typeof bindingTarget.value.scenarioVersion === 'number'
    ) {
      bindingForm.scenarioVersion = bindingTarget.value.scenarioVersion
    }
  } catch (error) {
    availableVersions.value = []
    bindingErrors.value = parseApiError(error)
  } finally {
    versionsLoading.value = false
  }
}

async function handleSubmitBinding(): Promise<void> {
  if (!bindingTarget.value || !canSubmitBinding.value || bindingSaving.value) return
  const botInstanceId = bindingTarget.value.botInstanceId
  if (!botInstanceId) return

  bindingSaving.value = true
  bindingErrors.value = []

  try {
    if (bindingTarget.value.projectId) {
      if (bindingTarget.value.scenarioVersion !== bindingForm.scenarioVersion) {
        await botsApi.changeScenarioVersion(botInstanceId, {
          botInstanceId,
          newScenarioVersion: bindingForm.scenarioVersion!,
        })
      }

      const nextMode = toScenarioVersionUpdateMode(bindingForm.autoUpgrade)
      if (isAutoUpgradeMode(bindingTarget.value.scenarioVersionUpdateMode) !== bindingForm.autoUpgrade) {
        await botsApi.changeScenarioVersionUpdateMode(botInstanceId, {
          botInstanceId,
          scenarioVersionUpdateMode: nextMode,
        })
      }
    } else {
      await botsApi.bind(botInstanceId, {
        botInstanceId,
        projectId: bindingForm.projectId,
        scenarioVersion: bindingForm.scenarioVersion!,
        scenarioVersionUpdateMode: toScenarioVersionUpdateMode(bindingForm.autoUpgrade),
      })
    }

    closeBindingModal()
    await refreshBots()
  } catch (error) {
    bindingErrors.value = parseApiError(error)
  } finally {
    bindingSaving.value = false
  }
}

function openTokenModal(bot: BotListItem): void {
  tokenTarget.value = bot
  tokenForm.accessToken = ''
  tokenForm.communityId = bot.communityId ?? ''
  tokenErrors.value = []
}

function closeTokenModal(): void {
  if (tokenSaving.value) return
  tokenTarget.value = null
  tokenErrors.value = []
}

async function handleUpdateToken(): Promise<void> {
  if (!tokenTarget.value?.botInstanceId || tokenSaving.value) return

  tokenSaving.value = true
  tokenErrors.value = []

  try {
    await botsApi.updateToken(tokenTarget.value.botInstanceId, {
      botInstanceId: tokenTarget.value.botInstanceId,
      accessToken: tokenForm.accessToken.trim(),
      communityId: isVkBot(tokenTarget.value)
        ? tokenForm.communityId.trim()
        : undefined,
    })

    closeTokenModal()
    await refreshBots()
  } catch (error) {
    tokenErrors.value = parseApiError(error)
  } finally {
    tokenSaving.value = false
  }
}

async function handleToggleStatus(bot: BotListItem): Promise<void> {
  if (!bot.botInstanceId || busyBotId.value) return

  busyBotId.value = bot.botInstanceId
  pageErrors.value = []

  try {
    if (bot.status === 'BOT_STATUS_ACTIVE') {
      await botsApi.deactivate(bot.botInstanceId)
    } else {
      await botsApi.activate(bot.botInstanceId)
    }

    await refreshBots()
  } catch (error) {
    pageErrors.value = parseApiError(error)
  } finally {
    busyBotId.value = null
  }
}

function openUnbindModal(bot: BotListItem): void {
  unbindTarget.value = bot
  unbindErrors.value = []
}

async function handleUnbind(): Promise<void> {
  if (!unbindTarget.value?.botInstanceId || unbinding.value) return

  unbinding.value = true
  unbindErrors.value = []

  try {
    await botsApi.unbind(unbindTarget.value.botInstanceId)
    unbindTarget.value = null
    await refreshBots()
  } catch (error) {
    unbindErrors.value = parseApiError(error)
  } finally {
    unbinding.value = false
  }
}

function openDeleteModal(bot: BotListItem): void {
  deleteTarget.value = bot
  deleteErrors.value = []
}

async function handleDelete(): Promise<void> {
  if (!deleteTarget.value?.botInstanceId || deleting.value) return

  deleting.value = true
  deleteErrors.value = []

  try {
    await botsApi.delete(deleteTarget.value.botInstanceId)
    deleteTarget.value = null
    await refreshBots()
  } catch (error) {
    deleteErrors.value = parseApiError(error)
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.bots-page {
  padding: 36px 40px 30vh;
  max-width: 1120px;
  margin: 0 auto;
}

.bots-page__add {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 160px;
  padding: 9px 16px;
}

.bots-page__add-plus {
  font-size: 16px;
  line-height: 1;
}

.bots-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-bottom: 24px;
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-card);
}

.bots-summary__item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 16px 18px;
  border-right: 0.5px solid var(--color-border);
}

.bots-summary__item:last-child {
  border-right: none;
}

.bots-summary__label {
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.bots-summary__value {
  color: var(--color-text);
  font-size: 18px;
  font-weight: 600;
}

.bots-state {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 12px;
  min-height: 320px;
  color: var(--color-text-secondary);
  text-align: center;
}

.bots-state__icon {
  width: 42px;
  height: 42px;
  opacity: 0.55;
}

.bots-state__icon--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.8;
}

.bots-state__title {
  margin: 0;
  color: var(--color-text);
  font-size: 16px;
  font-weight: 500;
}

.bots-state .btn-primary {
  width: auto;
  padding: 9px 20px;
  margin-top: 4px;
}

.bots-state__spinner {
  width: 28px;
  height: 28px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.bots-table-wrap {
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-card);
}

.bots-table {
  width: 100%;
  table-layout: fixed;
}

.bots-table :deep(.p-datatable-table) {
  table-layout: fixed;
  width: 100%;
}

.bots-table :deep(.p-datatable-thead > tr > th) {
  padding: 14px 20px;
  color: var(--color-text);
  background: var(--color-bg-secondary);
  border-bottom: 0.5px solid var(--color-border);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: none;
}

.bots-table :deep(.p-datatable-tbody > tr > td) {
  padding: 16px 20px;
  border-bottom: 0.5px solid var(--color-border);
  color: var(--color-text-secondary);
  font-size: 13px;
  vertical-align: middle;
}

.bots-table :deep(.p-datatable-tbody > tr:last-child > td) {
  border-bottom: none;
}

.bots-table :deep(.bots-table__reorder-col) {
  width: 44px;
  text-align: center;
}

.bots-table :deep(.p-datatable-reorderable-row-handle) {
  cursor: grab;
  color: var(--color-text-secondary);
}

.bots-table :deep(.p-datatable-reorderable-row-handle:active) {
  cursor: grabbing;
}

.bots-table :deep(.bots-table__binding-col) {
  width: 210px;
}

.bots-table :deep(.bots-table__status-col) {
  width: 112px;
}

.bots-table :deep(.bots-table__token-col) {
  width: 160px;
}

.bots-table :deep(.bots-table__actions-col) {
  width: 132px;
  text-align: right;
}

.bots-table :deep(.bots-table__actions-col .p-datatable-column-header-content) {
  justify-content: flex-end;
}

.bots-table__bot {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.bots-table__bot-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  border-radius: 10px;
  border: 1px solid var(--color-border);
  background: var(--color-bg-card);
}

.bots-table__bot-icon--active {
  border-color: color-mix(in srgb, #16a34a 42%, var(--color-border));
  background: color-mix(in srgb, #16a34a 13%, var(--color-bg-card));
}

.bots-table__bot-icon--inactive {
  border-color: color-mix(in srgb, #dc2626 42%, var(--color-border));
  background: color-mix(in srgb, #dc2626 11%, var(--color-bg-card));
}

.bots-table__bot-img {
  width: 20px;
  height: 20px;
  display: block;
  opacity: 0.72;
}

.bots-table__bot-img--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.9;
}

.bots-table__bot-copy {
  display: flex;
  align-items: center;
  min-width: 0;
}

.bots-table__bot-top {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.bots-table__bot-name {
  overflow: hidden;
  color: var(--color-text);
  font-size: 14px;
  font-weight: 500;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bots-table__platform-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 22px;
  padding: 3px 8px;
  border-radius: 999px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  white-space: nowrap;
  flex-shrink: 0;
}

.bots-table__platform-logo {
  width: 71px;
  height: auto;
  display: block;
}

.bots-table__binding {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.bots-table__project {
  overflow: hidden;
  color: var(--color-text);
  font-weight: 500;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bots-table__version {
  color: var(--color-primary);
  font-size: 12px;
}

.bots-table__version-mode {
  color: var(--color-text-secondary);
  font-size: 12px;
}

.bots-table__muted,
.bots-table__token {
  color: var(--color-text-secondary);
}

.bots-table__actions-inner {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  white-space: nowrap;
}

.bots-table__menu-button {
  width: 34px;
  height: 34px;
  min-width: 34px;
  padding: 0;
  letter-spacing: 1px;
}

:global(.bots-actions-menu) {
  min-width: 190px;
}

:global(.bots-actions-menu .bots-actions-menu__item--danger .p-menu-item-content),
:global(.bots-actions-menu .bots-actions-menu__item--danger .p-menu-item-label) {
  color: var(--color-danger);
}

.btn-link:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.page-errors {
  margin-top: 16px;
  color: var(--color-danger);
  font-size: 13px;
}

.page-errors p {
  margin: 0;
}

.field-checkbox {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--color-text);
  font-size: 13px;
}

.field-checkbox :deep(.p-checkbox) {
  flex: 0 0 auto;
}

.field-checkbox label {
  cursor: default;
  user-select: none;
}

.platform-picker {
  display: flex;
  gap: 10px;
}

.platform-option {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  padding: 8px;
  border: 1px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-card);
  color: var(--color-text-secondary);
  font-family: inherit;
  cursor: pointer;
}

.platform-option--active {
  border-color: var(--color-primary);
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
}

.platform-option__logo {
  width: 28px;
  height: 28px;
  object-fit: contain;
  display: block;
}

select,
input {
  width: 100%;
}

@media (max-width: 1100px) {
  .bots-table-wrap {
    overflow-x: auto;
  }

  .bots-table :deep(.p-datatable-table) {
    min-width: 1040px;
  }
}

@media (max-width: 900px) {
  .bots-page {
    padding: 28px 20px 30vh;
  }

  .bots-page__header {
    align-items: flex-start;
    flex-direction: column;
  }

  .bots-summary {
    grid-template-columns: 1fr;
  }

  .bots-summary__item {
    border-right: none;
  }

  .bots-summary__item:not(:last-child) {
    border-bottom: 0.5px solid var(--color-border);
  }
}
</style>
