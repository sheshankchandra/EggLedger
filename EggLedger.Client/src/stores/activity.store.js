import { defineStore } from 'pinia'
import activityService from '@/services/activity.service'

const PAGE_SIZE = 30

/**
 * Owns the recent activity feed (stock/consume/settlement/join events) for whichever room is
 * currently open. Supports incremental "load more" pagination on top of the initial fetch.
 */
export const useActivityStore = defineStore('activity', {
  state: () => ({
    roomCode: null,
    events: [],
    page: 1,
    hasMore: true,
    loading: false,
    loadingMore: false,
    error: null,
    _abortController: null,
  }),
  actions: {
    async fetchActivity(roomCode) {
      this.roomCode = roomCode
      this.page = 1
      this.hasMore = true
      this.loading = true
      this.error = null
      this._abortController?.abort()
      this._abortController = new AbortController()

      try {
        this.events = await activityService.getRoomActivity(
          roomCode,
          { page: 1, pageSize: PAGE_SIZE },
          this._abortController.signal,
        )
        this.hasMore = this.events.length === PAGE_SIZE
      } catch (err) {
        if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
        console.error('Failed to fetch room activity:', err)
        this.error = 'Could not load room activity.'
      } finally {
        this.loading = false
      }
    },

    async loadMore() {
      if (!this.hasMore || this.loadingMore) return
      this.loadingMore = true
      try {
        const nextPage = this.page + 1
        const more = await activityService.getRoomActivity(this.roomCode, {
          page: nextPage,
          pageSize: PAGE_SIZE,
        })
        this.events.push(...more)
        this.page = nextPage
        this.hasMore = more.length === PAGE_SIZE
      } catch (err) {
        console.error('Failed to load more room activity:', err)
      } finally {
        this.loadingMore = false
      }
    },

    reset() {
      this.roomCode = null
      this.events = []
      this.page = 1
      this.hasMore = true
      this.error = null
    },
  },
})
