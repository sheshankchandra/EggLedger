import { defineStore } from 'pinia';
import apiClient from '@/services/api';
import router from '@/router';

export const useAuthStore = defineStore('auth', {
    state: () => ({
        token: localStorage.getItem('token') || null,
        user: JSON.parse(localStorage.getItem('user')) || null,
    }),
    getters: {
        isAuthenticated: (state) => !!state.token,
        getUser: (state) => state.user,
    },
    actions: {
        async login(credentials) {
            try {
                const response = await apiClient.post('/api/auth/login', credentials);
                const token = response.data.token;
                
                this.setToken(token);
                await this.fetchProfile(); // Fetch profile right after login

                router.push('/profile'); // Redirect to profile page
            } catch (error) {
                console.error('Login failed:', error);
                // You should handle login errors, e.g., show a message to the user
                throw error;
            }
        },

        async fetchProfile() {
            if (!this.token) return;

            try {
                const response = await apiClient.get('/api/user/profile');
                const user = response.data;
                this.setUser(user);
            } catch (error) {
                console.error('Failed to fetch profile:', error);
                // If fetching fails (e.g., token expired), log the user out
                this.logout();
            }
        },
        
        handleGoogleLoginCallback(token) {
            this.setToken(token);
            // We assume the token is valid and proceed to fetch the profile
            this.fetchProfile();
            router.push('/profile');
        },

        setToken(token) {
            this.token = token;
            localStorage.setItem('token', token);
        },

        setUser(user) {
            this.user = user;
            localStorage.setItem('user', JSON.stringify(user));
        },
        
        logout() {
            this.token = null;
            this.user = null;
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            router.push('/login');
        },
    },
});