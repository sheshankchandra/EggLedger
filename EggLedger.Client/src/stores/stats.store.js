import { defineStore } from 'pinia'
import statsService from '@/services/stats.service'

/**
 * Owns the current user's gamified consumption stats (streaks, protein/calories, timeline)
 * for whichever range (Week/Month/Year/Max) is selected.
 */
export const useStatsStore = defineStore('stats', {
  state: () => ({
    range: 'Week',
    totalEggsConsumed: 0,
    totalProteinGrams: 0,
    totalCalories: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    buckets: [],
    loading: false,
    error: null,
    _abortController: null,
  }),
  actions: {
    async fetchStats(range = this.range) {
      this.range = range
      this.loading = true
      this.error = null
      this._abortController?.abort()
      this._abortController = new AbortController()

      try {
        const data = await statsService.getUserStats(range, this._abortController.signal)
        this.totalEggsConsumed = data.totalEggsConsumed
        this.totalProteinGrams = data.totalProteinGrams
        this.totalCalories = data.totalCalories
        this.currentStreakDays = data.currentStreakDays
        this.longestStreakDays = data.longestStreakDays
        this.buckets = data.buckets
      } catch (err) {
        if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
        console.error('Failed to fetch stats:', err)
        this.error = 'Could not load your stats.'
      } finally {
        this.loading = false
      }
    },

    reset() {
      this.range = 'Week'
      this.totalEggsConsumed = 0
      this.totalProteinGrams = 0
      this.totalCalories = 0
      this.currentStreakDays = 0
      this.longestStreakDays = 0
      this.buckets = []
      this.error = null
    },
  },
})
