import axios from 'axios'
import { useAuthStore } from '@/stores/auth.store'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  // Send the HttpOnly refresh-token cookie on cross-site auth calls.
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: attach the in-memory access token as a Bearer header.
apiClient.interceptors.request.use(
  (config) => {
    const authStore = useAuthStore()
    const token = authStore.token
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error),
)

// De-duplicate concurrent refreshes: many 401s share one refresh round-trip.
let refreshPromise = null

// Response interceptor: on a 401, silently refresh once and replay the request.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const status = error.response?.status
    const url = original?.url || ''
    const isAuthCall = url.includes('/auth/refresh') || url.includes('/auth/login')

    if (status === 401 && original && !original._retry && !isAuthCall) {
      original._retry = true
      const authStore = useAuthStore()
      try {
        refreshPromise = refreshPromise || authStore.refreshSession()
        const newToken = await refreshPromise
        refreshPromise = null
        if (newToken) {
          original.headers.Authorization = `Bearer ${newToken}`
          return apiClient(original)
        }
      } catch {
        refreshPromise = null
      }
      // Refresh failed -> session is over.
      await authStore.logout()
    }

    return Promise.reject(error)
  },
)

export default apiClient
