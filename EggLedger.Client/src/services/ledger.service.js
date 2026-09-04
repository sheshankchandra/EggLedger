import apiClient from './api'

export const ledgerService = {
  // Get the full "who owes whom" picture for a room: balances, pairwise debts, suggested settlements
  async getLedger(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/ledger`, { signal })
    return response.data
  },

  // Get settlement history for a room
  async getSettlementHistory(roomCode, signal) {
    const response = await apiClient.get(`/egg-ledger-api/room/${roomCode}/ledger/history`, {
      signal,
    })
    return response.data
  },

  // Record that the caller received `amount` from `payerId` (caller is always the receiver)
  async recordSettlement(roomCode, { payerId, amount, note }, signal) {
    const response = await apiClient.post(
      `/egg-ledger-api/room/${roomCode}/ledger/settle`,
      { payerId, amount, note },
      { signal },
    )
    return response.data
  },

  // Delete a settlement (only the receiver who recorded it can remove it)
  async deleteSettlement(roomCode, settlementId, signal) {
    const response = await apiClient.delete(
      `/egg-ledger-api/room/${roomCode}/ledger/settle/${settlementId}`,
      { signal },
    )
    return response.data
  },
}

export default ledgerService
