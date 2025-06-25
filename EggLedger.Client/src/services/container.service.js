import apiClient from './api';

// Maps to your /api/Container controller
export const containerService = {
  // GET /api/Container
  getContainers() {
    return apiClient.get('/api/Container');
  },
  // GET /api/Container/{id}
  getContainerById(id) {
    return apiClient.get(`/api/Container/${id}`);
  },
  // POST /api/Container (Body is ContainerCreateDto)
  createContainer(containerData) {
    return apiClient.post('/api/Container', containerData);
  },
  // DELETE /api/Container/{id}
  deleteContainer(id) {
    return apiClient.delete(`/api/Container/${id}`);
  }
  // We'll ignore the PUT, search, and paged endpoints for now to keep it simple
};