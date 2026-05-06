import apiClient from './client'
import type {
  BindBotToProjectRequest,
  CreateBotWithoutProjectRequest,
  CreateBotRequest,
  CreateBotResponse,
  GetBotResponse,
  GetBotsByProjectResponse,
  GetBotsByUserIdResponse,
  UpdateBotTokenRequest,
  ChangeBotScenarioVersionRequest,
} from '@/types/api'

/**
 * service-bot-manager — управление бот-инстансами проекта.
 *
 * Эндпоинты:
 *   POST   /api/v1/bots                                 — создать бот-инстанс
 *   GET    /api/v1/bots                                 — список всех ботов пользователя
 *   POST   /api/v1/bots/standalone                      — создать standalone-бота без проекта
 *   GET    /api/v1/bots/{id}                            — получить бот-инстанс
 *   DELETE /api/v1/bots/{id}                            — удалить бот-инстанс
 *   POST   /api/v1/bots/{id}/bind                       — привязать бота к проекту и версии
 *   POST   /api/v1/bots/{id}/unbind                     — отвязать бота от проекта
 *   GET    /api/v1/projects/{projectId}/bots            — список ботов проекта
 *   POST   /api/v1/bots/{id}/activate                   — активировать
 *   POST   /api/v1/bots/{id}/deactivate                 — деактивировать
 *   PUT    /api/v1/bots/{id}/token                      — обновить токен/community
 *   PUT    /api/v1/bots/{id}/scenario-version           — переключить версию сценария
 */
export const botsApi = {
  list(): Promise<GetBotsByUserIdResponse> {
    return apiClient.get('/api/v1/bots').then(r => r.data)
  },

  create(data: CreateBotRequest): Promise<CreateBotResponse> {
    return apiClient.post('/api/v1/bots', data).then(r => r.data)
  },

  createStandalone(data: CreateBotWithoutProjectRequest): Promise<CreateBotResponse> {
    return apiClient.post('/api/v1/bots/standalone', data).then(r => r.data)
  },

  get(botInstanceId: string): Promise<GetBotResponse> {
    return apiClient.get(`/api/v1/bots/${botInstanceId}`).then(r => r.data)
  },

  delete(botInstanceId: string): Promise<void> {
    return apiClient.delete(`/api/v1/bots/${botInstanceId}`).then(r => r.data)
  },

  listByProject(projectId: string): Promise<GetBotsByProjectResponse> {
    return apiClient.get(`/api/v1/projects/${projectId}/bots`).then(r => r.data)
  },

  bind(botInstanceId: string, data: BindBotToProjectRequest): Promise<void> {
    return apiClient.post(`/api/v1/bots/${botInstanceId}/bind`, data).then(r => r.data)
  },

  unbind(botInstanceId: string): Promise<void> {
    return apiClient.post(`/api/v1/bots/${botInstanceId}/unbind`, { botInstanceId }).then(r => r.data)
  },

  activate(botInstanceId: string): Promise<void> {
    return apiClient.post(`/api/v1/bots/${botInstanceId}/activate`, {}).then(r => r.data)
  },

  deactivate(botInstanceId: string): Promise<void> {
    return apiClient.post(`/api/v1/bots/${botInstanceId}/deactivate`, {}).then(r => r.data)
  },

  updateToken(botInstanceId: string, data: UpdateBotTokenRequest): Promise<void> {
    return apiClient.put(`/api/v1/bots/${botInstanceId}/token`, data).then(r => r.data)
  },

  changeScenarioVersion(
    botInstanceId: string,
    data: ChangeBotScenarioVersionRequest,
  ): Promise<void> {
    return apiClient
      .put(`/api/v1/bots/${botInstanceId}/scenario-version`, data)
      .then(r => r.data)
  },
}
