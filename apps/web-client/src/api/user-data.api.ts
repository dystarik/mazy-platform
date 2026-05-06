import apiClient from './client'
import type { GetUserDataRecordsParams, GetUserDataRecordsResponse } from '@/types/api'

export const userDataApi = {
  list(projectId: string, params: GetUserDataRecordsParams = {}): Promise<GetUserDataRecordsResponse> {
    return apiClient
      .get(`/api/v1/projects/${projectId}/user-data/records`, { params })
      .then(r => r.data)
  },
}
