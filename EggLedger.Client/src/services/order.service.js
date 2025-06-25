import apiClient from './api';

// Maps to your /api/Order controller
export const orderService = {
  // GET /api/Order/container/{containerId}
  getOrdersForContainer(containerId) {
    return apiClient.get(`/api/Order/container/${containerId}`);
  },
  // POST /api/Order/consume (Body is ConsumeOrderDto)
  consumeOrder(orderData) {
    return apiClient.post('/api/Order/consume', orderData);
  },
  // POST /api/Order/stock (Body is StockOrderDto)
  stockOrder(orderData) {
    return apiClient.post('/api/Order/stock', orderData);
  }
  // We will add the /settle logic later when the endpoint exists.
};