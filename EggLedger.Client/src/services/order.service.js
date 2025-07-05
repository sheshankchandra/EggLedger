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
}

export default orderService
