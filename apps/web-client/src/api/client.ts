import axios from 'axios'
import { decodeJwt } from '@/composables/useJwt'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Утилиты для работы с токенами
export function saveTokens(accessToken: string, refreshToken: string) {
  const { jti } = decodeJwt(accessToken)
  sessionStorage.setItem('accessToken', accessToken)
  localStorage.setItem('refreshToken', refreshToken)
  localStorage.setItem('refreshTokenId', jti)
}

export function clearTokens() {
  sessionStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('refreshTokenId')
}

// Request interceptor
apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Очередь запросов которые ждут пока refresh завершится
let isRefreshing = false
let waitingQueue: Array<(token: string) => void> = []

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error)
    }

    // Не пытаемся рефрешить для auth/mfa эндпоинтов — они сами управляют сессией
    const isExcluded = originalRequest.url?.includes('/auth/') ||
      originalRequest.url?.includes('/sessions/refresh') ||
      originalRequest.url?.includes('/mfa/')

    if (isExcluded) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve) => {
        waitingQueue.push((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          resolve(apiClient(originalRequest))
        })
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const refreshTokenId = localStorage.getItem('refreshTokenId')
      const refreshToken = localStorage.getItem('refreshToken')

      const response = await axios.post(
        `${import.meta.env.VITE_API_URL}/api/v1/sessions/refresh`,
        { refreshTokenId, refreshToken }
      )

      const { accessToken, refreshToken: newRefreshToken } = response.data.tokens
      saveTokens(accessToken, newRefreshToken)

      waitingQueue.forEach((cb) => cb(accessToken))
      waitingQueue = []

      originalRequest.headers.Authorization = `Bearer ${accessToken}`
      return apiClient(originalRequest)
    } catch {
      clearTokens()
      waitingQueue = []
      window.location.href = '/auth/login'
      return Promise.reject(error)
    } finally {
      isRefreshing = false
    }
  }
)

export default apiClient
