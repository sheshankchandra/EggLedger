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

// Silently restore the session from the HttpOnly refresh cookie BEFORE installing
// the router. The router runs its navigation guard during install, so if we mount
// it first the guard sees no access token yet and redirects protected routes to
// login. Waiting for the refresh means the guard evaluates against the real state.
const authStore = useAuthStore(pinia)
authStore.initializeAuth().finally(() => {
  app.use(router)
  app.mount('#app')
})
