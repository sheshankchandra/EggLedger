<template>
  <header>
    <nav>
      <RouterLink v-if="isAuthenticated" to="/dashboard">Dashboard</RouterLink>
      <RouterLink v-else to="/login">Home</RouterLink>

      <template v-if="isAuthenticated">
        <RouterLink to="/profile">Profile</RouterLink>
        <a href="#" @click.prevent="handleLogout">Logout</a>
      </template>

      <template v-else>
        <RouterLink to="/login">Login</RouterLink>
        <RouterLink to="/register">Register</RouterLink>
      </template>
    </nav>
  </header>

  <main>
    <RouterView />
  </main>
</template>

<script setup>
// No changes needed in the script part of App.vue
import { computed } from 'vue';
import { RouterLink, RouterView } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const isAuthenticated = computed(() => authStore.isAuthenticated);

const handleLogout = () => {
  authStore.logout();
};
</script>

<style scoped>
/* Your existing styles are fine */
header { line-height: 1.5; border-bottom: 1px solid #eee; }
nav { display: flex; justify-content: center; align-items: center; padding: 1rem; gap: 1rem; }
nav a { font-weight: bold; color: #2c3e50; text-decoration: none; }
nav a.router-link-exact-active { color: #42b983; }
main { padding: 2rem; }
</style>