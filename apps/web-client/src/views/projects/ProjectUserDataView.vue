<template>
  <div class="user-data-page">
    <div class="user-data-page__header">
      <div>
        <h1 class="user-data-page__title">Данные пользователей</h1>
        <p class="user-data-page__desc">
          {{ project?.name ?? 'Проект' }}
          <span v-if="activeSchemaName"> / {{ activeSchemaName }}</span>
          <span v-if="activeVersionLabel"> / {{ activeVersionLabel }}</span>
        </p>
      </div>
      <div class="user-data-page__actions">
        <Button
          class="user-data-prime-button"
          label="Назад"
          severity="secondary"
          outlined
          @click="goBackToProject"
        />
        <Button
          class="user-data-prime-button"
          label="Обновить"
          severity="secondary"
          outlined
          :disabled="loading"
          @click="loadData"
        />
      </div>
    </div>

    <div class="user-data-panel">
      <div class="user-data-filters">
        <div class="user-data-filter">
          <label>Схема</label>
          <Select
            v-model="filters.schemaId"
            :options="schemaOptions"
            option-label="label"
            option-value="value"
            class="user-data-filter__control"
            overlay-class="user-data-schema-select-overlay"
            @change="applyFilters"
          />
        </div>
      </div>

      <div v-if="errors.length" class="user-data-errors">
        <p v-for="(message, index) in errors" :key="index">{{ message }}</p>
      </div>

      <div v-if="loading" class="user-data-state">Загрузка данных...</div>
      <div v-else-if="!records.length" class="user-data-state">Записей пока нет</div>
      <DataTable
        v-else
        :value="tableRows"
        data-key="recordId"
        class="user-data-grid"
        show-gridlines
        striped-rows
        size="small"
        scrollable
        scroll-height="flex"
        :row-class="rowClass"
      >
        <Column header="Пользователь" class="user-data-grid__user">
          <template #body="{ data: record }">
            <span
              v-if="!record.__placeholder"
              class="user-data-cell"
              :title="record.platformUserId || ''"
            >
              {{ record.platformUserId || '—' }}
            </span>
            <span v-else class="user-data-cell user-data-cell--empty"></span>
          </template>
        </Column>

        <Column
          v-for="column in dataColumns"
          :key="column.key"
          :header="column.header"
          class="user-data-grid__field"
        >
          <template #body="{ data: record }">
            <span
              v-if="!record.__placeholder"
              class="user-data-cell"
              :title="recordValue(record, column.key)"
            >
              {{ recordValue(record, column.key) }}
            </span>
            <span v-else class="user-data-cell user-data-cell--empty"></span>
          </template>
        </Column>

        <Column header="Бот" class="user-data-grid__bot">
          <template #body="{ data: record }">
            <span
              v-if="!record.__placeholder"
              class="user-data-cell"
              :title="botName(record.botId)"
            >
              {{ botName(record.botId) }}
            </span>
            <span v-else class="user-data-cell user-data-cell--empty"></span>
          </template>
        </Column>
        <Column header="Обновлено" class="user-data-grid__date">
          <template #body="{ data: record }">
            <span v-if="!record.__placeholder">{{ formatTs(record.updatedAt) }}</span>
            <span v-else class="user-data-cell user-data-cell--empty"></span>
          </template>
        </Column>
      </DataTable>

      <div class="user-data-pagination">
        <span>{{ totalCount ? `${pageStart + 1}-${pageEnd} из ${totalCount}` : '0 записей' }}</span>
        <div class="user-data-pagination__actions">
          <Button
            class="user-data-prime-button"
            label="Назад"
            severity="secondary"
            outlined
            :disabled="page === 0 || loading"
            @click="prevPage"
          />
          <Button
            class="user-data-prime-button"
            label="Далее"
            severity="secondary"
            outlined
            :disabled="!hasNextPage || loading"
            @click="nextPage"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Select from 'primevue/select'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { botsApi, entitySchemasApi, projectsApi, userDataApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'
import type {
  BotListItem,
  EntitySchemaListItem,
  GetEntitySchemaResponse,
  GetProjectResponse,
  UserDataRecordItem,
} from '@/types/api'

const route = useRoute()
const router = useRouter()
const projectId = route.params.id as string
const pageSize = 50

const project = ref<GetProjectResponse | null>(null)
const schemas = ref<EntitySchemaListItem[]>([])
const schemaDetails = ref<Record<string, GetEntitySchemaResponse>>({})
const bots = ref<BotListItem[]>([])
const records = ref<UserDataRecordItem[]>([])
const totalCount = ref(0)
const loading = ref(false)
const errors = ref<string[]>([])
const page = ref(0)
const selectedRecordId = computed(() => typeof route.query.recordId === 'string' ? route.query.recordId : '')

type UserDataTableRow = UserDataRecordItem & { __placeholder?: true }

const minVisibleRows = 5
const tableRows = computed<UserDataTableRow[]>(() => {
  const rows = records.value as UserDataTableRow[]
  if (!rows.length) return rows

  const placeholderCount = Math.max(0, minVisibleRows - rows.length)
  return [
    ...rows,
    ...Array.from({ length: placeholderCount }, (_, index) => ({
      recordId: `__empty-${index}`,
      __placeholder: true,
    } as UserDataTableRow)),
  ]
})

function goBackToProject(): void {
  router.push({ name: 'project', params: { id: projectId }, query: { tab: 'userData' } })
}

const filters = reactive({
  schemaId: typeof route.query.schemaId === 'string' ? route.query.schemaId : '',
  scenarioVersion: typeof route.query.scenarioVersion === 'string' ? route.query.scenarioVersion : '',
})

const schemaOptions = computed(() => [
  { label: 'Все схемы', value: '' },
  ...schemas.value.map(schema => ({
    label: schema.name ?? schema.schemaId ?? 'Без названия',
    value: schema.schemaId ?? '',
  })),
])

const activeSchemaName = computed(() => {
  if (!filters.schemaId) return ''
  return schemaDetails.value[filters.schemaId]?.name
    ?? schemas.value.find(schema => schema.schemaId === filters.schemaId)?.name
    ?? ''
})
const activeVersionLabel = computed(() => filters.scenarioVersion ? `v${filters.scenarioVersion}` : '')

const dataColumns = computed(() => {
  const keys: string[] = []
  const addKey = (key?: string) => {
    if (!key || keys.includes(key)) return
    keys.push(key)
  }

  if (filters.schemaId) {
    schemaDetails.value[filters.schemaId]?.fields?.forEach(field => addKey(field.name))
  }

  for (const record of records.value) {
    if (!filters.schemaId && record.schemaId) {
      schemaDetails.value[record.schemaId]?.fields?.forEach(field => addKey(field.name))
    }

    Object.keys(parseRecordData(record.dataJson)).forEach(addKey)
  }

  return keys.map(key => ({ key, header: key }))
})

const pageStart = computed(() => page.value * pageSize)
const pageEnd = computed(() => Math.min(pageStart.value + records.value.length, totalCount.value))
const hasNextPage = computed(() => pageEnd.value < totalCount.value)

onMounted(async () => {
  await Promise.all([loadProject(), loadSchemas(), loadBots()])
  await loadData()
})

async function loadProject(): Promise<void> {
  try {
    project.value = await projectsApi.get(projectId)
  } catch {
    project.value = null
  }
}

async function loadSchemas(): Promise<void> {
  const response = await entitySchemasApi.list(projectId)
  schemas.value = response.items ?? []

  await Promise.all(
    schemas.value
      .map(schema => schema.schemaId)
      .filter((schemaId): schemaId is string => Boolean(schemaId))
      .map(loadSchemaDetails),
  )
}

async function loadSchemaDetails(schemaId: string): Promise<void> {
  if (schemaDetails.value[schemaId]) return
  try {
    schemaDetails.value[schemaId] = await entitySchemasApi.get(schemaId)
  } catch {
    schemaDetails.value[schemaId] = { schemaId, name: schemaName(schemaId), fields: [] }
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

async function loadData(): Promise<void> {
  loading.value = true
  errors.value = []

  try {
    if (filters.schemaId) await loadSchemaDetails(filters.schemaId)

    const response = await userDataApi.list(projectId, {
      schemaId: filters.schemaId || undefined,
      scenarioVersion: filters.scenarioVersion ? Number(filters.scenarioVersion) : undefined,
      pageSize,
      pageOffset: page.value * pageSize,
    })

    records.value = response.items ?? []
    totalCount.value = response.totalCount ?? 0
  } catch (error) {
    records.value = []
    totalCount.value = 0
    errors.value = parseApiError(error)
  } finally {
    loading.value = false
  }
}

function applyFilters(): void {
  page.value = 0
  router.replace({
    name: 'project-user-data',
    params: { id: projectId },
    query: {
      schemaId: filters.schemaId || undefined,
      scenarioVersion: filters.scenarioVersion || undefined,
    },
  })
  void loadData()
}

function prevPage(): void {
  if (page.value === 0) return
  page.value -= 1
  void loadData()
}

function nextPage(): void {
  if (!hasNextPage.value) return
  page.value += 1
  void loadData()
}

function schemaName(schemaId?: string): string {
  if (!schemaId) return 'Схема'
  return schemas.value.find(schema => schema.schemaId === schemaId)?.name ?? 'Схема'
}

function botName(botId?: string): string {
  if (!botId) return 'Бот не найден'
  return bots.value.find(bot => bot.botInstanceId === botId)?.name ?? botId
}

function recordValue(record: UserDataTableRow, key: string): string {
  if (record.__placeholder) return ''
  return formatRecordValue(parseRecordData(record.dataJson)[key])
}

function parseRecordData(dataJson?: string): Record<string, unknown> {
  if (!dataJson) return {}

  try {
    const parsed = JSON.parse(dataJson)
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? parsed as Record<string, unknown>
      : {}
  } catch {
    return {}
  }
}

function formatRecordValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return JSON.stringify(value)
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

function rowClass(record: UserDataTableRow): string {
  if (record.__placeholder) return 'user-data-grid__row--empty'
  return record.recordId && record.recordId === selectedRecordId.value ? 'user-data-grid__row--selected' : ''
}
</script>

<style scoped>
.user-data-page {
  width: min(1160px, calc(100vw - 48px));
  margin: 0 auto;
  padding: 36px 0 30vh;
}

.user-data-page__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 24px;
}

.user-data-page__title {
  margin: 0 0 4px;
  color: var(--color-text);
  font-size: 24px;
  font-weight: 600;
}

.user-data-page__desc {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 13px;
}

.user-data-page__actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.user-data-panel {
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-bg-card);
}

.user-data-filters {
  display: grid;
  grid-template-columns: minmax(220px, 1fr);
  gap: 12px;
  padding: 16px 20px;
  border-bottom: 0.5px solid var(--color-border);
}

.user-data-filter {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.user-data-filter label {
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.user-data-filter__control,
.user-data-filter :deep(.p-select) {
  width: 100%;
  min-height: 38px;
}

.user-data-filter :deep(.p-select) {
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  box-shadow: none;
}

.user-data-filter :deep(.p-select.p-focus) {
  border-color: var(--color-primary);
  box-shadow: none;
}

.user-data-filter :deep(.p-select-label) {
  padding: 8px 12px;
  font: inherit;
  font-size: 13px;
}

:global(.user-data-schema-select-overlay) {
  border: 0.5px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-card);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.12);
  overflow: hidden;
}

:global(.user-data-schema-select-overlay .p-select-option) {
  min-height: 34px;
  color: var(--color-text);
  font-size: 13px;
}

:global(.user-data-schema-select-overlay .p-select-option[data-p-focused='true']) {
  background: var(--color-bg-secondary);
  color: var(--color-text);
}

:global(.user-data-schema-select-overlay .p-select-option[data-p-selected='true']),
:global(.user-data-schema-select-overlay .p-select-option[aria-selected='true']) {
  background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg-card));
  color: var(--color-text);
}

:global(.user-data-schema-select-overlay .p-select-option[data-p-selected='true'][data-p-focused='true']),
:global(.user-data-schema-select-overlay .p-select-option[aria-selected='true'][data-p-focused='true']) {
  background: color-mix(in srgb, var(--color-primary) 10%, var(--color-bg-card));
  color: var(--color-text);
}

.user-data-prime-button.p-button {
  width: auto;
  min-height: 38px;
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-bg-card);
  color: var(--color-text-label);
  box-shadow: none;
  font: inherit;
}

.user-data-prime-button.p-button:not(:disabled):hover {
  border-color: var(--color-border);
  background: var(--color-bg-secondary);
  color: var(--color-text);
}

.user-data-prime-button :deep(.p-button-label) {
  font-weight: 600;
}

.user-data-errors,
.user-data-state {
  padding: 18px 20px;
  color: var(--color-text-secondary);
  font-size: 13px;
}

.user-data-errors {
  color: var(--color-danger);
}

.user-data-errors p {
  margin: 0;
}

.user-data-grid :deep(.p-datatable-table) {
  min-width: 980px;
}

.user-data-grid :deep(.p-datatable-thead > tr > th) {
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

.user-data-grid :deep(.p-datatable-tbody > tr) {
  background: var(--color-bg-card);
  color: var(--color-text);
}

.user-data-grid :deep(.p-datatable-tbody > tr.user-data-grid__row--selected) {
  background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg-card));
}

.user-data-grid :deep(.p-datatable-tbody > tr > td) {
  padding: 10px 18px;
  border-color: var(--color-border);
  vertical-align: top;
}

.user-data-grid :deep(.p-datatable-tbody > tr:nth-child(even) > td) {
  background: color-mix(in srgb, var(--color-primary) 3%, var(--color-bg-card));
}

.user-data-grid :deep(.p-datatable-tbody > tr:hover > td) {
  background: color-mix(in srgb, var(--color-primary) 8%, var(--color-bg-card));
}

.user-data-grid :deep(.p-datatable-tbody > tr.user-data-grid__row--empty > td) {
  height: 42px;
  background: var(--color-bg-card);
}

.user-data-grid :deep(.p-datatable-tbody > tr.user-data-grid__row--empty:hover > td) {
  background: var(--color-bg-card);
}

.user-data-grid :deep(.p-datatable-table-container) {
  overflow-x: auto;
}

.user-data-grid :deep(.user-data-grid__field) {
  min-width: 180px;
}

.user-data-grid :deep(.user-data-grid__user),
.user-data-grid :deep(.user-data-grid__bot) {
  min-width: 150px;
}

.user-data-grid :deep(.user-data-grid__date) {
  min-width: 170px;
}

.user-data-cell {
  display: block;
  max-width: 260px;
  overflow: hidden;
  color: var(--color-text);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-data-cell--empty {
  min-height: 20px;
}

.user-data-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-top: 0.5px solid var(--color-border);
  color: var(--color-text-secondary);
  font-size: 13px;
}

.user-data-pagination__actions {
  display: flex;
  gap: 8px;
}

@media (max-width: 800px) {
  .user-data-page {
    width: calc(100vw - 32px);
    padding-top: 28px;
  }

  .user-data-page__header,
  .user-data-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .user-data-page__actions {
    width: 100%;
  }

  .user-data-filters {
    grid-template-columns: 1fr;
  }
}
</style>
