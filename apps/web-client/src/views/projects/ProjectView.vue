<template>
  <div class="project-page">
    <div class="project-page__header">
      <div class="project-page__title-block">
        <router-link :to="{ name: 'projects' }" class="btn-secondary project-page__back">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 12H5M12 5l-7 7 7 7"/>
          </svg>
          К проектам
        </router-link>
        <div class="project-page__title-row">
          <h1 class="project-page__title">{{ project?.name ?? 'Проект' }}</h1>
          <PlatformBadge
            v-if="project"
            :platform="project.platformType"
            :label="platformLabel(project.platformType)"
          />
        </div>
      </div>
    </div>

    <div class="project-summary">
      <div class="project-summary__item">
        <span class="project-summary__label">Релиз</span>
        <span class="project-summary__value">{{ release?.version ? `v${release.version}` : 'Нет' }}</span>
      </div>
      <div class="project-summary__item">
        <span class="project-summary__label">Схемы</span>
        <span class="project-summary__value">{{ schemas.length }}</span>
      </div>
      <div class="project-summary__item">
        <span class="project-summary__label">Боты</span>
        <span class="project-summary__value">{{ bots.length }}</span>
      </div>
      <div class="project-summary__item">
        <span class="project-summary__label">Записи</span>
        <span class="project-summary__value">{{ userDataTotalCount }}</span>
      </div>
    </div>

    <div class="project-tabs">
      <button
        class="project-tab"
        :class="{ 'project-tab--active': tab === 'scenario' }"
        type="button"
        @click="tab = 'scenario'"
      >
        Сценарий
      </button>
      <button
        class="project-tab"
        :class="{ 'project-tab--active': tab === 'schemas' }"
        type="button"
        @click="tab = 'schemas'"
      >
        Схемы данных
      </button>
      <button
        class="project-tab"
        :class="{ 'project-tab--active': tab === 'userData' }"
        type="button"
        @click="tab = 'userData'"
      >
        Данные пользователей
      </button>
    </div>

    <section v-if="tab === 'scenario'" class="project-section">
      <div class="scenario-panel">
        <div class="scenario-panel__row scenario-panel__row--draft">
          <div class="scenario-panel__main">
            <span class="scenario-panel__label">Черновик</span>
            <span class="scenario-panel__desc">Редактируемая версия сценария</span>
          </div>
          <div class="scenario-panel__actions">
            <Button
              label="Открыть редактор"
              class="scenario-panel__button"
              @click="openEditor"
            />
          </div>
        </div>

        <div class="scenario-panel__row">
          <div class="scenario-panel__main">
            <span class="scenario-panel__label">Релиз</span>
            <span class="scenario-panel__desc">Версия, которую исполняют привязанные боты</span>
          </div>
          <div class="scenario-panel__release">
            <StatusPill
              :label="release?.version ? `v${release.version}` : 'Нет релиза'"
              :variant="release ? 'active' : 'muted'"
            />
            <span class="scenario-panel__usage">
              Ботов: {{ botCountForVersion(release?.version) }}
            </span>
            <Button
              label="Посмотреть релиз"
              class="scenario-panel__button"
              type="button"
              :disabled="!release?.graphJson"
              @click="openReleaseViewer"
            />
          </div>
        </div>

      </div>

      <div class="scenario-publish-card">
        <div class="scenario-panel__main">
          <span class="scenario-panel__label">Публикация</span>
          <span class="scenario-panel__desc">
            Перенести текущий черновик в релиз для привязанных ботов
          </span>
        </div>
        <Button
          :label="promoting ? 'Публикую...' : 'Опубликовать изменения'"
          class="scenario-panel__button scenario-panel__button--publish"
          :loading="promoting"
          :disabled="!draft || promoting"
          @click="handlePromote"
        />
      </div>

      <div v-if="scenarioErrors.length" class="project-errors project-errors--scenario">
        <p v-for="(msg, i) in scenarioErrors" :key="i">{{ msg }}</p>
      </div>

      <div class="project-block">
        <div class="project-block__header">
          <div>
            <h2 class="project-block__title">История версий</h2>
            <p class="project-block__desc">Опубликованные версии сценария и использование ботами</p>
          </div>
        </div>

        <div v-if="versionsLoading" class="project-state">Загрузка версий...</div>
        <div v-else-if="!versions.length" class="project-state">Опубликованных версий пока нет</div>
        <table v-else class="project-table">
          <thead>
            <tr>
              <th>Версия</th>
              <th>Дата</th>
              <th>Боты</th>
              <th>Статус</th>
              <th class="project-table__actions-head">Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="version in versions" :key="version.version">
              <td class="project-table__strong">v{{ version.version }}</td>
              <td>{{ formatTs(version.createdAt) }}</td>
              <td>{{ botCountForVersion(version.version) }}</td>
              <td>
                <StatusPill
                  :label="version.version === release?.version ? 'Текущий' : 'Архив'"
                  :variant="version.version === release?.version ? 'active' : 'muted'"
                  size="small"
                />
              </td>
              <td class="project-table__actions">
                <Button
                  label="Посмотреть"
                  text
                  type="button"
                  :disabled="!version.version"
                  @click="openVersionViewer(version.version!)"
                />
                <Button
                  v-if="version.version !== release?.version"
                  label="Откатить"
                  text
                  :disabled="rolling || versionDeleting"
                  @click="handleRollbackTo(version.version!)"
                />
                <IconButton
                  v-if="canDeleteVersion(version)"
                  :icon="trashIcon"
                  label="Удалить версию"
                  :disabled="versionDeleting"
                  @click="confirmDeleteVersion(version)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section v-if="tab === 'schemas'" class="project-section">
      <div class="project-block">
        <div class="project-block__header">
          <div>
            <h2 class="project-block__title">Схемы данных</h2>
            <p class="project-block__desc">Структуры, которые сценарий использует для постоянных данных</p>
          </div>
          <Button label="Новая схема" class="project-block__button" @click="openCreateSchema" />
        </div>

        <div v-if="schemasLoading" class="project-state">Загрузка схем...</div>
        <div v-else-if="!schemas.length" class="project-state">Схем пока нет</div>
        <table v-else class="project-table">
          <thead>
            <tr>
              <th>Схема</th>
              <th>Поля</th>
              <th class="project-table__actions-head">Действия</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="schema in schemas" :key="schema.schemaId">
              <tr class="project-table__clickable" @click="toggleSchema(schema.schemaId!)">
                <td class="project-table__strong">{{ schema.name }}</td>
                <td>{{ schemaFieldCount(schema.schemaId!) }}</td>
                <td class="project-table__actions">
                  <Button
                    :label="expandedSchema === schema.schemaId ? 'Скрыть' : 'Открыть'"
                    text
                    type="button"
                    @click.stop="toggleSchema(schema.schemaId!)"
                  />
                  <IconButton
                    :icon="trashIcon"
                    label="Удалить схему"
                    @click.stop="confirmDeleteSchema(schema)"
                  />
                </td>
              </tr>
              <tr v-if="expandedSchema === schema.schemaId" class="schema-details-row">
                <td colspan="3">
                  <div class="schema-details">
                    <div class="schema-details__header">
                      <span class="schema-details__title">Поля схемы</span>
                      <Button
                        label="Добавить поле"
                        severity="secondary"
                        outlined
                        class="schema-details__button"
                        @click="openAddField(schema.schemaId!)"
                      />
                    </div>

                    <div v-if="!schemaDetails[schema.schemaId!]?.fields?.length" class="project-state project-state--compact">
                      Полей нет
                    </div>
                    <DataTable
                      v-else
                      :value="schemaDetails[schema.schemaId!]?.fields ?? []"
                      data-key="fieldId"
                      class="fields-datatable"
                      show-gridlines
                      striped-rows
                      size="small"
                    >
                      <Column header="Поле">
                        <template #body="{ data: field }">
                          <span class="project-table__strong">{{ field.name }}</span>
                        </template>
                      </Column>
                      <Column header="Тип">
                        <template #body="{ data: field }">
                          {{ fieldTypeLabel(field.fieldType) }}
                        </template>
                      </Column>
                      <Column header="Обязательное">
                        <template #body="{ data: field }">
                          <Tag
                            :value="field.isRequired ? 'Да' : 'Нет'"
                            :severity="field.isRequired ? 'success' : 'secondary'"
                            rounded
                          />
                        </template>
                      </Column>
                    </DataTable>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </section>

    <section v-if="tab === 'userData'" class="project-section">
      <div class="project-block">
        <div class="project-block__header project-block__header--stacked">
          <div>
            <h2 class="project-block__title">Данные пользователей</h2>
            <p class="project-block__desc">Записи, которые сценарии сохранили во время общения с ботами</p>
          </div>
          <Button
            class="project-block__button project-prime-button"
            label="Обновить"
            severity="secondary"
            outlined
            :disabled="userDataLoading"
            @click="loadUserData"
          />
        </div>

        <div v-if="userDataErrors.length" class="project-errors project-errors--inside">
          <p v-for="(msg, i) in userDataErrors" :key="i">{{ msg }}</p>
        </div>

        <div v-if="userDataLoading" class="project-state">Загрузка данных...</div>
        <div v-else-if="!userDataGroups.length" class="project-state">Записей пока нет</div>
        <DataTable
          v-else
          :value="userDataGroups"
          data-key="key"
          class="user-data-datatable"
          show-gridlines
          striped-rows
          size="small"
          @row-click="openUserDataTableFromRow"
        >
          <Column header="Схема / версия" class="user-data-datatable__schema">
            <template #body="{ data: group }">
              <div class="record-schema">
                <span class="project-table__strong">{{ group.schemaName }}</span>
                <span class="record-schema__version">v{{ group.scenarioVersion ?? '—' }}</span>
              </div>
            </template>
          </Column>
          <Column header="Записи" class="user-data-datatable__count">
            <template #body="{ data: group }">
              {{ group.recordsCount }}
            </template>
          </Column>
          <Column header="Обновлено" class="user-data-datatable__date">
            <template #body="{ data: group }">
              {{ formatTs(group.updatedAt) }}
            </template>
          </Column>
        </DataTable>
      </div>
    </section>
  </div>

  <AppModal
    :open="createSchemaOpen"
    title="Новая схема"
    @update:open="createSchemaOpen = $event"
  >
          <div class="field">
            <label>Название</label>
            <InputText
              v-model="schemaForm.name"
              type="text"
              placeholder="Например: Заявка"
              autofocus
              @keydown.enter="handleCreateSchema"
            />
          </div>
          <FormErrorList :messages="schemaErrors" />

    <template #footer>
          <Button
            class="modal__primary"
            :label="schemaCreating ? 'Создаю...' : 'Создать'"
            :loading="schemaCreating"
            :disabled="!schemaForm.name.trim() || schemaCreating"
            @click="handleCreateSchema"
          />
    </template>
  </AppModal>

  <AppModal
    :open="addFieldOpen"
    title="Добавить поле"
    @update:open="addFieldOpen = $event"
  >
          <div class="field">
            <label>Название поля</label>
            <InputText
              v-model="fieldForm.name"
              type="text"
              placeholder="Например: email"
              autofocus
              @keydown.enter="handleAddField"
            />
          </div>
          <div class="field">
            <label>Тип</label>
            <div class="field-type-picker">
              <Button
                v-for="ft in fieldTypes"
                :key="ft.value"
                class="field-type-option"
                :class="{ 'field-type-option--active': fieldForm.fieldType === ft.value }"
                :label="ft.label"
                text
                type="button"
                @click="fieldForm.fieldType = ft.value"
              />
            </div>
          </div>
          <label class="field-checkbox">
            <Checkbox v-model="fieldForm.isRequired" binary />
            <span>Обязательное поле</span>
          </label>
          <FormErrorList :messages="fieldErrors" />

    <template #footer>
          <Button
            class="modal__primary"
            :label="fieldAdding ? 'Добавляю...' : 'Добавить'"
            :loading="fieldAdding"
            :disabled="!fieldForm.name.trim() || fieldAdding"
            @click="handleAddField"
          />
    </template>
  </AppModal>

  <ConfirmModal
    :open="Boolean(deleteSchemaTarget)"
    title="Удалить схему?"
    :message="`Схема ${deleteSchemaTarget?.name || ''} будет удалена вместе со всеми полями.`"
    confirm-label="Удалить"
    loading-label="Удаляю..."
    danger
    :loading="schemaDeleting"
    :errors="deleteSchemaErrors"
    @update:open="!$event && (deleteSchemaTarget = null)"
    @confirm="handleDeleteSchema"
  />

  <ConfirmModal
    :open="Boolean(deleteVersionTarget)"
    title="Удалить версию?"
    :message="`Версия v${deleteVersionTarget?.version ?? ''} будет удалена из истории релизов.`"
    confirm-label="Удалить"
    loading-label="Удаляю..."
    danger
    :loading="versionDeleting"
    :errors="deleteVersionErrors"
    @update:open="!$event && (deleteVersionTarget = null)"
    @confirm="handleDeleteVersion"
  />
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { botsApi, entitySchemasApi, projectsApi, scenarioApi, userDataApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'
import AppModal from '@/components/ui/AppModal.vue'
import ConfirmModal from '@/components/ui/ConfirmModal.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'
import IconButton from '@/components/ui/IconButton.vue'
import PlatformBadge from '@/components/ui/PlatformBadge.vue'
import StatusPill from '@/components/ui/StatusPill.vue'
import type {
  BotListItem,
  EntitySchemaListItem,
  FieldType,
  GetEntitySchemaResponse,
  GetProjectResponse,
  GetReleasedScenarioResponse,
  GetScenarioDraftResponse,
  PlatformType,
  UserDataRecordItem,
  VersionItem,
} from '@/types/api'
import trashIcon from '@/assets/icons/trash.svg'

const route = useRoute()
const router = useRouter()
const projectId = route.params.id as string

type ProjectTab = 'scenario' | 'schemas' | 'userData'

function normalizeProjectTab(value: unknown): ProjectTab {
  return value === 'schemas' || value === 'userData' ? value : 'scenario'
}

const tab = ref<ProjectTab>(normalizeProjectTab(route.query.tab))

watch(
  () => route.query.tab,
  (value) => {
    tab.value = normalizeProjectTab(value)
  },
)

interface UserDataGroup {
  key: string
  schemaId?: string
  schemaName: string
  scenarioVersion?: number
  recordsCount: number
  updatedAt?: number
}

const project = ref<GetProjectResponse | null>(null)
const draft = ref<GetScenarioDraftResponse | null>(null)
const release = ref<GetReleasedScenarioResponse | null>(null)
const versions = ref<VersionItem[]>([])
const versionsLoading = ref(false)
const promoting = ref(false)
const rolling = ref(false)
const scenarioErrors = ref<string[]>([])
const deleteVersionTarget = ref<VersionItem | null>(null)
const versionDeleting = ref(false)
const deleteVersionErrors = ref<string[]>([])

const bots = ref<BotListItem[]>([])

const schemas = ref<EntitySchemaListItem[]>([])
const schemasLoading = ref(false)
const schemaDetails = ref<Record<string, GetEntitySchemaResponse>>({})
const expandedSchema = ref<string | null>(null)

const createSchemaOpen = ref(false)
const schemaCreating = ref(false)
const schemaErrors = ref<string[]>([])
const schemaForm = reactive({ name: '' })

const addFieldOpen = ref(false)
const addFieldSchemaId = ref<string | null>(null)
const fieldAdding = ref(false)
const fieldErrors = ref<string[]>([])
const fieldForm = reactive({ name: '', fieldType: 'FIELD_TYPE_STRING' as FieldType, isRequired: false })

const deleteSchemaTarget = ref<EntitySchemaListItem | null>(null)
const schemaDeleting = ref(false)
const deleteSchemaErrors = ref<string[]>([])

const userDataRecords = ref<UserDataRecordItem[]>([])
const userDataTotalCount = ref(0)
const userDataLoading = ref(false)
const userDataErrors = ref<string[]>([])

const fieldTypes: { value: FieldType; label: string }[] = [
  { value: 'FIELD_TYPE_STRING', label: 'Текст' },
  { value: 'FIELD_TYPE_NUMBER', label: 'Число' },
  { value: 'FIELD_TYPE_BOOLEAN', label: 'Да/Нет' },
  { value: 'FIELD_TYPE_DATE_TIME', label: 'Дата' },
  { value: 'FIELD_TYPE_REFERENCE', label: 'Ссылка' },
  { value: 'FIELD_TYPE_ENUM', label: 'Список' },
]

const botUsageByVersion = computed(() => {
  const map = new Map<number, number>()
  for (const bot of bots.value) {
    if (!bot.scenarioVersion) continue
    map.set(bot.scenarioVersion, (map.get(bot.scenarioVersion) ?? 0) + 1)
  }

  return map
})

const userDataGroups = computed<UserDataGroup[]>(() => {
  const groups = new Map<string, UserDataGroup>()

  for (const record of userDataRecords.value) {
    const schemaId = record.schemaId ?? ''
    const scenarioVersion = record.scenarioVersion
    const key = `${schemaId}:${scenarioVersion ?? 0}`
    const existing = groups.get(key)

    if (existing) {
      existing.recordsCount += 1
      existing.updatedAt = Math.max(existing.updatedAt ?? 0, record.updatedAt ?? 0)
      continue
    }

    groups.set(key, {
      key,
      schemaId,
      schemaName: record.schemaName || schemaName(schemaId),
      scenarioVersion,
      recordsCount: 1,
      updatedAt: record.updatedAt,
    })
  }

  return Array.from(groups.values()).sort((left, right) => (right.updatedAt ?? 0) - (left.updatedAt ?? 0))
})

onMounted(async () => {
  await Promise.all([
    loadProject(),
    loadScenario(),
    loadVersions(),
    loadSchemas(),
    loadBots(),
    loadUserData(),
  ])
})

function platformLabel(type?: PlatformType): string {
  if (type === 'PLATFORM_TYPE_VK') return 'VK'
  if (type === 'PLATFORM_TYPE_TELEGRAM') return 'Telegram'
  return 'Универсальный'
}

function fieldTypeLabel(type?: FieldType): string {
  return fieldTypes.find(fieldType => fieldType.value === type)?.label ?? type ?? ''
}

function formatTs(ts?: number): string {
  if (!ts) return ''

  return new Date(ts * 1000).toLocaleString('ru-RU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function botCountForVersion(version?: number): number {
  if (!version) return 0
  return botUsageByVersion.value.get(version) ?? 0
}

function canDeleteVersion(version: VersionItem): boolean {
  if (!version.version) return false
  return botCountForVersion(version.version) === 0
}

function schemaFieldCount(schemaId: string): string {
  const details = schemaDetails.value[schemaId]
  if (!details) return '—'
  return String(details.fields?.length ?? 0)
}

function schemaName(schemaId?: string): string {
  if (!schemaId) return '—'
  return schemas.value.find(schema => schema.schemaId === schemaId)?.name ?? 'Схема'
}

function openUserDataTable(group: UserDataGroup): void {
  router.push({
    name: 'project-user-data',
    params: { id: projectId },
    query: {
      schemaId: group.schemaId,
      scenarioVersion: group.scenarioVersion ? String(group.scenarioVersion) : undefined,
    },
  })
}

function openUserDataTableFromRow(event: { data: UserDataGroup }): void {
  openUserDataTable(event.data)
}

function openEditor(): void {
  router.push({ name: 'scenario-editor', params: { id: projectId, sid: 'draft' } })
}

function openReleaseViewer(): void {
  router.push({ name: 'scenario-editor', params: { id: projectId, sid: 'release' } })
}

function openVersionViewer(version: number): void {
  router.push({ name: 'scenario-editor', params: { id: projectId, sid: `version-${version}` } })
}

async function loadProject(): Promise<void> {
  try {
    project.value = await projectsApi.get(projectId)
  } catch {
    project.value = null
  }
}

async function loadScenario(): Promise<void> {
  try {
    draft.value = await scenarioApi.getDraft(projectId)
  } catch {
    draft.value = null
  }

  try {
    release.value = await scenarioApi.getRelease(projectId)
  } catch {
    release.value = null
  }
}

async function loadVersions(): Promise<void> {
  versionsLoading.value = true
  try {
    const response = await scenarioApi.getVersions(projectId)
    versions.value = (response.versions ?? []).slice().reverse()
  } catch {
    versions.value = []
  } finally {
    versionsLoading.value = false
  }
}

async function loadBots(): Promise<void> {
  try {
    const response = await botsApi.listByProject(projectId)
    bots.value = response.items ?? []
  } catch {
    bots.value = []
  }
}

async function loadUserData(): Promise<void> {
  userDataLoading.value = true
  userDataErrors.value = []

  try {
    const pageSize = 200
    const records: UserDataRecordItem[] = []
    let pageOffset = 0
    let totalCount = 0

    do {
      const response = await userDataApi.list(projectId, {
        pageSize,
        pageOffset,
      })

      const items = response.items ?? []
      records.push(...items)
      totalCount = response.totalCount ?? records.length
      pageOffset += items.length

      if (items.length === 0) {
        break
      }
    } while (records.length < totalCount)

    userDataRecords.value = records
    userDataTotalCount.value = totalCount
  } catch (error) {
    userDataRecords.value = []
    userDataTotalCount.value = 0
    userDataErrors.value = parseApiError(error)
  } finally {
    userDataLoading.value = false
  }
}

async function handlePromote(): Promise<void> {
  if (promoting.value) return
  promoting.value = true
  scenarioErrors.value = []

  try {
    await scenarioApi.promote(projectId)
    await Promise.all([loadScenario(), loadVersions(), loadBots()])
  } catch (error) {
    scenarioErrors.value = parseApiError(error)
  } finally {
    promoting.value = false
  }
}

async function handleRollbackTo(version: number): Promise<void> {
  if (rolling.value) return
  rolling.value = true
  scenarioErrors.value = []

  try {
    await scenarioApi.rollback(projectId, version)
    await Promise.all([loadScenario(), loadVersions(), loadBots()])
  } catch (error) {
    scenarioErrors.value = parseApiError(error)
  } finally {
    rolling.value = false
  }
}

function confirmDeleteVersion(version: VersionItem): void {
  deleteVersionTarget.value = version
  deleteVersionErrors.value = []
}

async function handleDeleteVersion(): Promise<void> {
  if (!deleteVersionTarget.value?.version || versionDeleting.value) return
  const version = deleteVersionTarget.value.version
  versionDeleting.value = true
  deleteVersionErrors.value = []

  try {
    await scenarioApi.deleteVersion(projectId, version)
    deleteVersionTarget.value = null
    await Promise.all([loadScenario(), loadVersions(), loadBots()])
  } catch (error) {
    deleteVersionErrors.value = parseApiError(error)
  } finally {
    versionDeleting.value = false
  }
}

async function loadSchemas(): Promise<void> {
  schemasLoading.value = true
  try {
    const response = await entitySchemasApi.list(projectId)
    schemas.value = response.items ?? []
    await Promise.all(
      schemas.value
        .filter(schema => schema.schemaId)
        .map(schema => loadSchemaDetails(schema.schemaId!)),
    )
  } catch {
    schemas.value = []
  } finally {
    schemasLoading.value = false
  }
}

async function loadSchemaDetails(schemaId: string, force = false): Promise<void> {
  if (schemaDetails.value[schemaId] && !force) return

  try {
    schemaDetails.value[schemaId] = await entitySchemasApi.get(schemaId)
  } catch {
    delete schemaDetails.value[schemaId]
  }
}

async function toggleSchema(schemaId: string): Promise<void> {
  if (expandedSchema.value === schemaId) {
    expandedSchema.value = null
    return
  }

  expandedSchema.value = schemaId
  await loadSchemaDetails(schemaId)
}

function openCreateSchema(): void {
  schemaForm.name = ''
  schemaErrors.value = []
  createSchemaOpen.value = true
}

async function handleCreateSchema(): Promise<void> {
  if (!schemaForm.name.trim() || schemaCreating.value) return
  schemaCreating.value = true
  schemaErrors.value = []

  try {
    await entitySchemasApi.create(projectId, { projectId, name: schemaForm.name.trim() })
    createSchemaOpen.value = false
    await loadSchemas()
  } catch (error) {
    schemaErrors.value = parseApiError(error)
  } finally {
    schemaCreating.value = false
  }
}

function openAddField(schemaId: string): void {
  addFieldSchemaId.value = schemaId
  fieldForm.name = ''
  fieldForm.fieldType = 'FIELD_TYPE_STRING'
  fieldForm.isRequired = false
  fieldErrors.value = []
  addFieldOpen.value = true
}

async function handleAddField(): Promise<void> {
  if (!fieldForm.name.trim() || !addFieldSchemaId.value || fieldAdding.value) return
  const schemaId = addFieldSchemaId.value
  fieldAdding.value = true
  fieldErrors.value = []

  try {
    await entitySchemasApi.addField(schemaId, {
      name: fieldForm.name.trim(),
      fieldType: fieldForm.fieldType,
      isRequired: fieldForm.isRequired,
    })
    await loadSchemaDetails(schemaId, true)
    expandedSchema.value = schemaId
    addFieldOpen.value = false
  } catch (error) {
    fieldErrors.value = parseApiError(error)
  } finally {
    fieldAdding.value = false
  }
}

function confirmDeleteSchema(schema: EntitySchemaListItem): void {
  deleteSchemaTarget.value = schema
  deleteSchemaErrors.value = []
}

async function handleDeleteSchema(): Promise<void> {
  if (!deleteSchemaTarget.value?.schemaId || schemaDeleting.value) return
  const schemaId = deleteSchemaTarget.value.schemaId
  schemaDeleting.value = true
  deleteSchemaErrors.value = []

  try {
    await entitySchemasApi.delete(schemaId)
    delete schemaDetails.value[schemaId]
    if (expandedSchema.value === schemaId) expandedSchema.value = null
    deleteSchemaTarget.value = null
    await loadSchemas()
  } catch (error) {
    deleteSchemaErrors.value = parseApiError(error)
  } finally {
    schemaDeleting.value = false
  }
}
</script>

<style scoped>
.project-page {
  padding: 36px 40px 30vh;
  max-width: 1040px;
  margin: 0 auto;
}

.project-page__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 22px;
}

.project-page__title-block {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}

.project-page__back {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: fit-content;
  min-height: 34px;
  padding: 8px 12px;
  text-decoration: none;
}

.project-page__title-row {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.project-page__title {
  margin: 0;
  color: var(--color-text);
  font-size: 24px;
  font-weight: 600;
  line-height: 1.2;
}

.project-page__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.project-page__action {
  width: auto;
  padding: 9px 16px;
}

.project-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  overflow: hidden;
  margin-bottom: 24px;
  background: var(--color-bg-card);
}

.project-summary__item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 16px 18px;
  border-right: 0.5px solid var(--color-border);
}

.project-summary__item:last-child {
  border-right: none;
}

.project-summary__label {
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.project-summary__value {
  color: var(--color-text);
  font-size: 18px;
  font-weight: 600;
}

.project-tabs {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  max-width: 100%;
  padding: 4px;
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-secondary);
  margin-bottom: 22px;
  overflow-x: auto;
}

.project-tab {
  min-height: 34px;
  padding: 8px 14px;
  border: 0.5px solid transparent;
  border-radius: 9px;
  background: none;
  color: var(--color-text-secondary);
  font: inherit;
  font-size: 13px;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s, border-color 0.15s, background 0.15s, box-shadow 0.15s;
}

.project-tab:hover {
  color: var(--color-text);
  background: color-mix(in srgb, var(--color-bg-card) 70%, transparent);
}

.project-tab--active {
  border-color: var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text);
  font-weight: 500;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.project-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.scenario-panel,
.scenario-publish-card,
.project-block {
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-card);
  overflow: hidden;
}

.scenario-publish-card {
  display: grid;
  grid-template-columns: minmax(220px, 1fr) auto;
  align-items: center;
  gap: 16px;
  padding: 18px 20px;
}

.scenario-panel__row {
  display: grid;
  grid-template-columns: minmax(220px, 1fr) auto;
  align-items: center;
  gap: 16px;
  padding: 18px 20px;
  border-bottom: 0.5px solid var(--color-border);
}

.scenario-panel__row--draft {
  grid-template-columns: minmax(220px, 1fr) auto;
}

.scenario-panel__row:last-of-type {
  border-bottom: none;
}

.scenario-panel__main {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.scenario-panel__label,
.project-block__title,
.schema-details__title {
  color: var(--color-text);
  font-size: 15px;
  font-weight: 600;
}

.scenario-panel__desc,
.project-block__desc {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 12px;
}

.scenario-panel__actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.scenario-panel__button,
.project-block__button,
.schema-details__button {
  width: auto;
  padding: 8px 14px;
}

.scenario-panel__button--publish {
  min-width: 190px;
}

.scenario-panel__release {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 14px;
  min-width: 0;
}

.scenario-panel__usage {
  color: var(--color-text-secondary);
  font-size: 13px;
  white-space: nowrap;
}

.project-errors {
  padding: 0 20px 18px;
  color: var(--color-danger);
  font-size: 13px;
}

.project-errors--scenario {
  border: 0.5px solid color-mix(in srgb, var(--color-danger) 35%, var(--color-border));
  border-radius: 12px;
  background: var(--color-bg-card);
  padding: 14px 16px;
}

.project-errors--inside {
  padding-top: 14px;
  padding-bottom: 0;
}

.project-errors p {
  margin: 0;
}

.project-block__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 18px 20px;
  border-bottom: 0.5px solid var(--color-border);
}

.project-block__header--stacked {
  align-items: flex-start;
}

.project-block__title {
  margin: 0 0 4px;
}

.project-state {
  padding: 18px 20px;
  color: var(--color-text-secondary);
  font-size: 13px;
}

.project-state--compact {
  padding: 8px 0;
}

.project-table,
.fields-table {
  width: 100%;
  border-collapse: collapse;
}

.project-table-wrap {
  overflow-x: auto;
}

.project-table th,
.fields-table th {
  padding: 12px 20px;
  text-align: left;
  color: var(--color-text);
  background: var(--color-bg-secondary);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: none;
}

.project-table td,
.fields-table td {
  padding: 14px 20px;
  border-top: 0.5px solid var(--color-border);
  color: var(--color-text-secondary);
  font-size: 13px;
  vertical-align: middle;
}

.project-table__strong {
  color: var(--color-text) !important;
  font-weight: 500;
}

.project-table__actions-head,
.project-table__actions {
  text-align: right !important;
}

.project-table__actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.project-table__clickable {
  cursor: pointer;
}

.project-table__clickable:hover {
  background: color-mix(in srgb, var(--color-primary) 4%, transparent);
}

.project-prime-button {
  width: auto;
  min-height: 38px;
}

.project-prime-button.p-button {
  border: 0.5px solid var(--color-primary);
  border-radius: 8px;
  background: var(--color-primary);
  color: #fff;
  box-shadow: none;
  font: inherit;
  transition: background 0.15s, border-color 0.15s, color 0.15s, opacity 0.15s;
}

.project-prime-button.p-button:not(:disabled):hover {
  border-color: var(--color-primary-hover);
  background: var(--color-primary-hover);
  color: #fff;
}

.project-prime-button.p-button:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--color-primary) 35%, transparent);
  outline-offset: 2px;
  box-shadow: none;
}

.project-prime-button.p-button.p-button-secondary {
  border-color: var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text-label);
}

.project-prime-button.p-button.p-button-secondary:not(:disabled):hover {
  border-color: var(--color-border);
  background: var(--color-bg-secondary);
  color: var(--color-text);
}

.project-prime-button.p-button:disabled {
  opacity: 0.55;
}

.project-prime-button :deep(.p-button-label) {
  font-weight: 600;
}

.user-data-datatable {
  overflow: hidden;
  border-top: 0.5px solid var(--color-border);
}

.user-data-datatable :deep(.p-datatable-table) {
  min-width: 620px;
}

.user-data-datatable :deep(.p-datatable-thead > tr > th) {
  padding: 12px 18px;
  border-color: var(--color-border);
  background: color-mix(in srgb, var(--color-primary) 5%, var(--color-bg-card));
  color: var(--color-text);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0;
  text-align: left;
  text-transform: none;
}

.user-data-datatable :deep(.p-datatable-tbody > tr) {
  background: var(--color-bg-card);
  color: var(--color-text);
}

.user-data-datatable :deep(.p-datatable-tbody > tr > td) {
  padding: 9px 18px;
  border-color: var(--color-border);
  vertical-align: top;
}

.user-data-datatable :deep(.p-datatable-tbody > tr:nth-child(even) > td) {
  background: color-mix(in srgb, var(--color-primary) 3%, var(--color-bg-card));
}

.user-data-datatable :deep(.p-datatable-tbody > tr) {
  cursor: pointer;
  transition: background 0.15s;
}

.user-data-datatable :deep(.p-datatable-tbody > tr:hover > td) {
  background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg-card));
}

.user-data-datatable :deep(.p-datatable-table-container) {
  overflow-x: auto;
}

.user-data-datatable :deep(.user-data-datatable__schema) {
  min-width: 220px;
}

.user-data-datatable :deep(.user-data-datatable__count) {
  min-width: 110px;
}

.user-data-datatable :deep(.user-data-datatable__date) {
  min-width: 160px;
}

.record-schema {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.record-schema__version {
  color: var(--color-text-secondary);
  font-size: 12px;
}

.schema-details-row td {
  padding: 0;
  background: color-mix(in srgb, var(--color-primary) 4%, var(--color-bg-card));
}

.schema-details {
  padding: 18px 20px 20px;
  border-top: 0.5px solid var(--color-border);
}

.schema-details__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}

.fields-datatable {
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-card);
}

.fields-datatable :deep(.p-datatable-table) {
  min-width: 520px;
}

.fields-datatable :deep(.p-datatable-thead > tr > th) {
  padding: 12px 14px;
  border: 0;
  border-bottom: 0.5px solid var(--color-border);
  background: color-mix(in srgb, var(--color-primary) 7%, var(--color-bg-card));
  color: var(--color-text);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: none;
}

.fields-datatable :deep(.p-datatable-tbody > tr) {
  background: var(--color-bg-card);
  color: var(--color-text);
}

.fields-datatable :deep(.p-datatable-tbody > tr > td) {
  padding: 13px 14px;
  border: 0;
  border-bottom: 0.5px solid var(--color-border);
}

.fields-datatable :deep(.p-datatable-tbody > tr:last-child > td) {
  border-bottom: 0;
}

.fields-datatable :deep(.p-datatable-table-container) {
  overflow-x: auto;
}

.field-type-picker {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.field-type-option {
  padding: 7px 12px;
  border: 1.5px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font: inherit;
  font-size: 12px;
  cursor: pointer;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
}

.field-type-option:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
}

.field-type-option--active {
  border-color: var(--color-primary);
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
  color: var(--color-primary);
}

.field-checkbox {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--color-text);
  font-size: 13px;
  cursor: pointer;
}

.field-checkbox input {
  width: 15px;
  height: 15px;
  accent-color: var(--color-primary);
}

@media (max-width: 900px) {
  .project-page {
    padding: 28px 20px 30vh;
  }

  .project-page__header {
    align-items: flex-start;
    flex-direction: column;
  }

  .project-page__actions {
    width: 100%;
  }

  .project-page__action {
    flex: 1;
  }

  .project-summary {
    grid-template-columns: 1fr;
  }

  .project-summary__item {
    border-right: none;
  }

  .project-summary__item:not(:last-child) {
    border-bottom: 0.5px solid var(--color-border);
  }

  .project-tabs {
    display: flex;
    width: 100%;
  }

  .project-tab {
    flex: 0 0 auto;
  }

  .scenario-panel__row {
    grid-template-columns: 1fr;
    align-items: flex-start;
  }

  .scenario-publish-card {
    grid-template-columns: 1fr;
    align-items: flex-start;
  }

  .scenario-panel__actions {
    width: 100%;
  }

  .scenario-panel__release {
    width: 100%;
    justify-content: flex-start;
    flex-wrap: wrap;
  }

  .scenario-panel__button {
    flex: 1;
  }

  .project-table {
    min-width: 680px;
  }

  .project-block {
    overflow-x: auto;
  }
}
</style>
