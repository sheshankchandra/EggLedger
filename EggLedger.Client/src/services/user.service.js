import apiClient from './api'

// Maps to your /egg-ledger-api/user controller
export const userService = {
  // GET /egg-ledger-api/user/profile
  getProfile(signal) {
    return apiClient.get('/egg-ledger-api/user/profile', { signal })
  },

  // GET /egg-ledger-api/user/{id}
  getUserById(userId, signal) {
    return apiClient.get(`/egg-ledger-api/user/${userId}`, { signal })
  },

  // PUT /egg-ledger-api/user/{id}
  updateUser(userId, userData, signal) {
    return apiClient.put(`/egg-ledger-api/user/${userId}`, userData, { signal })
  },

  // DELETE /egg-ledger-api/user/{id}
  deleteUser(userId, signal) {
    return apiClient.delete(`/egg-ledger-api/user/${userId}`, { signal })
  },

  // GET /egg-ledger-api/user/all
  getAllUsers(signal) {
    return apiClient.get('/egg-ledger-api/user/all', { signal })
  },
}

export default userService
