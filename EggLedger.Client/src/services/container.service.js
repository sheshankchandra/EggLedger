import apiClient from './api'

// Maps to your /egg-ledger-api/room/{roomCode}/container controller
export const containerService = {
  // GET /egg-ledger-api/room/{roomCode}/container/all
  getContainers(roomCode, signal) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/all`, { signal })
  },

  // GET /egg-ledger-api/room/{roomCode}/container/{id}
  getContainerById(roomCode, id, signal) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/${id}`, { signal })
  },

  // POST /egg-ledger-api/room/{roomCode}/container/create
  createContainer(roomCode, containerData, signal) {
    return apiClient.post(`/egg-ledger-api/room/${roomCode}/container/create`, containerData, {
      signal,
    })
  },

  // PUT /egg-ledger-api/room/{roomCode}/container/update/{id}
  updateContainer(roomCode, id, containerData, signal) {
    return apiClient.put(`/egg-ledger-api/room/${roomCode}/container/update/${id}`, containerData, {
      signal,
    })
  },

  // DELETE /egg-ledger-api/room/{roomCode}/container/delete/{id}
  deleteContainer(roomCode, id, signal) {
    return apiClient.delete(`/egg-ledger-api/room/${roomCode}/container/delete/${id}`, { signal })
  },

  // GET /egg-ledger-api/room/{roomCode}/container/user/{name}
  searchContainersByOwner(roomCode, ownerName, signal) {
    return apiClient.get(`/egg-ledger-api/room/${roomCode}/container/user/${ownerName}`, { signal })
  },

  // GET /egg-ledger-api/room/{roomCode}/container/paged?page={page}&pageSize={pageSize}
  getPagedContainers(roomCode, page = 1, pageSize = 20, signal) {
    return apiClient.get(
      `/egg-ledger-api/room/${roomCode}/container/paged?page=${page}&pageSize=${pageSize}`,
      { signal },
    )
  },
}

export default containerService
