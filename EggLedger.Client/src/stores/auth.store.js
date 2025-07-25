import { defineStore } from 'pinia'
import authService from '@/services/auth.service'
import roomService from '@/services/room.service'
import userService from '@/services/user.service'
import router from '@/router'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || null,
    refreshToken: localStorage.getItem('refreshToken') || null,
    user: JSON.parse(localStorage.getItem('user')) || null,
    userRooms: JSON.parse(localStorage.getItem('userRooms')) || [],
    isNewUser: false,
    abortControllers: {
      profile: null,
      rooms: null,
      auth: null,
    },
    loading: {
      profile: false,
      rooms: false,
    },
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    getUser: (state) => state.user,
    getUserRooms: (state) => state.userRooms,
    hasRooms: (state) => state.userRooms && state.userRooms.length > 0,
    isLoadingRooms: (state) => state.loading.rooms,
    isLoadingProfile: (state) => state.loading.profile,
    getIsNewUser: (state) => state.isNewUser,
  },
  actions: {
    createAbortController(type = 'general') {
      if (this.abortControllers[type]) {
        this.abortControllers[type].abort()
      }
      this.abortControllers[type] = new AbortController()
      return this.abortControllers[type].signal
    },

    async login(credentials) {
      try {
        const response = await authService.login(credentials)
        const token = response.data.accessToken
        this.setToken(token)
        await this.fetchProfile()
        router.push('/dashboard')
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
        this.isNewUser = true
        this.setToken(token)
        await this.fetchProfile()
        router.push('/dashboard')
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Registration failed:', error)
        throw error
      }
    },

    async fetchProfile() {
      if (!this.token || this.loading.profile) return

      try {
        this.loading.profile = true
        const signal = this.createAbortController('profile')
        const response = await userService.getProfile(signal)
        const user = response.data
        this.setUser(user)
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Failed to fetch profile:', error)
        this.logout()
      } finally {
        this.loading.profile = false
      }
    },

    async fetchUserRooms() {
      if (!this.token || this.loading.rooms) return

      try {
        this.loading.rooms = true
        const signal = this.createAbortController('rooms')
        const userRooms = await roomService.getUserRooms(signal)
        this.setUserRooms(userRooms)
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Failed to fetch user rooms:', error)
        this.setUserRooms([])
      } finally {
        this.loading.rooms = false
      }
    },

    handleGoogleLoginCallback(token, refreshToken, isNewRegistration = false) {
      this.setToken(token)
      this.setRefreshToken(refreshToken)
      this.isNewUser = isNewRegistration
      this.fetchProfile().then(async () => {
        router.push('/dashboard')
      })
    },

    setToken(token) {
      this.token = token
      localStorage.setItem('token', token)
    },

    setRefreshToken(refreshToken) {
      this.refreshToken = refreshToken
      localStorage.setItem('refreshToken', refreshToken)
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
        // Abort all ongoing requests
        Object.values(this.abortControllers).forEach((controller) => {
          if (controller) {
            controller.abort()
          }
        })
        await authService.logout(this.refreshToken)
      } catch (error) {
        console.error('Logout API call failed:', error)
      }

      this.token = null
      this.refreshToken = null
      this.user = null
      this.userRooms = []
      this.abortControllers = {
        profile: null,
        rooms: null,
        auth: null,
      }
      this.loading = {
        profile: false,
        rooms: false,
      }
      localStorage.removeItem('token')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')
      localStorage.removeItem('userRooms')
      authService.removeAuthToken()
      router.push('/')
    },
  },
})
