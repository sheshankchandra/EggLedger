import { defineStore } from 'pinia'
import authService from '@/services/auth.service'
import roomService from '@/services/room.service'
import userService from '@/services/user.service'
import router from '@/router'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || null,
    user: JSON.parse(localStorage.getItem('user')) || null,
    userRooms: JSON.parse(localStorage.getItem('userRooms')) || [],
    abortController: null,
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    getUser: (state) => state.user,
    getUserRooms: (state) => state.userRooms,
    hasRooms: (state) => state.userRooms && state.userRooms.length > 0,
  },
  actions: {
    createAbortController() {
      if (this.abortController) {
        this.abortController.abort()
      }
      this.abortController = new AbortController()
      return this.abortController.signal
    },

    async login(credentials) {
      try {
        const response = await authService.login(credentials)
        const token = response.data.accessToken
        this.setToken(token)
        await this.fetchProfile()
        await this.fetchUserRooms()
        router.push('/')
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Login failed:', error)
        throw error
      }
    },

    async register(userData) {
      try {
        const response = await authService.register(userData)
        const token = response.data.accessToken
        this.setToken(token)
        await this.fetchProfile()
        await this.fetchUserRooms()
        router.push('/')
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Registration failed:', error)
        throw error
      }
    },

    async fetchProfile() {
      if (!this.token) return

      try {
        const signal = this.createAbortController()
        const response = await userService.getProfile(signal)
        const user = response.data
        this.setUser(user)
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Failed to fetch profile:', error)
        this.logout()
      }
    },

    async fetchUserRooms() {
      if (!this.token) return

      try {
        const signal = this.createAbortController()
        const userRooms = await roomService.getUserRooms(signal)
        this.setUserRooms(userRooms)
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Failed to fetch user rooms:', error)
        this.setUserRooms([])
      }
    },

    handleGoogleLoginCallback(token) {
      this.setToken(token)
      this.fetchProfile().then(async () => {
        await this.fetchUserRooms()
        router.push('/')
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
        if (this.abortController) {
          this.abortController.abort()
        }
        await authService.logout()
      } catch (error) {
        console.error('Logout API call failed:', error)
      }

      this.token = null
      this.user = null
      this.userRooms = []
      this.abortController = null
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      localStorage.removeItem('userRooms')
      authService.removeAuthToken()
      router.push('/login')
    },
  },
})
