import apiClient from './api'

// Maps to your /egg-ledger-api/user controller
export const userService = {
  // GET /egg-ledger-api/user/profile
  getProfile() {
    return apiClient.get('/egg-ledger-api/user/profile')
  },

  // GET /egg-ledger-api/user/{id}
  getUserById(userId) {
    return apiClient.get(`/egg-ledger-api/user/${userId}`)
  },

  // PUT /egg-ledger-api/user/{id}
  updateUser(userId, userData) {
    return apiClient.put(`/egg-ledger-api/user/${userId}`, userData)
  },

  // DELETE /egg-ledger-api/user/{id}
  deleteUser(userId) {
    return apiClient.delete(`/egg-ledger-api/user/${userId}`)
  },

  // GET /egg-ledger-api/user/all
  getAllUsers() {
    return apiClient.get('/egg-ledger-api/user/all')
  },
}

export default userService
