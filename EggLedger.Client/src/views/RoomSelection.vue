<template>
  <div class="container">
    <div class="header">
      <h1>Welcome back, {{ authStore.getUser?.name || 'User' }}!</h1>
      <p>Choose a room to enter or create/join a new one.</p>
    </div>

    <div v-if="authStore.getUserRooms.length === 0" class="no-rooms">
      <div class="empty-state">
        <h2>🏠 No Rooms Yet</h2>
        <p>You're not in any rooms. Create one or join with a code!</p>
        <router-link to="/lobby" class="btn btn-primary">Go to Room Lobby</router-link>
      </div>
    </div>

    <div v-else class="rooms-section">
      <h2>Your Rooms</h2>
      <div class="rooms-grid">
        <div
          v-for="room in authStore.getUserRooms"
          :key="room.id"
          class="room-card"
          @click="enterRoom(room)"
        >
          <div class="room-header">
            <h3>{{ room.name }}</h3>
            <span class="room-code">{{ room.code }}</span>
          </div>
          <div class="room-stats">
            <span class="stat">👥 {{ room.memberCount || 0 }}</span>
            <span class="stat">🥚 {{ room.totalEggs || 0 }}</span>
            <span class="stat">📦 {{ room.containerCount || 0 }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="actions">
      <router-link to="/lobby" class="action-btn">
        <div class="action-icon">🚀</div>
        <h3>Create Room</h3>
        <p>Start a new room</p>
      </router-link>
      <router-link to="/lobby" class="action-btn">
        <div class="action-icon">🚪</div>
        <h3>Join Room</h3>
        <p>Enter with a code</p>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const enterRoom = (room) => {
  sessionStorage.setItem('selectedRoom', JSON.stringify(room))
  sessionStorage.setItem('eggLedgerRoomCode', room.code)
  router.push('/room')
}
</script>

<style scoped>
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 3rem;
}

.header h1 {
  font-size: 2.5rem;
  color: #333;
  margin-bottom: 0.5rem;
}

.header p {
  font-size: 1.2rem;
  color: #666;
}

.no-rooms {
  text-align: center;
  padding: 3rem;
}

.empty-state h2 {
  color: #666;
  margin-bottom: 1rem;
}

.rooms-section h2 {
  font-size: 1.8rem;
  color: #333;
  margin-bottom: 1.5rem;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 3rem;
}

.room-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: all 0.3s ease;
  border: 2px solid transparent;
}

.room-card:hover {
  border-color: #4caf50;
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.15);
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.room-header h3 {
  margin: 0;
  color: #333;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.8rem;
}

.room-stats {
  display: flex;
  gap: 1rem;
}

.stat {
  background: #f5f5f5;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  color: #555;
}

.actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

.action-btn {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  text-decoration: none;
  color: inherit;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
  border: 2px solid transparent;
}

.action-btn:hover {
  border-color: #ff9800;
  transform: translateY(-2px);
  text-decoration: none;
  color: inherit;
}

.action-icon {
  font-size: 2.5rem;
  margin-bottom: 1rem;
}

.action-btn h3 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.action-btn p {
  margin: 0;
  color: #666;
}

.btn {
  display: inline-block;
  padding: 1rem 2rem;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 600;
  transition: all 0.2s ease;
}

.btn-primary {
  background: #4caf50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
  text-decoration: none;
  color: white;
}
</style>
