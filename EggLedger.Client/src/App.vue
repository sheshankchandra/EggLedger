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
import { computed } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const authStore = useAuthStore()
const isAuthenticated = computed(() => authStore.isAuthenticated)

const handleLogout = () => {
  authStore.logout()
}
</script>

<style scoped>
#app {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
header {
  width: 100%;
  border-bottom: 10px solid #eee;
  background: #fafafa;
}
nav {
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: center;
  padding: 1rem;
  gap: 1rem;
  width: 100%;
  box-sizing: border-box;
}
nav a {
  font-weight: bold;
  color: #2c3e50;
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  transition:
    background 0.2s,
    color 0.2s;
}
nav a.router-link-exact-active {
  color: #fff;
  background: #2c3e50;
}
main {
  flex: 1 1 auto;
  padding: 2rem;
  width: 100%;
  box-sizing: border-box;
  display: block;
}
</style>
