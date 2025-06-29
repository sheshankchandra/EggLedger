import { defineStore } from 'pinia'
import authService from '@/services/auth.service'
import userService from '@/services/user.service'
import roomService from '@/services/room.service'
import router from '@/router'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || null,
    user: JSON.parse(localStorage.getItem('user')) || null,
    userRooms: JSON.parse(localStorage.getItem('userRooms')) || [],
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    getUser: (state) => state.user,
    getUserRooms: (state) => state.userRooms,
    hasRooms: (state) => state.userRooms && state.userRooms.length > 0,
  },
  actions: {
    async login(credentials) {
      try {
        const response = await authService.login(credentials)
        const token = response.data.accessToken
        this.setToken(token)
        await this.fetchProfile() // Fetch profile right after login
        await this.fetchUserRooms() // Fetch user's rooms

        // Route based on room membership
        if (this.hasRooms) {
          // If user has rooms, let them choose which room to enter
          router.push('/room-selection')
        } else {
          // If user has no rooms, send to lobby to create/join a room
          router.push('/lobby')
        }
      } catch (error) {
        console.error('Login failed:', error)
        throw error
      }
    },

    async register(userData) {
      try {
        const response = await authService.register(userData)
        // Registration successful, now log the user in
        return response.data
      } catch (error) {
        console.error('Registration failed:', error)
        throw error
      }
    },

    async fetchProfile() {
      if (!this.token) return

      try {
        const response = await userService.getProfile()
        const user = response.data
        this.setUser(user)
      } catch (error) {
        console.error('Failed to fetch profile:', error)
        // If fetching fails (e.g., token expired), log the user out
        this.logout()
      }
    },

    async fetchUserRooms() {
      if (!this.token) return

      try {
        const userRooms = await roomService.getUserRooms()
        this.setUserRooms(userRooms)
      } catch (error) {
        console.error('Failed to fetch user rooms:', error)
        // If fetching fails, set empty array
        this.setUserRooms([])
      }
    },

    handleGoogleLoginCallback(token) {
      this.setToken(token)
      this.fetchProfile().then(async () => {
        await this.fetchUserRooms()
        if (this.hasRooms) {
          router.push('/room-selection')
        } else {
          router.push('/lobby')
        }
      })
    },

    setToken(token) {
      this.token = token
      localStorage.setItem('token', token)
    },

    setUser(user) {
      this.user = user
      localStorage.setItem('user', JSON.stringify(user))
    },

    setUserRooms(userRooms) {
      this.userRooms = userRooms
      localStorage.setItem('userRooms', JSON.stringify(userRooms))
    },

    async logout() {
      try {
        // Call the backend logout endpoint
        await authService.logout()
      } catch (error) {
        console.error('Logout API call failed:', error)
        // Continue with local logout even if API call fails
      }

      // Clear local state
      this.token = null
      this.user = null
      this.userRooms = []
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      localStorage.removeItem('userRooms')
      authService.removeAuthToken()
      router.push('/login')
    },
  },
})
