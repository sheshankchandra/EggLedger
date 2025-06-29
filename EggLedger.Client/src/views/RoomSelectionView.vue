<template>
  <div class="room-selection-container">
    <div class="header">
      <h2>Welcome back, {{ authStore.getUser?.name || 'User' }}!</h2>
      <p>Choose a room to enter or create/join a new one.</p>
    </div>

    <!-- User's Rooms -->
    <div class="rooms-section">
      <h3>Your Rooms</h3>
      <div v-if="authStore.getUserRooms.length === 0" class="no-rooms">
        <p>You're not in any rooms yet.</p>
        <router-link to="/lobby" class="btn btn-primary">Go to Room Lobby</router-link>
      </div>
      <div v-else class="rooms-grid">
        <div
          v-for="room in authStore.getUserRooms"
          :key="room.id"
          class="room-card"
          @click="enterRoom(room)"
        >
          <div class="room-header">
            <h4>{{ room.name }}</h4>
            <span class="room-code">{{ room.code }}</span>
          </div>
          <div class="room-info">
            <p><strong>Members:</strong> {{ room.memberCount || 0 }}</p>
            <p><strong>Created:</strong> {{ formatDate(room.createdAt) }}</p>
          </div>
          <div class="room-stats">
            <span class="stat"> 🥚 {{ room.totalEggs || 0 }} eggs </span>
            <span class="stat"> 📦 {{ room.containerCount || 0 }} containers </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Actions -->
    <div class="actions-section">
      <h3>Other Options</h3>
      <div class="actions-grid">
        <router-link to="/lobby" class="action-card">
          <div class="action-icon">🚀</div>
          <h4>Create New Room</h4>
          <p>Start a fresh room for your group</p>
        </router-link>
        <router-link to="/lobby" class="action-card">
          <div class="action-icon">🚪</div>
          <h4>Join Another Room</h4>
          <p>Enter a room with an invitation code</p>
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const enterRoom = (room) => {
  // Store the selected room info for the RoomPage component
  sessionStorage.setItem('selectedRoom', JSON.stringify(room))
  sessionStorage.setItem('eggLedgerRoomCode', room.code)
  router.push('/room')
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}
</script>

<style scoped>
.room-selection-container {
  max-width: 1200px;
  margin: 2rem auto;
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 3rem;
}

.header h2 {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
  margin-bottom: 0.5rem;
}

.header p {
  font-size: 1.1rem;
  color: #666;
}

.rooms-section {
  margin-bottom: 3rem;
}

.rooms-section h3 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
  color: #333;
}

.no-rooms {
  text-align: center;
  padding: 2rem;
  background: #f8f9fa;
  border-radius: 8px;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.room-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.3s ease;
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

.room-header h4 {
  margin: 0;
  color: #333;
  font-size: 1.2rem;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.9rem;
}

.room-info {
  margin-bottom: 1rem;
}

.room-info p {
  margin: 0.25rem 0;
  color: #666;
  font-size: 0.9rem;
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

.actions-section h3 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
  color: #333;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.action-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  text-decoration: none;
  color: inherit;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 2px solid transparent;
  transition: all 0.3s ease;
}

.action-card:hover {
  border-color: #ff9800;
  transform: translateY(-2px);
  text-decoration: none;
  color: inherit;
}

.action-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.action-card h4 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.action-card p {
  margin: 0;
  color: #666;
  font-size: 0.9rem;
}

.btn {
  display: inline-block;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 500;
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
