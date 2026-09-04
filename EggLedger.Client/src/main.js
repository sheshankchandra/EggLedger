import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth.store'
import { useThemeStore } from '@/stores/theme.store'

// Import consolidated CSS
import './assets/styles/main.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)

// Applied synchronously, before mount, so there's no flash of the wrong theme.
useThemeStore(pinia).init()

// Restore the session from the refresh cookie before installing the router, so its
// navigation guards evaluate against the authenticated state on the initial load.
const authStore = useAuthStore(pinia)
authStore.initializeAuth().finally(() => {
  app.use(router)
  app.mount('#app')
})
