import apiClient from './api';

// Maps to your /egg-ledger-api/{roomCode}/orders controller
export const orderService = {
  // POST /egg-ledger-api/{roomCode}/orders/stock
  stockOrder(roomCode, orderData) {
    return apiClient.post(`/egg-ledger-api/${roomCode}/orders/stock`, orderData);
  },

  // POST /egg-ledger-api/{roomCode}/orders/consume
  consumeOrder(roomCode, orderData) {
    return apiClient.post(`/egg-ledger-api/${roomCode}/orders/consume`, orderData);
  },

  // GET /egg-ledger-api/{roomCode}/orders/{orderId}
  getOrderById(roomCode, orderId) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/${orderId}`);
  },

  // GET /egg-ledger-api/{roomCode}/orders/user/{userId}
  getOrdersByUser(roomCode, userId) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/user/${userId}`);
  },

  // GET /egg-ledger-api/{roomCode}/orders/container/{containerId}
  getOrdersByContainer(roomCode, containerId) {
    return apiClient.get(`/egg-ledger-api/${roomCode}/orders/container/${containerId}`);
  }
};

export default orderService;
