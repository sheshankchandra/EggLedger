import apiClient from './api'

// Maps to your /egg-ledger-api/auth controller
export const authService = {
  // POST /egg-ledger-api/auth/login
  login(credentials) {
    return apiClient.post('/egg-ledger-api/auth/login', credentials)
  },

  // POST /egg-ledger-api/auth/register
  register(userData) {
    return apiClient.post('/egg-ledger-api/auth/register', userData)
  },

  // POST /egg-ledger-api/auth/refresh
  // The refresh token travels in an HttpOnly cookie, so there is no body to send.
  refresh() {
    return apiClient.post('/egg-ledger-api/auth/refresh')
  },

  // POST /egg-ledger-api/auth/logout
  // Server revokes the refresh token (read from the cookie) and clears the cookie.
  logout() {
    return apiClient.post('/egg-ledger-api/auth/logout')
  },

  // GET /egg-ledger-api/auth/google-login
  googleLogin() {
    const baseURL = import.meta.env.VITE_API_BASE_URL || window.location.origin
    window.location.href = `${baseURL}/egg-ledger-api/auth/google-login`
  },
}

export default authService
