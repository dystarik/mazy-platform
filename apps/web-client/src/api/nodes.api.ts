import apiClient from './client'
import type { GetNodeCatalogResponse, PlatformType } from '@/types/api'

export const nodesApi = {
  getCatalog(platformType: PlatformType = 'PLATFORM_TYPE_VK'): Promise<GetNodeCatalogResponse> {
    return apiClient.get('/api/v1/nodes/catalog', { params: { platformType } }).then((r) => r.data)
  },
}
