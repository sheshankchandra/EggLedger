import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth.store'

// Import consolidated CSS
import './assets/styles/main.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)

// Restore the session from the refresh cookie before installing the router, so its
// navigation guards evaluate against the authenticated state on the initial load.
const authStore = useAuthStore(pinia)
authStore.initializeAuth().finally(() => {
  app.use(router)
  app.mount('#app')
})
