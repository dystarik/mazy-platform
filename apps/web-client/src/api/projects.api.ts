import apiClient from './client'
import type {
  CreateProjectRequest,
  CreateProjectResponse,
  GetProjectListResponse,
  GetProjectResponse,
} from '@/types/api'

export const projectsApi = {
  list(): Promise<GetProjectListResponse> {
    return apiClient.get('/api/v1/projects').then((r) => r.data)
  },

  create(data: CreateProjectRequest): Promise<CreateProjectResponse> {
    return apiClient.post('/api/v1/projects', data).then((r) => r.data)
  },

  get(id: string): Promise<GetProjectResponse> {
    return apiClient.get(`/api/v1/projects/${id}`).then((r) => r.data)
  },

  delete(id: string): Promise<void> {
    return apiClient.delete(`/api/v1/projects/${id}`).then((r) => r.data)
  },
}
