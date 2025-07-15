import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import AccountsView from '../views/AccountsView.vue'
import DashboardView from '../views/DashboardView.vue'
import RoomView from '../views/RoomView.vue'
import ProfileView from '../views/ProfileView.vue'
import GoogleCallbackView from '../views/GoogleCallbackView.vue'
import LobbyView from '../views/LobbyView.vue'
import ContainerDetailView from '../views/ContainerDetailView.vue'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
    },
    {
      path: '/register',
      name: 'register',
      component: RegisterView,
    },
    // New unified accounts routes
    {
      path: '/eggledger/accounts/login',
      name: 'accounts-login',
      component: AccountsView,
    },
    {
      path: '/eggledger/accounts/signup',
      name: 'accounts-signup',
      component: AccountsView,
    },
    {
      path: '/lobby',
      name: 'lobby',
      component: LobbyView,
      meta: { requiresAuth: true },
    },
    {
      path: '/auth/callback',
      name: 'google-callback',
      component: GoogleCallbackView,
    },
    {
      path: '/',
      name: 'dashboard',
      component: DashboardView,
      meta: { requiresAuth: true },
    },
    {
      path: '/room',
      name: 'room',
      component: RoomView,
      meta: { requiresAuth: true },
    },
    {
      path: '/profile',
      name: 'profile',
      component: ProfileView,
      meta: { requiresAuth: true },
    },
    {
      path: '/container/:containerId',
      name: 'container-detail',
      component: ContainerDetailView,
      meta: { requiresAuth: true },
      props: (route) => ({
        containerId: route.params.containerId,
      }),
    },
  ],
})

// Navigation Guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  // Define auth-related routes
  const authRoutes = ['login', 'register', 'accounts-login', 'accounts-signup']

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/eggledger/accounts/login')
  } else if (authRoutes.includes(to.name) && authStore.isAuthenticated) {
    next('/')
  } else {
    next()
  }
})

export default router
