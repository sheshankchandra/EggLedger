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
        <button @click="handleLogout" class="logout-btn">Logout</button>
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
  background: white;
  border-bottom: 1px solid #e0e0e0;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-title {
  margin: 0;
  color: #333;
  cursor: pointer;
  transition: color 0.2s;
}

.app-title:hover {
  color: #4caf50;
}

.main-nav {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.nav-spacer {
  width: 2rem;
}

.nav-btn {
  padding: 0.5rem 1rem;
  border: 1px solid #ddd;
  background: white;
  border-radius: 4px;
  text-decoration: none;
  color: #333;
  transition: all 0.2s;
}

.nav-btn:hover {
  background: #f0f0f0;
}

.nav-btn.active {
  background: #4caf50;
  color: white;
  border-color: #4caf50;
}

.logout-btn {
  padding: 0.5rem 1rem;
  border: 1px solid #f44336;
  background: #f44336;
  color: white;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.logout-btn:hover {
  background: #d32f2f;
}
</style>
