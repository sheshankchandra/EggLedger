import apiClient from './api'

export const roomService = {
  // Get all rooms the user is a member of
  async getUserRooms() {
    const response = await apiClient.get('/egg-ledger-api/room/user/all')
    return response.data
  },

  // Get room details by code
  async getRoomByCode(roomCode) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}`)
    return response.data
  },

  // Create a new room
  async createRoom(roomData) {
    const response = await apiClient.post('/egg-ledger-api/room/create/', roomData)
    return response.data
  },

  // Join a room by code
  async joinRoom(joinData) {
    const response = await apiClient.post('/egg-ledger-api/room/join/', joinData)
    return response.data
  },

  // Get all users in a room
  async getAllRoomUsers(roomCode) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/all`)
    return response.data
  },

  // Update room public status (admin only)
  async updateRoomPublicStatus(updateData) {
    const response = await apiClient.post('/egg-ledger-api/room/update/IsPublic', updateData)
    return response.data
  },
}

export default roomService
