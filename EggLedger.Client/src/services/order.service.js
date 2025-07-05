import apiClient from './api'

// Maps to your /egg-ledger-api/{roomCode}/orders controller
export const orderService = {
  // POST /egg-ledger-api/{roomCode}/orders/stock
  stockOrder(roomCode, orderData, signal) {
    return apiClient.post(`/egg-ledger-api/${roomCode}/orders/stock`, orderData, { signal })
  },

  // POST /egg-ledger-api/{roomCode}/orders/consume
  consumeOrder(roomCode, orderData, signal) {
    return apiClient.post(`/egg-ledger-api/${roomCode}/orders/consume`, orderData, { signal })
  },

  // GET /egg-ledger-api/{roomCode}/orders/{orderId}
  getOrderById(roomCode, orderId, signal) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/${orderId}`, { signal })
  },

  // GET /egg-ledger-api/{roomCode}/orders/user/{userId}
  getOrdersByUser(roomCode, userId, signal) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/user/${userId}`, { signal })
  },

  // GET /egg-ledger-api/{roomCode}/orders/container/{containerId}
  getOrdersByContainer(roomCode, containerId, signal) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/container/${containerId}`, { signal })
  },

  getOrdersForContainer(containerId, signal, roomCode) {
    // If roomCode is not provided, you might need to get it from a store or context
    if (!roomCode) {
      // You might need to get roomCode from your auth store or current route
      console.warn('getOrdersForContainer: roomCode not provided, this might cause issues')
    }
    return this.getOrdersByContainer(roomCode || '', containerId, signal)
  },
}

export default orderService
