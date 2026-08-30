import axios from 'axios'
import { useAuthStore } from '@/stores/auth.store'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  // Send the HttpOnly refresh-token cookie on cross-site auth calls.
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    // Anti-CSRF: a custom header the API requires on cookie-authenticated
    // endpoints. Browsers force a CORS preflight for it, so a cross-site page
    // cannot forge these requests.
    'X-EggLedger-CSRF': '1',
    // Report the frontend build so the API can tag telemetry with it, letting
    // us see which client version made each request.
    'X-Client-Version': import.meta.env.VITE_APP_VERSION || 'dev',
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
        // refreshSession de-dupes concurrent callers, so a burst of 401s shares
        // one refresh round-trip and rotates the single-use token only once.
        const newToken = await authStore.refreshSession()
        if (newToken) {
          original.headers.Authorization = `Bearer ${newToken}`
          return apiClient(original)
        }
      } catch {
        // fall through to logout below
      }
      // Refresh failed -> session is over.
      await authStore.logout()
    }

    return Promise.reject(error)
  },
)

export default apiClient
