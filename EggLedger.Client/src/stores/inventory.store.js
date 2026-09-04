import { defineStore } from 'pinia'
import containerService from '@/services/container.service'
import orderService from '@/services/order.service'

/**
 * Owns container/order data for whichever room is currently open. Previously this state and
 * its fetch logic lived directly inside RoomComponent.vue; centralizing it here means any
 * screen can read the same inventory without re-fetching or re-implementing loading/error state.
 */
export const useInventoryStore = defineStore('inventory', {
  state: () => ({
    roomCode: null,
    containers: [],
    loading: false,
    error: null,
    _abortController: null,
  }),
  getters: {
    totalRemaining: (state) =>
      state.containers.reduce((sum, container) => sum + container.remainingQuantity, 0),
  },
  actions: {
    async fetchContainers(roomCode) {
      this.roomCode = roomCode
      this.loading = true
      this.error = null
      this._abortController?.abort()
      this._abortController = new AbortController()

      try {
        const response = await containerService.getContainers(
          roomCode,
          this._abortController.signal,
        )
        this.containers = response.data
      } catch (err) {
        if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
        console.error('Failed to fetch containers:', err)
        this.error = 'Could not load containers.'
      } finally {
        this.loading = false
      }
    },

    async stockOrder(roomCode, dto, signal) {
      await orderService.stockOrder(roomCode, dto, signal)
      await this.fetchContainers(roomCode)
    },

    // Returns the consume-order result DTO (it may carry a "not enough stock" message even
    // on a successful HTTP response, so the caller decides how to present it).
    async consumeOrder(roomCode, dto, signal) {
      const response = await orderService.consumeOrder(roomCode, dto, signal)
      await this.fetchContainers(roomCode)
      return response.data
    },

    async deleteContainer(roomCode, containerId, signal) {
      await containerService.deleteContainer(roomCode, containerId, signal)
      await this.fetchContainers(roomCode)
    },

    async searchMyContainers(roomCode, ownerName, signal) {
      const response = await containerService.searchContainersByOwner(roomCode, ownerName, signal)
      return response.data
    },

    reset() {
      this.roomCode = null
      this.containers = []
      this.error = null
    },
  },
})
