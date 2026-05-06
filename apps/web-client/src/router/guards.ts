import { useAuthStore } from '@/stores/auth.store'
import { useMfaStore } from '@/stores/mfa.store'

export function authGuard() {
  const authStore = useAuthStore()

  if (!authStore.isAuthenticated) {
    return { name: 'login' }
  }
}

export function guestGuard() {
  const authStore = useAuthStore()

  if (authStore.isAuthenticated) {
    return { name: 'projects' }
  }
}

export function mfaGuard() {
  const mfaStore = useMfaStore()

  if (!mfaStore.mfaSessionId) {
    return { name: 'login' }
  }
}
