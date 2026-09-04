import { defineStore } from 'pinia'
import ledgerService from '@/services/ledger.service'

/**
 * Owns the "who owes whom" ledger for whichever room is currently open: per-user balances,
 * detailed pairwise debts, a simplified settle-up plan, and settlement history.
 */
export const useLedgerStore = defineStore('ledger', {
  state: () => ({
    roomCode: null,
    balances: [],
    pairwiseDebts: [],
    suggestedSettlements: [],
    history: [],
    loading: false,
    historyLoading: false,
    error: null,
    _abortController: null,
  }),
  actions: {
    async fetchLedger(roomCode) {
      this.roomCode = roomCode
      this.loading = true
      this.error = null
      this._abortController?.abort()
      this._abortController = new AbortController()

      try {
        const data = await ledgerService.getLedger(roomCode, this._abortController.signal)
        this.balances = data.balances
        this.pairwiseDebts = data.pairwiseDebts
        this.suggestedSettlements = data.suggestedSettlements
      } catch (err) {
        if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
        console.error('Failed to fetch ledger:', err)
        this.error = 'Could not load the room ledger.'
      } finally {
        this.loading = false
      }
    },

    async fetchHistory(roomCode, signal) {
      this.historyLoading = true
      try {
        this.history = await ledgerService.getSettlementHistory(roomCode, signal)
      } catch (err) {
        if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
        console.error('Failed to fetch settlement history:', err)
      } finally {
        this.historyLoading = false
      }
    },

    async recordSettlement(roomCode, payload, signal) {
      await ledgerService.recordSettlement(roomCode, payload, signal)
      await Promise.all([this.fetchLedger(roomCode), this.fetchHistory(roomCode, signal)])
    },

    async deleteSettlement(roomCode, settlementId, signal) {
      await ledgerService.deleteSettlement(roomCode, settlementId, signal)
      await Promise.all([this.fetchLedger(roomCode), this.fetchHistory(roomCode, signal)])
    },

    reset() {
      this.roomCode = null
      this.balances = []
      this.pairwiseDebts = []
      this.suggestedSettlements = []
      this.history = []
      this.error = null
    },
  },
})
