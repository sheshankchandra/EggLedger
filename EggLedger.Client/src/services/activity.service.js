import apiClient from './api'

export const activityService = {
  async getRoomActivity(roomCode, { page = 1, pageSize = 30 } = {}, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/activity`, {
      params: { page, pageSize },
      signal,
    })
    return response.data
  },
}

export default activityService
