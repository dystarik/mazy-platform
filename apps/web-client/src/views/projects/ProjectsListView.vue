<template>
  <div class="projects-page">
    <PageHeader title="Проекты">
      <template #actions>
      <Button label="Новый проект" class="projects-page__add" @click="openModal" />
      </template>
    </PageHeader>

    <div class="projects-summary">
      <div class="projects-summary__item">
        <span class="projects-summary__label">Всего</span>
        <span class="projects-summary__value">{{ totalProjectsCount }}</span>
      </div>
      <div class="projects-summary__item">
        <span class="projects-summary__label">VK</span>
        <span class="projects-summary__value">{{ vkProjectsCount }}</span>
      </div>
      <div class="projects-summary__item">
        <span class="projects-summary__label">Telegram</span>
        <span class="projects-summary__value">{{ telegramProjectsCount }}</span>
      </div>
      <div class="projects-summary__item">
        <span class="projects-summary__label">Универсальные</span>
        <span class="projects-summary__value">{{ universalProjectsCount }}</span>
      </div>
    </div>

    <!-- Загрузка -->
    <div v-if="store.loading" class="projects-state">
      <div class="projects-state__spinner" />
      <span>Загрузка проектов...</span>
    </div>

    <!-- Пустое состояние -->
    <EmptyState v-else-if="store.projects.length === 0" title="Проектов пока нет" :icon="folderIcon">
      <Button label="Создать проект" @click="openModal" />
    </EmptyState>

    <!-- Таблица проектов -->
    <div v-else class="projects-table-wrap">
      <DataTable
        :value="store.projects"
        data-key="projectId"
        class="projects-table"
        @row-click="goToProject($event.data.projectId!)"
        @row-reorder="handleProjectRowReorder"
      >
        <Column row-reorder class="projects-table__reorder-col" />
        <Column header="Проект" class="projects-table__project-col">
          <template #body="{ data: project }">
              <div class="projects-table__project">
                <div class="projects-table__project-icon">
                  <img
                    class="projects-table__project-folder"
                    :class="{ 'projects-table__project-folder--dark': theme === 'dark' }"
                    :src="folderIcon"
                    alt=""
                  />
                </div>
                <div class="projects-table__project-copy">
                  <div class="projects-table__project-top">
                    <span class="projects-table__project-name">{{ project.name }}</span>
                  </div>
                </div>
              </div>
          </template>
        </Column>
        <Column header="Платформа" class="projects-table__platform-col">
          <template #body="{ data: project }">
            <PlatformBadge
              :platform="project.platformType"
              :label="platformLabel(project.platformType)"
              show-label
            />
          </template>
        </Column>
        <Column header="Действия" class="projects-table__actions-col">
          <template #body="{ data: project }">
              <div class="projects-table__actions-inner">
                <Button
                  label="Открыть"
                  text
                  @click.stop="goToProject(project.projectId!)"
                />
                <IconButton
                  :icon="trashIcon"
                  label="Удалить проект"
                  danger
                  @click.stop="confirmDelete(project)"
                />
              </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <AppModal
      :open="modalOpen"
      title="Новый проект"
      @update:open="$event ? (modalOpen = true) : closeModal()"
    >
            <div class="field">
              <label>Название проекта</label>
              <InputText
                v-model="form.name"
                type="text"
                placeholder="Например: Поддержка клиентов"
                autofocus
                @keydown.enter="handleCreate"
              />
            </div>
            <div class="field">
              <label>Платформа</label>
              <div class="platform-picker">
                <button
                  class="platform-option"
                  :class="{ 'platform-option--active': form.platformType === 'PLATFORM_TYPE_UNIVERSAL' }"
                  type="button"
                  @click="form.platformType = 'PLATFORM_TYPE_UNIVERSAL'"
                >
                  <img
                    class="platform-option__logo platform-option__logo--globe"
                    :class="{ 'platform-option__logo--dark': theme === 'dark' }"
                    :src="globeIcon"
                    alt=""
                  />
                  <span>Универсальный</span>
                </button>
                <button
                  class="platform-option platform-option--vk"
                  :class="{ 'platform-option--active': form.platformType === 'PLATFORM_TYPE_VK' }"
                  type="button"
                  @click="form.platformType = 'PLATFORM_TYPE_VK'"
                >
                  <img class="platform-option__logo platform-option__logo--vk" :src="vkIconLogo" alt="VK" />
                  <span>ВКонтакте</span>
                </button>
                <button
                  class="platform-option platform-option--telegram"
                  :class="{ 'platform-option--active': form.platformType === 'PLATFORM_TYPE_TELEGRAM' }"
                  type="button"
                  @click="form.platformType = 'PLATFORM_TYPE_TELEGRAM'"
                >
                  <img
                    class="platform-option__logo platform-option__logo--telegram"
                    :src="telegramIcon"
                    alt="Telegram"
                  />
                  <span>Телеграм</span>
                </button>
              </div>
            </div>
            <FormErrorList :messages="createErrors" />

      <template #footer>
            <Button
              label="Создать"
              :loading="creating"
              :disabled="!form.name.trim() || creating"
              @click="handleCreate"
            />
      </template>
    </AppModal>

    <ConfirmModal
      :open="Boolean(deleteTarget)"
      title="Удалить проект?"
      :message="`Проект ${deleteTarget?.name || ''} будет удалён безвозвратно вместе со всеми сценариями.`"
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
import { computed, ref, reactive, onMounted } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputText from 'primevue/inputtext'
import { useRouter } from 'vue-router'
import { useProjectsStore } from '@/stores/projects.store'
import { parseApiError } from '@/composables/useApiError'
import { useTheme } from '@/composables/useTheme'
import AppModal from '@/components/ui/AppModal.vue'
import ConfirmModal from '@/components/ui/ConfirmModal.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import IconButton from '@/components/ui/IconButton.vue'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlatformBadge from '@/components/ui/PlatformBadge.vue'
import type { PlatformType, ProjectListItem } from '@/types/api'
import folderIcon from '@/assets/icons/folder.svg'
import globeIcon from '@/assets/icons/globe.svg'
import telegramIcon from '@/assets/icons/TG Logo.svg'
import trashIcon from '@/assets/icons/trash.svg'
import vkIconLogo from '@/assets/icons/VK Logo.svg'
import vkTextLogoBlackWhite from '@/assets/icons/VK Text Logo Black&white.svg'
import vkTextLogoWhite from '@/assets/icons/VK Text Logo White.svg'

const router = useRouter()
const store = useProjectsStore()
const { theme } = useTheme()
const projectOrderStorageKey = 'mazy-platform:project-order'

interface ProjectRowReorderEvent {
  value: ProjectListItem[]
}

onMounted(async () => {
  await store.fetchProjects()
  store.projects = applyStoredProjectOrder(store.projects)
})

function platformLabel(type?: PlatformType): string {
  if (type === 'PLATFORM_TYPE_VK') return 'ВКонтакте'
  if (type === 'PLATFORM_TYPE_TELEGRAM') return 'Телеграм'
  return 'Универсальный'
}

function handleProjectRowReorder(event: ProjectRowReorderEvent): void {
  store.projects = event.value
  saveProjectOrder(event.value)
}

function applyStoredProjectOrder(items: ProjectListItem[]): ProjectListItem[] {
  const order = readProjectOrder()
  if (!order.length) return items

  const orderMap = new Map(order.map((id, index) => [id, index]))
  return [...items].sort((left, right) => {
    const leftOrder = orderMap.get(projectOrderKey(left))
    const rightOrder = orderMap.get(projectOrderKey(right))

    if (leftOrder === undefined && rightOrder === undefined) return 0
    if (leftOrder === undefined) return 1
    if (rightOrder === undefined) return -1
    return leftOrder - rightOrder
  })
}

function saveProjectOrder(items: ProjectListItem[]): void {
  localStorage.setItem(
    projectOrderStorageKey,
    JSON.stringify(items.map(projectOrderKey).filter(Boolean)),
  )
}

function readProjectOrder(): string[] {
  try {
    const raw = localStorage.getItem(projectOrderStorageKey)
    const parsed = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string') : []
  } catch {
    return []
  }
}

function projectOrderKey(project: ProjectListItem): string {
  return project.projectId ?? ''
}

const vkPlatformLogo = computed(() =>
  theme.value === 'dark'
    ? vkTextLogoWhite
    : vkTextLogoBlackWhite,
)

const totalProjectsCount = computed(() => store.projects.length)
const vkProjectsCount = computed(() =>
  store.projects.filter((project) => project.platformType === 'PLATFORM_TYPE_VK').length,
)
const telegramProjectsCount = computed(() =>
  store.projects.filter((project) => project.platformType === 'PLATFORM_TYPE_TELEGRAM').length,
)
const universalProjectsCount = computed(() =>
  store.projects.filter((project) =>
    project.platformType === undefined
    || project.platformType === 'PLATFORM_TYPE_UNSPECIFIED'
    || project.platformType === 'PLATFORM_TYPE_UNIVERSAL',
  ).length,
)

function goToProject(id: string): void {
  router.push({ name: 'project', params: { id } })
}

// --- Создание ---
const modalOpen = ref(false)
const creating = ref(false)
const createErrors = ref<string[]>([])
const form = reactive({ name: '', platformType: 'PLATFORM_TYPE_UNIVERSAL' as PlatformType })

function openModal(): void {
  form.name = ''
  form.platformType = 'PLATFORM_TYPE_UNIVERSAL'
  createErrors.value = []
  modalOpen.value = true
}

function closeModal(): void {
  if (creating.value) return
  modalOpen.value = false
}

async function handleCreate(): Promise<void> {
  if (!form.name.trim() || creating.value) return
  creating.value = true
  createErrors.value = []
  try {
    const id = await store.createProject({ name: form.name.trim(), platformType: form.platformType })
    modalOpen.value = false
    router.push({ name: 'project', params: { id } })
  } catch (e) {
    createErrors.value = parseApiError(e)
  } finally {
    creating.value = false
  }
}

// --- Удаление ---
const deleteTarget = ref<ProjectListItem | null>(null)
const deleting = ref(false)
const deleteErrors = ref<string[]>([])

function confirmDelete(project: ProjectListItem): void {
  deleteTarget.value = project
  deleteErrors.value = []
}

async function handleDelete(): Promise<void> {
  if (!deleteTarget.value || deleting.value) return
  deleting.value = true
  deleteErrors.value = []
  try {
    await store.deleteProject(deleteTarget.value.projectId!)
    deleteTarget.value = null
  } catch (e) {
    deleteErrors.value = parseApiError(e)
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.projects-page {
  padding: 36px 40px 30vh;
  max-width: 1120px;
  margin: 0 auto;
}

.projects-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 24px;
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-card);
}

.projects-summary__item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 16px 18px;
  border-right: 0.5px solid var(--color-border);
}

.projects-summary__item:last-child {
  border-right: none;
}

.projects-summary__label {
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.projects-summary__value {
  color: var(--color-text);
  font-size: 18px;
  font-weight: 600;
}

.projects-page__add {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 160px;
  padding: 9px 16px;
}

.projects-page__add-plus {
  font-size: 16px;
  line-height: 1;
}

/* Состояния */
.projects-state {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 12px;
  min-height: 320px;
  color: var(--color-text-secondary);
  text-align: center;
}

.projects-state__spinner {
  width: 28px;
  height: 28px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Таблица */
.projects-table-wrap {
  background: var(--color-bg-card);
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  overflow: hidden;
}

.projects-table {
  width: 100%;
}

.projects-table :deep(.p-datatable-table) {
  table-layout: fixed;
  width: 100%;
}

.projects-table :deep(.p-datatable-thead > tr > th) {
  padding: 14px 20px;
  text-align: left;
  font-size: 14px;
  font-weight: 700;
  text-transform: none;
  letter-spacing: 0;
  color: var(--color-text);
  background: var(--color-bg-secondary);
  border-bottom: 0.5px solid var(--color-border);
}

.projects-table :deep(.p-datatable-tbody > tr > td) {
  padding: 16px 20px;
  border-bottom: 0.5px solid var(--color-border);
  vertical-align: middle;
}

.projects-table :deep(.p-datatable-tbody > tr:last-child > td) {
  border-bottom: none;
}

.projects-table :deep(.p-datatable-tbody > tr) {
  cursor: pointer;
  transition: background 0.15s ease;
}

.projects-table :deep(.p-datatable-tbody > tr:hover > td) {
  background: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg-card));
}

.projects-table :deep(.projects-table__reorder-col) {
  width: 44px;
  text-align: center;
}

.projects-table :deep(.p-datatable-reorderable-row-handle) {
  cursor: grab;
  color: var(--color-text-secondary);
}

.projects-table :deep(.p-datatable-reorderable-row-handle:active) {
  cursor: grabbing;
}

.projects-table__row {
  cursor: pointer;
  transition: background 0.15s;
}

.projects-table__row:hover {
  background: color-mix(in srgb, var(--color-primary) 4%, transparent);
}

.projects-table__project {
  display: flex;
  align-items: center;
  gap: 12px;
}

.projects-table__project-icon {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  background: var(--color-bg-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-primary);
  flex-shrink: 0;
}

.projects-table__project-folder {
  width: 19px;
  height: 19px;
  display: block;
  opacity: 0.72;
}

.projects-table__project-folder--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.9;
}

.projects-table__project-copy {
  display: flex;
  flex-direction: column;
}

.projects-table__project-top {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.projects-table__project-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text);
}

.projects-table :deep(.p-datatable-thead > tr > th:nth-child(2)),
.projects-table :deep(.p-datatable-tbody > tr > td:nth-child(2)) {
  width: 34%;
}

.projects-table :deep(.p-datatable-thead > tr > th:nth-child(3)),
.projects-table :deep(.p-datatable-tbody > tr > td:nth-child(3)) {
  width: 44%;
}

.projects-table :deep(.p-datatable-thead > tr > th:nth-child(4)),
.projects-table :deep(.p-datatable-tbody > tr > td:nth-child(4)) {
  width: 22%;
  text-align: right;
}

.projects-table :deep(.p-datatable-thead > tr > th:nth-child(4) .p-datatable-column-header-content) {
  justify-content: flex-end;
}

.projects-table__actions-head {
  text-align: center;
}

.projects-table__actions {
  text-align: center;
}

.projects-table__actions-inner {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  white-space: nowrap;
}

.projects-table__open {
  font-size: 13px;
  color: var(--color-primary);
}

/* Выбор платформы */
.platform-picker {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
}

.platform-option {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 86px;
  padding: 12px;
  border-radius: 10px;
  border: 1.5px solid var(--color-border);
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font-size: 13px;
  font-family: inherit;
  cursor: pointer;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
}

.platform-option__logo {
  width: 36px;
  height: 36px;
  object-fit: contain;
  display: block;
  flex-shrink: 0;
}

.platform-option__logo--globe {
  opacity: 0.75;
}

.platform-option__logo--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.9;
}

.platform-option__logo--telegram {
  opacity: 1;
}

.platform-option:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
}

.platform-option--active {
  border-color: var(--color-primary);
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
  color: var(--color-primary);
}

@media (max-width: 900px) {
  .projects-page {
    padding: 28px 20px 30vh;
  }

  .projects-summary {
    grid-template-columns: 1fr;
  }

  .projects-summary__item {
    border-right: none;
  }

  .projects-summary__item:not(:last-child) {
    border-bottom: 0.5px solid var(--color-border);
  }

  .projects-table thead {
    display: none;
  }

  .projects-table,
  .projects-table tbody,
  .projects-table tr,
  .projects-table td {
    display: block;
    width: 100%;
  }

  .projects-table__row {
    padding: 16px 0;
    border-bottom: 0.5px solid var(--color-border);
  }

  .projects-table tbody tr:last-child {
    border-bottom: none;
  }

  .projects-table tbody td {
    padding: 0 16px;
    border-bottom: none;
  }

  .projects-table tbody td + td {
    margin-top: 12px;
  }

  .projects-table__actions {
    text-align: left;
    padding-top: 6px;
  }

  .projects-table__actions-inner {
    justify-content: flex-start;
  }

}
</style>
