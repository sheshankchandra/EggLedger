<template>
  <header class="main-header">
    <div class="header-content">
      <h1 @click="$router.push('/')" class="app-title">EggLedger</h1>
      <nav class="main-nav">
        <router-link to="/" class="nav-btn" active-class="active"> Dashboard </router-link>
        <router-link to="/room" class="nav-btn" active-class="active" v-if="selectedRoom">
          Room
        </router-link>
        <router-link to="/profile" class="nav-btn" active-class="active"> Profile </router-link>
        <div class="nav-spacer"></div>
        <button @click="handleLogout" class="btn btn-danger btn-sm">Logout</button>
      </nav>
    </div>
  </header>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

const authStore = useAuthStore()

const selectedRoomCode = computed(() => {
  return sessionStorage.getItem('selectedRoomCode')
})

const selectedRoom = computed(() => {
  if (!selectedRoomCode.value) return null
  // Convert stored room code to number to match the data type from API
  const roomCodeToFind = Number(selectedRoomCode.value)
  return authStore.getUserRooms.find((room) => room.roomCode === roomCodeToFind) || null
})

const handleLogout = () => {
  authStore.logout()
}
</script>

<style scoped>
.main-header {
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.header-content {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-md) var(--spacing-xl);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-title {
  margin: 0;
  color: var(--text-primary);
  cursor: pointer;
  transition: color var(--transition-fast);
}

.app-title:hover {
  color: var(--color-primary);
}

.main-nav {
  display: flex;
  gap: var(--spacing-md);
  align-items: center;
}

.nav-spacer {
  width: var(--spacing-xl);
}

.nav-btn {
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--border-medium);
  background: var(--bg-primary);
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--text-primary);
  transition: all var(--transition-normal);
}

.nav-btn:hover {
  background: var(--bg-tertiary);
}

.nav-btn.active {
  background: var(--color-primary);
  color: var(--text-inverse);
  border-color: var(--color-primary);
}
</style>
