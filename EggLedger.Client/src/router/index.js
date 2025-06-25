import { createRouter, createWebHistory } from 'vue-router';
import LoginView from '../views/LoginView.vue';
import RegisterView from '../views/RegisterView.vue';
import ProfileView from '../views/ProfileView.vue';
import DashboardView from '../views/DashboardView.vue';
import ContainerDetailView from '../views/ContainerDetailView.vue';
import GoogleCallbackView from '../views/GoogleCallbackView.vue';
import { useAuthStore } from '@/stores/auth.store';

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
    {
      path: '/profile',
      name: 'profile',
      component: ProfileView,
      meta: { requiresAuth: true } // Mark this route as protected
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
      meta: { requiresAuth: true }
    },
    {
      path: '/container/:id',
      name: 'container-detail',
      component: ContainerDetailView,
      meta: { requiresAuth: true }
    },
    {
      path: '/profile',
      name: 'profile',
      component: ProfileView,
      meta: { requiresAuth: true }
    },
    // Redirect root to profile if logged in, otherwise to login
    {
      path: '/',
      redirect: () => {
        const authStore = useAuthStore();
        return authStore.isAuthenticated ? '/profile' : '/login';
      }
    }
  ]
});

// Navigation Guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // If route requires auth and user is not authenticated, redirect to login
    next('/login');
  } else if (to.name === 'login' && authStore.isAuthenticated) {
    // If user is authenticated and tries to go to login page, redirect to profile
    next('/profile');
  }
  else {
    // Otherwise, allow navigation
    next();
  }
});


export default router