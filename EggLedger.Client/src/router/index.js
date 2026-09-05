import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import AccountsView from '../views/AccountsView.vue'
import DashboardView from '../views/DashboardView.vue'
import RoomView from '../views/RoomView.vue'
import BalancesView from '../views/BalancesView.vue'
import RoomActivityView from '../views/RoomActivityView.vue'
import ProfileView from '../views/ProfileView.vue'
import GoogleCallbackView from '../views/GoogleCallbackView.vue'
import ContainerDetailView from '../views/ContainerDetailView.vue'
import JoinInviteView from '../views/JoinInviteView.vue'
import NotFoundView from '../components/common/NotFoundComponent.vue'
import { useAuthStore } from '@/stores/auth.store'
import { rememberRedirect } from '@/utils/postLoginRedirect'

const routes = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
  },
  {
    path: '/accounts/login',
    name: 'accounts-login',
    component: AccountsView,
  },
  {
    path: '/accounts/signup',
    name: 'accounts-signup',
    component: AccountsView,
  },
  {
    path: '/auth/callback',
    name: 'google-callback',
    component: GoogleCallbackView,
  },
  {
    path: '/dashboard',
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
    path: '/room/balances',
    name: 'room-balances',
    component: BalancesView,
    meta: { requiresAuth: true },
  },
  {
    path: '/room/activity',
    name: 'room-activity',
    component: RoomActivityView,
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
  {
    path: '/join',
    name: 'join-invite',
    component: JoinInviteView,
    meta: { requiresAuth: true },
    props: (route) => ({ code: route.query.code }),
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: NotFoundView,
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Navigation Guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  // Define auth-related routes
  const authRoutes = ['accounts-login', 'accounts-signup']

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // Remember where an unauthenticated visit was headed (e.g. a shared invite link) so
    // login can send the user back here instead of always landing on the dashboard.
    rememberRedirect(to.fullPath)
    next('/accounts/login')
  } else if (authRoutes.includes(to.name) && authStore.isAuthenticated) {
    // Redirect authenticated users from accounts to dashboard
    next('/dashboard')
  } else if (to.name === 'home' && authStore.isAuthenticated) {
    // Redirect authenticated users from home to dashboard
    next('/dashboard')
  } else {
    next()
  }
})

export default router
