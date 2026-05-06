import apiClient from './client'
import type {
  GetScenarioDraftResponse,
  GetReleasedScenarioResponse,
  GetScenarioByVersionResponse,
  UpdateScenarioDraftRequest,
  ValidateScenarioDraftResponse,
  GetVersionHistoryResponse,
} from '@/types/api'

/**
 * service-scenario-repository — управление сценариями проекта.
 *
 * Жизненный цикл:
 *   draft → promote → released (новая версия)
 *   released ← rollback ← previous version
 *
 * Эндпоинты:
 *   GET    /api/v1/projects/{id}/scenario/draft                — текущий черновик
 *   PUT    /api/v1/projects/{id}/scenario/draft                — сохранить черновик
 *   GET    /api/v1/projects/{id}/scenario/release              — текущая опубликованная
 *   GET    /api/v1/projects/{id}/scenario/versions             — история версий
 *   GET    /api/v1/projects/{id}/scenario/versions/{version}   — конкретная версия (просмотр архива)
 *   DELETE /api/v1/projects/{id}/scenario/versions/{version}   — удалить версию из истории
 *   POST   /api/v1/projects/{id}/scenario/draft/validate       — проверить черновик
 *   POST   /api/v1/projects/{id}/scenario/promote              — draft → новая release-версия
 *   POST   /api/v1/projects/{id}/scenario/rollback             — откатиться на targetVersion
 */
export const scenarioApi = {
  // ── Draft ───────────────────────────────────────────────────────────────────
  getDraft(projectId: string): Promise<GetScenarioDraftResponse> {
    return apiClient.get(`/api/v1/projects/${projectId}/scenario/draft`).then(r => r.data)
  },

  saveDraft(projectId: string, data: UpdateScenarioDraftRequest): Promise<void> {
    return apiClient.put(`/api/v1/projects/${projectId}/scenario/draft`, data).then(r => r.data)
  },

  validateDraft(projectId: string): Promise<ValidateScenarioDraftResponse> {
    return apiClient
      .post(`/api/v1/projects/${projectId}/scenario/draft/validate`, { projectId })
      .then(r => r.data)
  },

  // ── Release ─────────────────────────────────────────────────────────────────
  getRelease(projectId: string): Promise<GetReleasedScenarioResponse> {
    return apiClient.get(`/api/v1/projects/${projectId}/scenario/release`).then(r => r.data)
  },

  promote(projectId: string): Promise<void> {
    return apiClient
      .post(`/api/v1/projects/${projectId}/scenario/promote`, { projectId })
      .then(r => r.data)
  },

  rollback(projectId: string, targetVersion: number): Promise<void> {
    return apiClient
      .post(`/api/v1/projects/${projectId}/scenario/rollback`, { projectId, targetVersion })
      .then(r => r.data)
  },

  // ── Versions ────────────────────────────────────────────────────────────────
  getVersions(projectId: string): Promise<GetVersionHistoryResponse> {
    return apiClient.get(`/api/v1/projects/${projectId}/scenario/versions`).then(r => r.data)
  },

  getByVersion(projectId: string, version: number): Promise<GetScenarioByVersionResponse> {
    return apiClient
      .get(`/api/v1/projects/${projectId}/scenario/versions/${version}`)
      .then(r => r.data)
  },

  deleteVersion(projectId: string, version: number): Promise<void> {
    return apiClient
      .delete(`/api/v1/projects/${projectId}/scenario/versions/${version}`)
      .then(r => r.data)
  },
}
