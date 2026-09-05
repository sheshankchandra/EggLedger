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

  // Get pending join requests for a room (admin only)
  async getPendingMembers(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/pending-members`, {
      signal,
    })
    return response.data
  },

  // Approve a pending join request (admin only)
  async approveMember(roomCode, memberUserId, signal) {
    const response = await apiClient.post(
      `/egg-ledger-api/room/${roomCode}/approve-member/${memberUserId}`,
      {},
      { signal },
    )
    return response.data
  },

  // Reject a pending join request (admin only)
  async rejectMember(roomCode, memberUserId, signal) {
    const response = await apiClient.post(
      `/egg-ledger-api/room/${roomCode}/reject-member/${memberUserId}`,
      {},
      { signal },
    )
    return response.data
  },

  // Get all users in a room
  async getAllRoomUsers(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/all`, { signal })
    return response.data
  },

  // Update room visibility - private (approval required) or open (join instantly) - admin only
  async updateRoomVisibility(roomCode, isOpen, signal) {
    const response = await apiClient.post(
      `/egg-ledger-api/room/${roomCode}/visibility`,
      { isOpen },
      { signal },
    )
    return response.data
  },

  // Delete a room (admin only)
  async deleteRoom(roomCode, signal) {
    const response = await apiClient.post(`/egg-ledger-api/room/delete/${roomCode}`, {}, { signal })
    return response.data
  },
}

export default roomService
