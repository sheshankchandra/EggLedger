import apiClient from './api';

// Maps to your /egg-ledger-api/auth controller
export const authService = {
  // POST /egg-ledger-api/auth/login
  login(credentials) {
    return apiClient.post('/egg-ledger-api/auth/login', credentials);
  },

  // POST /egg-ledger-api/auth/register
  register(userData) {
    return apiClient.post('/egg-ledger-api/auth/register', userData);
  },

  // POST /egg-ledger-api/auth/refresh
  refreshToken(refreshToken) {
    return apiClient.post('/egg-ledger-api/auth/refresh', { refreshToken });
  },

  // POST /egg-ledger-api/auth/logout
  logout() {
    return apiClient.post('/egg-ledger-api/auth/logout');
  },

  // GET /egg-ledger-api/auth/google-login
  googleLogin() {
    const baseURL = import.meta.env.VITE_API_BASE_URL || window.location.origin;
    window.location.href = `${baseURL}/egg-ledger-api/auth/google-login`;
  },

  // Helper method to store token
  setAuthToken(token) {
    localStorage.setItem('authToken', token);
    apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  },

  // Helper method to remove token
  removeAuthToken() {
    localStorage.removeItem('authToken');
    delete apiClient.defaults.headers.common['Authorization'];
  },

  // Helper method to get stored token
  getAuthToken() {
    return localStorage.getItem('authToken');
  }
};

export default authService;
