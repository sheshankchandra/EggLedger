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
app.use(router)

// Silently restore the session from the HttpOnly refresh cookie before mounting,
// so a page reload keeps the user signed in and protected routes resolve correctly.
const authStore = useAuthStore(pinia)
authStore.initializeAuth().finally(() => {
  app.mount('#app')
})
