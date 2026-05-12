import type { PlatformType } from '@/types/api'

export interface EditorPlatformCapabilities {
  platformType: PlatformType
  platformKey: 'universal' | 'vk' | 'telegram'
  canDeleteIncomingUserMessage: boolean
}

export function getEditorPlatformCapabilities(platformType: PlatformType): EditorPlatformCapabilities {
  if (platformType === 'PLATFORM_TYPE_TELEGRAM') {
    return { platformType, platformKey: 'telegram', canDeleteIncomingUserMessage: true }
  }
  if (platformType === 'PLATFORM_TYPE_VK') {
    return { platformType, platformKey: 'vk', canDeleteIncomingUserMessage: false }
  }
  return { platformType: 'PLATFORM_TYPE_UNIVERSAL', platformKey: 'universal', canDeleteIncomingUserMessage: false }
}
