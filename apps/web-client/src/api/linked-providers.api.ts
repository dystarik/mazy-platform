import apiClient from './client'
import type {
  LinkProviderRequest,
  GetLinkedProvidersResponse,
  ExternalProvider,
} from '@/types/api'

export const linkedProvidersApi = {
  // Получить список привязанных провайдеров
  getLinkedProviders(): Promise<GetLinkedProvidersResponse> {
    return apiClient.get('/api/v1/linked-providers').then((r) => r.data)
  },

  // Привязать провайдер
  linkProvider(data: LinkProviderRequest): Promise<void> {
    return apiClient.post('/api/v1/linked-providers', data).then((r) => r.data)
  },

  // Отвязать провайдер
  unlinkProvider(provider: ExternalProvider): Promise<void> {
    return apiClient.delete(`/api/v1/linked-providers/${provider}`).then((r) => r.data)
  },
}
