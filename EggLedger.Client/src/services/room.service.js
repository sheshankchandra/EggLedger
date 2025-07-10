import apiClient from './api'

export const roomService = {
  // Get all rooms the user is a member of
  async getUserRooms(signal) {
    const response = await apiClient.get('/egg-ledger-api/room/user/all', { signal })
    return response.data
  },

  // Get room details by code
  async getRoomByCode(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}`, { signal })
    return response.data
  },

  // Create a new room
  async createRoom(roomData, signal) {
    const response = await apiClient.post('/egg-ledger-api/room/create/', roomData, { signal })
    return response.data
  },

  // Join a room by code
  async joinRoom(roomCode, signal) {
    const response = await apiClient.post(`/egg-ledger-api/room/join/${roomCode}`, {}, { signal })
    return response.data
  },

  // Get all users in a room
  async getAllRoomUsers(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/all`, { signal })
    return response.data
  },

  // Update room public status (admin only)
  async updateRoomPublicStatus(updateData, signal) {
    const response = await apiClient.post('/egg-ledger-api/room/update/IsPublic', updateData, { signal })
    return response.data
  },

  // Delete a room (admin only)
  async deleteRoom(roomCode, signal) {
    const response = await apiClient.post(`/egg-ledger-api/room/delete/${roomCode}`, {}, { signal })
    return response.data
  },
}

export default roomService
