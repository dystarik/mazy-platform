<template>
  <BaseNode
    class="vk-send-carousel-node"
    :class="{ 'vk-send-carousel-node--branched': carouselOutputPorts.length > 0 }"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :style="nodeLayoutStyle"
    :has-output="carouselOutputPorts.length === 0"
    :has-output-ports="carouselOutputPorts.length > 0"
    :output-ports="carouselOutputPorts"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="vk-send-carousel-node__body">
      <label class="vk-send-carousel-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="vk-send-carousel-node__label">Текст</span>
        <EditorGridTextarea
          :model-value="text"
          :readonly="isReadOnly"
          placeholder="Сообщение над каруселью"
          :min-rows="2"
          :height="textHeight"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('text', value)"
          @update:height="updateTextHeight"
        />
      </label>

      <div class="vk-send-carousel-node__cards">
        <div class="vk-send-carousel-node__cards-head">
          <span>Карточки</span>
          <Button
            v-if="!isReadOnly"
            class="vk-send-carousel-node__add-card nodrag"
            type="button"
            label="+"
            title="Добавить карточку"
            @mousedown.stop
            @pointerdown.stop
            @click.stop="addCard"
          />
        </div>

        <Button
          v-if="!isReadOnly && cards.length === 0"
          class="vk-send-carousel-node__first-card nodrag"
          type="button"
          label="Добавить карточку"
          @mousedown.stop
          @pointerdown.stop
          @click.stop="addCard"
        />

        <div
          v-for="(card, cardIndex) in cards"
          :key="`card-${cardIndex}`"
          class="vk-send-carousel-node__card nodrag"
          @mousedown.stop
          @pointerdown.stop
        >
          <div class="vk-send-carousel-node__card-head">
            <span>Карточка {{ cardIndex + 1 }}</span>
            <Button
              v-if="!isReadOnly"
              class="vk-send-carousel-node__remove-card"
              type="button"
              label="×"
              title="Удалить карточку"
              @click.stop="removeCard(cardIndex)"
            />
          </div>

          <label class="vk-send-carousel-node__field vk-send-carousel-node__field--inner">
            <span class="vk-send-carousel-node__label">Заголовок</span>
            <EditorVariableInput
              :model-value="card.title"
              :readonly="isReadOnly"
              placeholder="Название"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateCardField(cardIndex, 'title', stringValue(value))"
            />
          </label>

          <label class="vk-send-carousel-node__field vk-send-carousel-node__field--inner">
            <span class="vk-send-carousel-node__label">Описание</span>
            <EditorGridTextarea
              :model-value="card.description"
              :readonly="isReadOnly"
              placeholder="Описание карточки"
              :min-rows="2"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateCardField(cardIndex, 'description', value)"
            />
          </label>

          <label class="vk-send-carousel-node__field vk-send-carousel-node__field--inner">
            <span class="vk-send-carousel-node__label">photoId</span>
            <EditorVariableInput
              :model-value="card.photoId"
              :readonly="isReadOnly"
              placeholder="photo123_456"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateCardField(cardIndex, 'photoId', stringValue(value))"
            />
          </label>

          <div class="vk-send-carousel-node__buttons">
            <div class="vk-send-carousel-node__buttons-head">
              <span>Кнопки</span>
              <Button
                v-if="!isReadOnly"
                class="vk-send-carousel-node__add-button"
                type="button"
                label="+"
                title="Добавить кнопку"
                @click.stop="addButton(cardIndex)"
              />
            </div>

            <Button
              v-if="!isReadOnly && card.buttons.length === 0"
              class="vk-send-carousel-node__first-button"
              type="button"
              label="Добавить кнопку"
              @click.stop="addButton(cardIndex)"
            />

            <EditorButtonPortRow
              v-for="(button, buttonIndex) in card.buttons"
              :key="buttonKey(button, buttonIndex)"
            >
              <div
                class="vk-send-carousel-node__button"
                :class="{ 'vk-send-carousel-node__button--readonly': isReadOnly }"
              >
                <EditorVariableInput
                  class="vk-send-carousel-node__button-input vk-send-carousel-node__button-input--label"
                  :model-value="button.label"
                  :readonly="isReadOnly"
                  placeholder="Текст"
                  :known-variables="knownVariables"
                  :variable-scope="variableScope"
                  @update:model-value="value => updateButtonField(cardIndex, buttonIndex, 'label', stringValue(value))"
                />
                <Select
                  v-if="!isReadOnly"
                  class="vk-send-carousel-node__button-style"
                  :model-value="button.style ?? DEFAULT_BUTTON_STYLE"
                  :options="styleOptions"
                  option-label="label"
                  option-value="value"
                  overlay-class="editor-node-select-overlay"
                  title="Стиль кнопки"
                  @update:model-value="value => updateButtonField(cardIndex, buttonIndex, 'style', normalizeStyle(value))"
                />
                <EditorVariableInput
                  class="vk-send-carousel-node__button-link"
                  :model-value="button.link ?? ''"
                  :readonly="isReadOnly"
                  placeholder="link"
                  :known-variables="knownVariables"
                  :variable-scope="variableScope"
                  @update:model-value="value => updateButtonField(cardIndex, buttonIndex, 'link', stringValue(value))"
                />
                <Button
                  v-if="!isReadOnly"
                  class="vk-send-carousel-node__remove-button"
                  type="button"
                  label="×"
                  title="Удалить кнопку"
                  @click.stop="removeButton(cardIndex, buttonIndex)"
                />
              </div>
            </EditorButtonPortRow>
          </div>
        </div>
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Button from 'primevue/button'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import { type EditorNodeUiState } from '@/components/editor/editorTypes'
import { describeEditorNodeLayout } from '@/components/editor/editorNodeLayoutContract'
import EditorButtonPortRow from '@/components/editor/EditorButtonPortRow.vue'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'

const BUTTON_STYLES = ['primary', 'secondary', 'success', 'danger'] as const
type CarouselButtonStyle = typeof BUTTON_STYLES[number]

interface VkCarouselCard {
  title: string
  description: string
  photoId: string
  buttons: VkCarouselButton[]
}

interface VkCarouselButton {
  label: string
  payload: string
  style?: CarouselButtonStyle
  link?: string
}

type VkCarouselCardKey = 'title' | 'description' | 'photoId'
type VkCarouselButtonKey = 'label' | 'style' | 'link'

const DEFAULT_BUTTON_STYLE: CarouselButtonStyle = 'primary'
const styleOptions = [
  { value: 'primary', label: 'Осн.' },
  { value: 'secondary', label: 'Доп.' },
  { value: 'success', label: 'Усп.' },
  { value: 'danger', label: 'Опас.' },
]

const props = defineProps<{
  nodeType: string
  params: Record<string, unknown>
  catalogParams: NodeParamItem[]
  isStart: boolean
  isSelected: boolean
  isReadOnly?: boolean
  isPickable?: boolean
  isPickTarget?: boolean
  isRelated?: boolean
  label: string
  uiState?: EditorNodeUiState
  knownVariables?: string[]
  variableScope?: VariableScope
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'update-ui': [value: EditorNodeUiState]
  'set-start': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const text = computed(() => stringValue(props.params.text))
const textHeight = computed(() => props.uiState?.textHeight)
const cards = computed(() => readCards(props.params.cards))
const layoutContract = computed(() => describeEditorNodeLayout({ type: props.nodeType, params: props.params }))
const carouselOutputPorts = computed(() => layoutContract.value.outputPorts)
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutContract.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutContract.value.width}px`,
  '--node-wide-width': `${layoutContract.value.width}px`,
  '--node-min-height': `${layoutContract.value.height}px`,
}) as CSSProperties)

function addCard(): void {
  if (props.isReadOnly) return
  emit('update-param', 'cards', [
    ...cloneCards(),
    {
      title: `Карточка ${cards.value.length + 1}`,
      description: '',
      photoId: '',
      buttons: [],
    },
  ])
}

function removeCard(cardIndex: number): void {
  if (props.isReadOnly) return
  const next = cloneCards()
  next.splice(cardIndex, 1)
  emit('update-param', 'cards', next)
}

function updateCardField(cardIndex: number, key: VkCarouselCardKey, value: string): void {
  if (props.isReadOnly) return
  const next = cloneCards()
  const card = next[cardIndex]
  if (!card) return

  card[key] = value
  emit('update-param', 'cards', next)
}

function addButton(cardIndex: number): void {
  if (props.isReadOnly) return
  const next = cloneCards()
  const card = next[cardIndex]
  if (!card) return

  const payload = createButtonPayload(next.flatMap(item => item.buttons))
  card.buttons.push({
    label: `Кнопка ${card.buttons.length + 1}`,
    payload,
    style: DEFAULT_BUTTON_STYLE,
  })
  emit('update-param', 'cards', next)
}

function removeButton(cardIndex: number, buttonIndex: number): void {
  if (props.isReadOnly) return
  const next = cloneCards()
  next[cardIndex]?.buttons.splice(buttonIndex, 1)
  emit('update-param', 'cards', next)
}

function updateButtonField(
  cardIndex: number,
  buttonIndex: number,
  key: VkCarouselButtonKey,
  value: string,
): void {
  if (props.isReadOnly) return
  const next = cloneCards()
  const button = next[cardIndex]?.buttons[buttonIndex]
  if (!button) return

  if (key === 'style') {
    button.style = normalizeStyle(value)
  } else if (key === 'link') {
    if (value.trim()) {
      button.link = value
    } else {
      delete button.link
    }
  } else {
    button[key] = value
  }
  emit('update-param', 'cards', next)
}

function updateParam(key: string, value: unknown): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function updateTextHeight(value: number): void {
  if (props.isReadOnly) return
  emit('update-ui', { ...(props.uiState ?? {}), textHeight: value })
}

function cloneCards(): VkCarouselCard[] {
  return cards.value.map(card => ({
    ...card,
    buttons: card.buttons.map(button => ({ ...button })),
  }))
}

function readCards(value: unknown): VkCarouselCard[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  return value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map((item) => {
      return {
        title: stringValue(item.title),
        description: stringValue(item.description),
        photoId: stringValue(item.photoId),
        buttons: readCarouselButtons(item.buttons, usedPayloads),
      }
    })
}

function readCarouselButtons(value: unknown, usedPayloads: Set<string>): VkCarouselButton[] {
  if (!Array.isArray(value)) return []

  const sourceRows = value.every(item => Array.isArray(item))
    ? value
    : [value]
  const result: VkCarouselButton[] = []

  for (const row of sourceRows) {
    if (!Array.isArray(row)) continue
    for (const item of row) {
      if (!isRecord(item)) continue
      const label = stringValue(item.label)
      const payload = stringValue(item.payload)
      const link = stringValue(item.link)
      if (!label && !payload && !link) continue

      const generatedPayload = link
        ? payload
        : payload
          ? reserveUniquePayload(payload, usedPayloads)
          : createUniqueStableCarouselPayload(label || link, result.length, usedPayloads)
      result.push({
        label: label || payload || link,
        payload: generatedPayload,
        ...(isButtonStyle(item.style) ? { style: item.style } : {}),
        ...(link ? { link } : {}),
      })
    }
  }

  return result
}

function reserveUniquePayload(payload: string, usedPayloads: Set<string>): string {
  let candidate = payload
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${payload}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function createButtonPayload(buttons: VkCarouselButton[]): string {
  const usedPayloads = new Set(buttons.map(button => button.payload))
  let index = buttons.length + 1
  let candidate = `button_${index}`

  while (usedPayloads.has(candidate)) {
    index += 1
    candidate = `button_${index}`
  }

  return candidate
}

function createUniqueStableCarouselPayload(label: string, index: number, usedPayloads: Set<string>): string {
  const base = label.trim()
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^\p{L}\p{N}_-]+/gu, '_')
    .replace(/^_+|_+$/g, '')
    || `button_${index + 1}`

  let candidate = base
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${base}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function normalizeStyle(value: unknown): CarouselButtonStyle {
  return isButtonStyle(value) ? value : DEFAULT_BUTTON_STYLE
}

function isButtonStyle(value: unknown): value is CarouselButtonStyle {
  return typeof value === 'string' && (BUTTON_STYLES as readonly string[]).includes(value)
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function buttonKey(button: VkCarouselButton, index: number): string {
  return button.payload || button.link || `${button.label}-${index}`
}

</script>

<style scoped>
.vk-send-carousel-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.vk-send-carousel-node__field {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.vk-send-carousel-node__field--compact,
.vk-send-carousel-node__cards {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.vk-send-carousel-node__field--inner {
  padding-bottom: 12px;
}

.vk-send-carousel-node__label,
.vk-send-carousel-node__cards-head,
.vk-send-carousel-node__card-head,
.vk-send-carousel-node__buttons-head {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: left;
}

.vk-send-carousel-node__label {
  align-self: flex-start;
}

.vk-send-carousel-node__cards {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.vk-send-carousel-node__cards-head,
.vk-send-carousel-node__card-head,
.vk-send-carousel-node__buttons-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 24px;
}

.vk-send-carousel-node__add-card,
.vk-send-carousel-node__remove-card,
.vk-send-carousel-node__add-button,
.vk-send-carousel-node__remove-button {
  width: 24px;
  height: 24px;
  min-width: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 4px;
  background: var(--color-bg);
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.vk-send-carousel-node__add-card:hover,
.vk-send-carousel-node__add-button:hover {
  color: var(--color-primary);
}

.vk-send-carousel-node__remove-card:hover,
.vk-send-carousel-node__remove-button:hover {
  border-color: var(--color-danger);
  color: var(--color-danger);
}

.vk-send-carousel-node__first-card,
.vk-send-carousel-node__first-button {
  box-sizing: border-box;
  width: 100%;
  height: 24px;
  min-height: 24px;
  border: 1px dashed color-mix(in srgb, var(--color-primary) 48%, var(--color-border));
  border-radius: 4px;
  background: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg));
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  font-size: 10px;
  line-height: 12px;
  padding: 0 6px;
}

.vk-send-carousel-node__card {
  display: flex;
  flex-direction: column;
  gap: 0;
  border: 1px solid color-mix(in srgb, var(--color-primary) 24%, var(--color-border));
  border-radius: 6px;
  background: color-mix(in srgb, var(--color-primary) 3%, var(--color-bg));
  padding: 12px;
}

.vk-send-carousel-node__buttons {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.vk-send-carousel-node__button {
  box-sizing: border-box;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 54px minmax(48px, 0.7fr) 24px;
  gap: 0;
  min-width: 0;
  height: 24px;
  min-height: 24px;
  max-height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  overflow: hidden;
}

.vk-send-carousel-node__button--readonly {
  grid-template-columns: minmax(0, 1fr) minmax(48px, 0.7fr);
}

.vk-send-carousel-node__buttons .editor-button-port-row + .editor-button-port-row,
.vk-send-carousel-node__first-button + .editor-button-port-row {
  margin-top: 12px;
}

.vk-send-carousel-node__button-input {
  min-width: 0;
}

.vk-send-carousel-node__button-input :deep(.editor-variable-input__highlight),
.vk-send-carousel-node__button-input :deep(.editor-variable-input__control),
.vk-send-carousel-node__button-link :deep(.editor-variable-input__highlight),
.vk-send-carousel-node__button-link :deep(.editor-variable-input__control) {
  height: 24px;
  border: 0;
  border-radius: 0;
  background: transparent !important;
  line-height: 18px;
  padding: 3px 6px;
}

.vk-send-carousel-node__button-style,
.vk-send-carousel-node__button-link,
.vk-send-carousel-node__remove-button {
  border-left: 1px solid var(--color-border-input);
}

.vk-send-carousel-node__button-style {
  width: 54px;
  min-width: 54px;
  height: 24px;
  border-top: 0 !important;
  border-right: 0 !important;
  border-bottom: 0 !important;
  border-radius: 0 !important;
  background: transparent !important;
  font-size: 8px;
}

.vk-send-carousel-node__button-style :deep(.p-select-label) {
  padding: 4px 2px 4px 6px;
  font-size: 8px;
  line-height: 14px;
}

.vk-send-carousel-node__button-style :deep(.p-select-dropdown) {
  width: 18px;
}

.vk-send-carousel-node__remove-button {
  border-top: 0;
  border-right: 0;
  border-bottom: 0;
  border-radius: 0;
}

.vk-send-carousel-node__button-link {
  min-width: 0;
}

</style>
