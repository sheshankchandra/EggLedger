import apiClient from './api';

// Maps to your /api/User controller
export const userService = {
    // GET /api/User/profile
    getProfile() {
        return apiClient.get('/api/User/profile');
    },
    // POST /api/User (for registration, body is UserCreateDto)
    register(userData) {
        return apiClient.post('/api/User', userData);
    }
}