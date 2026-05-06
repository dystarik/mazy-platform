/**
 * Single source of truth для UI-метаданных узлов сценария:
 * человекочитаемые метки и категория для sidebar.
 *
 * Все типы — реальные узлы из бэкенд-каталога. Узел появится в sidebar
 * только если он есть в NODE_META.
 */
import databaseIcon from '@/assets/icons/database.svg'
import globeIcon from '@/assets/icons/globe.svg'
import messageIcon from '@/assets/icons/message-square.svg'
import splitIcon from '@/assets/icons/split.svg'
import userIcon from '@/assets/icons/user.svg'
import vkIcon from '@/assets/icons/VK Logo Black & White.svg'

// ── Категории ────────────────────────────────────────────────────────────────

export type CategoryKey =
  | 'messageSending'
  | 'messageReceiving'
  | 'messageManagement'
  | 'logic'
  | 'data'
  | 'user'
  | 'integrations'
  | 'vk'

export interface CategoryMeta {
  /** Стабильный ключ категории — используется в NodeMeta.category */
  key: CategoryKey
  /** Лейбл в sidebar */
  label: string
  /** Цвет категории (для иконки и подсветки) */
  color: string
  /** SVG из assets */
  iconSrc: string
}

export const CATEGORIES: Readonly<Record<CategoryKey, CategoryMeta>> = {
  messageSending: {
    key:   'messageSending',
    label: 'Отправка',
    color: '#0096c7',
    iconSrc: messageIcon,
  },
  messageReceiving: {
    key:   'messageReceiving',
    label: 'Ожидание ответа',
    color: '#0096c7',
    iconSrc: messageIcon,
  },
  messageManagement: {
    key:   'messageManagement',
    label: 'Управление сообщением',
    color: '#0096c7',
    iconSrc: messageIcon,
  },
  logic: {
    key:   'logic',
    label: 'Логика',
    color: '#7b2ff7',
    iconSrc: splitIcon,
  },
  data: {
    key:   'data',
    label: 'Данные',
    color: '#2ecc71',
    iconSrc: databaseIcon,
  },
  user: {
    key:   'user',
    label: 'Пользователь',
    color: '#e91e63',
    iconSrc: userIcon,
  },
  integrations: {
    key:   'integrations',
    label: 'Интеграции',
    color: '#e67e22',
    iconSrc: globeIcon,
  },
  vk: {
    key:   'vk',
    label: 'VK',
    color: '#4680c2',
    iconSrc: vkIcon,
  },
}

// ── Узлы ─────────────────────────────────────────────────────────────────────

export interface NodeMeta {
  /** data.type — стабильный идентификатор */
  type: string
  /** Человекочитаемая метка */
  label: string
  /** Категория для группировки в sidebar */
  category: CategoryKey
}

/**
 * Порядок здесь = порядок в sidebar внутри категории.
 */
export const NODE_META: Readonly<Record<string, NodeMeta>> = {
  // ── Отправка ───────────────────────────────────────────────────────────────
  send_message:     { type: 'send_message',     label: 'Отправить сообщение',   category: 'messageSending' },
  send_image:       { type: 'send_image',       label: 'Отправить изображение', category: 'messageSending' },
  typing_indicator: { type: 'typing_indicator', label: 'Индикатор набора',      category: 'messageSending' },

  // ── Ожидание ответа ────────────────────────────────────────────────────────
  receive_message:      { type: 'receive_message',      label: 'Получить сообщение',   category: 'messageReceiving' },
  receive_image:        { type: 'receive_image',        label: 'Получить изображение', category: 'messageReceiving' },

  // ── Управление сообщением ──────────────────────────────────────────────────
  edit_message:   { type: 'edit_message',   label: 'Редактировать сообщение', category: 'messageManagement' },
  delete_message: { type: 'delete_message', label: 'Удалить сообщение',       category: 'messageManagement' },

  // ── Логика ─────────────────────────────────────────────────────────────────
  condition:    { type: 'condition',    label: 'Условие',              category: 'logic' },
  switch:       { type: 'switch',       label: 'Переключатель',        category: 'logic' },
  goto_node:    { type: 'goto_node',    label: 'Переход',              category: 'logic' },
  group_node:   { type: 'group_node',   label: 'Группа',               category: 'logic' },
  group_entry_marker: { type: 'group_entry_marker', label: 'Вход',     category: 'logic' },
  group_exit_marker:  { type: 'group_exit_marker',  label: 'Выход',    category: 'logic' },
  set_variable: { type: 'set_variable', label: 'Установить переменную', category: 'logic' },
  delay:        { type: 'delay',        label: 'Задержка',             category: 'logic' },

  // ── Данные ─────────────────────────────────────────────────────────────────
  data_record:   { type: 'data_record',   label: 'Данные',          category: 'data' },
  create_record: { type: 'create_record', label: 'Создать запись',  category: 'data' },
  get_record:    { type: 'get_record',    label: 'Получить запись', category: 'data' },
  query_records: { type: 'query_records', label: 'Запросить записи', category: 'data' },
  update_record: { type: 'update_record', label: 'Обновить запись', category: 'data' },
  delete_record: { type: 'delete_record', label: 'Удалить запись',  category: 'data' },

  // ── Пользователь ───────────────────────────────────────────────────────────
  get_user_info: { type: 'get_user_info', label: 'Инфо о пользователе', category: 'user' },

  // ── Интеграции ─────────────────────────────────────────────────────────────
  http_request: { type: 'http_request', label: 'HTTP запрос', category: 'integrations' },

  // ── VK ─────────────────────────────────────────────────────────────────────
  vk_send_keyboard:    { type: 'vk_send_keyboard',    label: 'VK: Клавиатура',       category: 'vk' },
  vk_send_carousel:    { type: 'vk_send_carousel',    label: 'VK: Карусель',         category: 'vk' },
  vk_remove_keyboard:  { type: 'vk_remove_keyboard',  label: 'VK: Убрать клавиатуру', category: 'vk' },
}

/**
 * Возвращает label узла. Если тип неизвестен — возвращает сам type.
 */
export function getNodeLabel(type: string): string {
  return NODE_META[type]?.label ?? type
}

export function getNodeAccentColor(type: string): string {
  const category = NODE_META[type]?.category
  return category ? CATEGORIES[category].color : 'var(--color-primary)'
}
