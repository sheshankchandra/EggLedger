import { defineStore } from 'pinia'
import roomService from '@/services/room.service'

const SELECTED_ROOM_KEY = 'selectedRoomCode'

const readSelectedRoomCode = () => {
  const stored = sessionStorage.getItem(SELECTED_ROOM_KEY)
  return stored ? Number(stored) : null
}

/**
 * Owns the user's room list and which room is currently "open". Previously this was split
 * across auth.store.js (the room list) and ad-hoc sessionStorage reads re-implemented in
 * RoomView, RoomIndicator, and ProfileComponent - centralized here so there's one source of
 * truth and one fetch, instead of each screen re-deriving the same thing.
 */
export const useRoomStore = defineStore('room', {
  state: () => ({
    rooms: [],
    selectedRoomCode: readSelectedRoomCode(),
    loading: false,
    _abortController: null,
  }),
  getters: {
    userRooms: (state) => state.rooms,
    hasRooms: (state) => state.rooms.length > 0,
    isLoading: (state) => state.loading,
    selectedRoom: (state) =>
      state.rooms.find((room) => room.roomCode === state.selectedRoomCode) || null,
  },
  actions: {
    async fetchUserRooms() {
      if (this.loading) return
      this.loading = true
      this._abortController?.abort()
      this._abortController = new AbortController()

      try {
        this.rooms = await roomService.getUserRooms(this._abortController.signal)
      } catch (error) {
        if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') return
        console.error('Failed to fetch user rooms:', error)
        this.rooms = []
      } finally {
        this.loading = false
      }
    },

    selectRoom(roomCode) {
      this.selectedRoomCode = Number(roomCode)
      sessionStorage.setItem(SELECTED_ROOM_KEY, String(this.selectedRoomCode))
    },

    clearSelectedRoom() {
      this.selectedRoomCode = null
      sessionStorage.removeItem(SELECTED_ROOM_KEY)
    },

    // Returns the raw FluentResults-shaped payload ({ isSuccess, value, ... }) so callers can
    // surface field-level errors; the room list is refreshed as a side effect either way.
    async createRoom(dto, signal) {
      const result = await roomService.createRoom(dto, signal)
      await this.fetchUserRooms()
      return result
    },

    async joinRoom(roomCode, signal) {
      const result = await roomService.joinRoom(roomCode, signal)
      await this.fetchUserRooms()
      return result
    },

    async fetchPendingMembers(roomCode, signal) {
      return await roomService.getPendingMembers(roomCode, signal)
    },

    async approveMember(roomCode, memberUserId, signal) {
      await roomService.approveMember(roomCode, memberUserId, signal)
    },

    async rejectMember(roomCode, memberUserId, signal) {
      await roomService.rejectMember(roomCode, memberUserId, signal)
    },

    async updateRoomVisibility(roomCode, isOpen, signal) {
      const result = await roomService.updateRoomVisibility(roomCode, isOpen, signal)
      await this.fetchUserRooms()
      return result
    },

    async deleteRoom(roomCode, signal) {
      await roomService.deleteRoom(roomCode, signal)
      this.clearSelectedRoom()
      await this.fetchUserRooms()
    },

    reset() {
      this.rooms = []
      this.clearSelectedRoom()
    },
  },
})
