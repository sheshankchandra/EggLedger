import apiClient from './api'

export const statsService = {
  // range: 'Week' | 'Month' | 'Year' | 'Max'
  async getUserStats(range, signal) {
    const response = await apiClient.get('/egg-ledger-api/user/stats', {
      params: { range },
      signal,
    })
    return response.data
  },
}

export default statsService
