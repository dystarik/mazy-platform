import { defineStore } from 'pinia'
import { ref } from 'vue'
import { sessionsApi } from '@/api'
import type { SessionInfo } from '@/types/api'

export const useSessionsStore = defineStore('sessions', () => {
  const sessions = ref<SessionInfo[]>([])
  const isLoading = ref(false)

  async function loadSessions(): Promise<void> {
    isLoading.value = true
    try {
      const response = await sessionsApi.getSessions()
      sessions.value = response.sessions ?? []
    } finally {
      isLoading.value = false
    }
  }

  async function logout(refreshTokenId: string): Promise<void> {
    await sessionsApi.logout({ refreshTokenId })
    sessions.value = sessions.value.filter(
      (s) => s.refreshTokenId !== refreshTokenId
    )
  }

  async function logoutAll(excludeCurrent: boolean): Promise<void> {
    await sessionsApi.logoutAll({ excludeCurrentSession: excludeCurrent })
    if (excludeCurrent) {
      sessions.value = sessions.value.filter((s) => s.isCurrent)
    } else {
      sessions.value = []
    }
  }

  return {
    sessions,
    isLoading,
    loadSessions,
    logout,
    logoutAll,
  }
})
