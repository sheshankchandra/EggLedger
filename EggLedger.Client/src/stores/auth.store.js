import { defineStore } from 'pinia'
import authService from '@/services/auth.service'
import userService from '@/services/user.service'
import router from '@/router'
import { useRoomStore } from '@/stores/room.store'
import { consumeRedirect } from '@/utils/postLoginRedirect'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    // Access token lives ONLY in memory (never localStorage) to limit XSS blast radius.
    // The refresh token is an HttpOnly cookie the browser manages; JS never sees it.
    token: null,
    user: JSON.parse(localStorage.getItem('user')) || null,
    isNewUser: false,
    // Shared in-flight refresh request; de-dupes concurrent refreshSession() callers.
    _refreshPromise: null,
    abortControllers: {
      profile: null,
      auth: null,
    },
    loading: {
      profile: false,
    },
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    getUser: (state) => state.user,
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
        this.setToken(response.data.accessToken)
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
        this.isNewUser = true
        this.setToken(response.data.accessToken)
        await this.fetchProfile()
        router.push('/dashboard')
      } catch (error) {
        if (error.name === 'AbortError') return
        console.error('Registration failed:', error)
        throw error
      }
    },

    // Exchange the HttpOnly refresh cookie for a fresh in-memory access token.
    // Returns the new token on success, or null if the session can no longer be refreshed.
    // Concurrent callers share a single in-flight request so the single-use refresh
    // token is only rotated once (e.g. app startup racing the OAuth callback).
    async refreshSession() {
      if (this._refreshPromise) return this._refreshPromise

      this._refreshPromise = (async () => {
        try {
          const response = await authService.refresh()
          const token = response.data.accessToken
          this.setToken(token)
          return token
        } catch {
          this.token = null
          return null
        } finally {
          this._refreshPromise = null
        }
      })()

      return this._refreshPromise
    },

    // Called once on app startup to silently restore a session from the refresh cookie.
    async initializeAuth() {
      const token = await this.refreshSession()
      if (token) {
        await this.fetchProfile()
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

    // After the OAuth redirect the refresh cookie is already set by the API.
    // App startup (initializeAuth) exchanges it for a token before mount, so reuse
    // that session here and only refresh if it isn't established yet.
    async handleGoogleLoginCallback(isNewRegistration = false) {
      this.isNewUser = isNewRegistration
      const token = this.token || (await this.refreshSession())
      if (token) {
        if (!this.user) await this.fetchProfile()
        router.push(consumeRedirect() || '/dashboard')
      } else {
        router.push('/accounts/login')
      }
    },

    setToken(token) {
      this.token = token
    },

    setUser(user) {
      this.user = user
      localStorage.setItem('user', JSON.stringify(user))
    },

    async logout() {
      try {
        // Abort all ongoing requests
        Object.values(this.abortControllers).forEach((controller) => {
          if (controller) {
            controller.abort()
          }
        })
        await authService.logout()
      } catch (error) {
        console.error('Logout API call failed:', error)
      }

      this.token = null
      this.user = null
      this.abortControllers = {
        profile: null,
        auth: null,
      }
      this.loading = {
        profile: false,
      }
      localStorage.removeItem('user')
      // Clean up the legacy pre-store cache key so stale data can't leak into the new room store.
      localStorage.removeItem('userRooms')

      useRoomStore().reset()

      router.push('/')
    },
  },
})
