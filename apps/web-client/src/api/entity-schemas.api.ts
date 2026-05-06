import apiClient from './client'
import type {
  CreateEntitySchemaRequest,
  CreateEntitySchemaResponse,
  GetEntitySchemaListResponse,
  GetEntitySchemaResponse,
  UpdateEntitySchemaRequest,
} from '@/types/api'

export const entitySchemasApi = {
  list(projectId: string): Promise<GetEntitySchemaListResponse> {
    return apiClient.get(`/api/v1/projects/${projectId}/schemas`).then((r) => r.data)
  },

  create(projectId: string, data: CreateEntitySchemaRequest): Promise<CreateEntitySchemaResponse> {
    return apiClient.post(`/api/v1/projects/${projectId}/schemas`, data).then((r) => r.data)
  },

  get(schemaId: string): Promise<GetEntitySchemaResponse> {
    return apiClient.get(`/api/v1/schemas/${schemaId}`).then((r) => r.data)
  },

  addField(schemaId: string, data: UpdateEntitySchemaRequest): Promise<void> {
    return apiClient.patch(`/api/v1/schemas/${schemaId}`, data).then((r) => r.data)
  },

  delete(schemaId: string): Promise<void> {
    return apiClient.delete(`/api/v1/schemas/${schemaId}`).then((r) => r.data)
  },
}
