import apiClient from './client'
import type {
  GetSessionsResponse,
  LogoutRequest,
  LogoutAllRequest,
} from '@/types/api'

export const sessionsApi = {
  getSessions(): Promise<GetSessionsResponse> {
    return apiClient.get('/api/v1/sessions').then((r) => r.data)
  },

  logout(data: LogoutRequest): Promise<void> {
    return apiClient.post('/api/v1/sessions/logout', data).then((r) => r.data)
  },

  logoutAll(data: LogoutAllRequest): Promise<void> {
    return apiClient.post('/api/v1/sessions/logout-all', data).then((r) => r.data)
  },
}
