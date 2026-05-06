export interface NodePreview {
  main: string
  detail?: string
  chips: string[]
}

type NodeParams = Record<string, unknown>

const EMPTY = 'Не заполнено'
const BUTTON_BRANCHING_NODE_TYPE = 'button_branching'
const DATA_NODE_TYPE = 'data_record'
const GROUP_NODE_TYPE = 'group_node'

export function getNodePreview(type: string, params: NodeParams): NodePreview {
  switch (type) {
    case BUTTON_BRANCHING_NODE_TYPE: {
      const buttons = objectList(params.buttons)
      return {
        main: text(params.text, 'Сообщение с кнопками'),
        detail: countLabel(buttons.length, 'ветка', 'ветки', 'веток'),
        chips: buttons.map((button) => text(button.label, text(button.payload, 'Кнопка'))),
      }
    }

    case 'send_message':
      if (objectList(params.buttons).length > 0) {
        return messageWithButtonsPreview(params.text, params.buttons, 'ветка', 'ветки', 'веток')
      }

      return {
        main: text(params.text, 'Текст сообщения'),
        detail: undefined,
        chips: [],
      }

    case 'send_buttons': {
      const buttons = objectList(params.buttons)
      return {
        main: text(params.text, 'Сообщение с кнопками'),
        detail: countLabel(buttons.length, 'кнопка', 'кнопки', 'кнопок'),
        chips: buttons.map((button) => text(button.label, 'Кнопка')),
      }
    }

    case 'edit_message': {
      const buttons = objectList(params.buttons)
      return {
        main: text(params.newText, 'Новый текст сообщения'),
        detail: buttons.length ? countLabel(buttons.length, 'ветка', 'ветки', 'веток') : undefined,
        chips: buttons.map((button) => text(button.label, 'Кнопка')),
      }
    }

    case 'delete_message':
      return {
        main: 'Удалить сообщение',
        detail: undefined,
        chips: [],
      }

    case 'send_image':
      return {
        main: text(params.caption, 'Отправить изображение'),
        detail: text(params.imageUrl, 'URL изображения'),
        chips: [],
      }

    case 'receive_message':
      return {
        main: params.validatorType ? `Ждет сообщение: ${text(params.validatorType)}` : 'Ждет сообщение',
        detail: variableDetail('Сохранить текст', params.messageTextVariable),
        chips: params.errorMessage ? [text(params.errorMessage)] : [],
      }

    case 'receive_button_press': {
      const payloads = stringList(params.expectedPayloads)
      return {
        main: 'Ждет нажатие кнопки',
        detail: payloads.length ? `Кнопки: ${payloads.join(', ')}` : 'Любая кнопка',
        chips: payloads,
      }
    }

    case 'receive_image':
      return {
        main: 'Ждет изображение',
        detail: variableDetail('Сохранить URL', params.imageUrlVariable),
        chips: params.captionVariable ? [`caption -> ${text(params.captionVariable)}`] : [],
      }

    case 'typing_indicator':
      return {
        main: 'Показать набор текста',
        chips: [],
      }

    case 'condition':
      return {
        main: `${text(params.leftOperand, 'left')} ${text(params.operator, '==')} ${text(params.rightOperand, 'right')}`,
        detail: 'Выходы: true / false',
        chips: [],
      }

    case 'switch': {
      const cases = objectList(params.cases)
      return {
        main: `Переключатель: ${text(params.variable, 'variable')}`,
        detail: countLabel(cases.length, 'ветка', 'ветки', 'веток'),
        chips: cases.map((item) => text(item.value, text(item.branchKey, 'case'))),
      }
    }

    case 'set_variable':
      return {
        main: `${text(params.variable, 'variable')} = ${text(params.value, 'value')}`,
        chips: [],
      }

    case 'delay':
      return {
        main: `${text(params.seconds, '0')} сек.`,
        detail: 'Задержка выполнения',
        chips: [],
      }

    case 'http_request':
      return {
        main: `${text(params.method, 'GET')} ${text(params.url, 'URL')}`,
        detail: variableDetail('Ответ', params.responseBodyVariable),
        chips: params.bodyType ? [text(params.bodyType)] : [],
      }

    case 'create_record':
      return dataPreview('Создать запись', params.entityName, params.fields, params.recordIdVariable)

    case 'update_record':
      return dataPreview('Обновить запись', undefined, params.fields, params.recordIdVariable)

    case 'query_records':
      return dataPreview('Найти записи', params.entityName, params.filter, params.recordsVariable)

    case 'get_record':
      return {
        main: 'Получить запись',
        detail: variableDetail('ID записи', params.recordIdVariable),
        chips: params.recordVariable ? [`save -> ${text(params.recordVariable)}`] : [],
      }

    case 'delete_record':
      return {
        main: 'Удалить запись',
        detail: variableDetail('ID записи', params.recordIdVariable),
        chips: [],
      }

    case DATA_NODE_TYPE:
      return smartDataPreview(params)

    case GROUP_NODE_TYPE:
      return {
        main: cleanGroupTitle(text(params.title, 'Группа узлов')),
        detail: countLabel(numberValue(params.nodeCount), 'узел', 'узла', 'узлов'),
        chips: [],
      }

    case 'group_entry_marker':
      return {
        main: 'Начало группы',
        detail: 'Сюда приходят внешние связи',
        chips: [],
      }

    case 'group_exit_marker':
      return {
        main: 'Конец группы',
        detail: 'Отсюда сценарий выходит наружу',
        chips: [],
      }

    case 'get_user_info':
      return {
        main: 'Данные пользователя',
        detail: `Префикс: ${text(params.prefix, 'user')}`,
        chips: [],
      }

    case 'vk_send_keyboard': {
      const rows = matrix(params.buttons)
      const buttons = rows.flat()
      return {
        main: text(params.text, 'VK клавиатура'),
        detail: `${countLabel(rows.length, 'ряд', 'ряда', 'рядов')}, ${countLabel(buttons.length, 'кнопка', 'кнопки', 'кнопок')}`,
        chips: buttons.map((button) => text(button.label, 'Кнопка')),
      }
    }

    case 'vk_send_carousel': {
      const cards = objectList(params.cards)
      return {
        main: text(params.text, 'VK карусель'),
        detail: countLabel(cards.length, 'карточка', 'карточки', 'карточек'),
        chips: cards.map((card) => text(card.title, 'Карточка')),
      }
    }

    case 'vk_remove_keyboard':
      return {
        main: text(params.text, 'Убрать клавиатуру'),
        detail: 'VK',
        chips: [],
      }

    default:
      return {
        main: 'Настройте параметры узла',
        detail: type,
        chips: [],
      }
  }
}

function smartDataPreview(params: NodeParams): NodePreview {
  const action = typeof params.action === 'string' ? params.action : 'create'

  switch (action) {
    case 'get':
      return {
        main: 'Получить запись',
        detail: variableDetail('ID записи', params.recordIdVariable),
        chips: params.recordVariable ? [`save -> ${text(params.recordVariable)}`] : [],
      }
    case 'query':
      return dataPreview('Найти записи', params.entityName, params.filter, params.recordsVariable)
    case 'update':
      return dataPreview('Обновить запись', undefined, params.fields, params.recordIdVariable)
    case 'delete':
      return {
        main: 'Удалить запись',
        detail: variableDetail('ID записи', params.recordIdVariable),
        chips: [],
      }
    case 'create':
    default:
      return dataPreview('Создать запись', params.entityName, params.fields, params.recordIdVariable)
  }
}

function messageWithButtonsPreview(
  textValue: unknown,
  buttonsValue: unknown,
  one: string,
  few: string,
  many: string,
): NodePreview {
  const buttons = objectList(buttonsValue)
  return {
    main: text(textValue, 'Сообщение с кнопками'),
    detail: countLabel(buttons.length, one, few, many),
    chips: buttons.map((button) => text(button.label, text(button.payload, 'Кнопка'))),
  }
}

function dataPreview(
  action: string,
  entity: unknown,
  fields: unknown,
  variable: unknown,
): NodePreview {
  const pairs = dictionaryEntries(fields)
  return {
    main: `${action}: ${text(entity, 'entity')}`,
    detail: pairs.length ? countLabel(pairs.length, 'поле', 'поля', 'полей') : undefined,
    chips: [
      ...pairs.map(([key]) => key),
      ...(variable ? [`save -> ${text(variable)}`] : []),
    ],
  }
}

function variableDetail(label: string, value: unknown): string | undefined {
  return value ? `${label}: ${text(value)}` : undefined
}

function text(value: unknown, fallback = EMPTY): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  if (typeof value !== 'string') return fallback
  const trimmed = value.trim()
  return trimmed ? truncate(trimmed) : fallback
}

function cleanGroupTitle(value: string): string {
  const cleaned = value
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(line => line && !['Вход', 'Выход'].includes(line))
    .join(' ')
    .trim()

  return cleaned || 'Группа узлов'
}

function truncate(value: string, max = 58): string {
  return value.length > max ? `${value.slice(0, max - 1)}...` : value
}

function objectList(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value)
    ? value.filter((item): item is Record<string, unknown> => isRecord(item))
    : []
}

function matrix(value: unknown): Record<string, unknown>[][] {
  if (!Array.isArray(value)) return []

  return value.map((row) =>
    Array.isArray(row)
      ? row.filter((item): item is Record<string, unknown> => isRecord(item))
      : [],
  )
}

function stringList(value: unknown): string[] {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === 'string' && item.trim().length > 0)
    : []
}

function numberValue(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : 0
}

function dictionaryEntries(value: unknown): [string, unknown][] {
  return isRecord(value) ? Object.entries(value).filter(([key]) => key.trim().length > 0) : []
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function countLabel(count: number, one: string, few: string, many: string): string {
  const mod10 = count % 10
  const mod100 = count % 100
  const label = mod10 === 1 && mod100 !== 11
    ? one
    : mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)
      ? few
      : many

  return `${count} ${label}`
}
