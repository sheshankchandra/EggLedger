import apiClient from './api';

// Maps to your /egg-ledger-api/room/{roomCode}/container controller
export const containerService = {
  // GET /egg-ledger-api/room/{roomCode}/container/all
  getContainers(roomCode) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/all`);
  },

  // GET /egg-ledger-api/room/{roomCode}/container/{id}
  getContainerById(roomCode, id) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/${id}`);
  },

  // POST /egg-ledger-api/room/{roomCode}/container/create
  createContainer(roomCode, containerData) {
    return apiClient.post(`/egg-ledger-api/room/${roomCode}/container/create`, containerData);
  },

  // PUT /egg-ledger-api/room/{roomCode}/container/update/{id}
  updateContainer(roomCode, id, containerData) {
    return apiClient.put(`/egg-ledger-api/room/${roomCode}/container/update/${id}`, containerData);
  },

  // DELETE /egg-ledger-api/room/{roomCode}/container/delete/{id}
  deleteContainer(roomCode, id) {
    return apiClient.delete(`/egg-ledger-api/room/${roomCode}/container/delete/${id}`);
  },

  // GET /egg-ledger-api/room/{roomCode}/container/user/{name}
  searchContainersByOwner(roomCode, ownerName) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/user/${ownerName}`);
  },

  // GET /egg-ledger-api/room/{roomCode}/container/paged?page={page}&pageSize={pageSize}
  getPagedContainers(roomCode, page = 1, pageSize = 20) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/paged?page=${page}&pageSize=${pageSize}`);
  }
};

export default containerService;
