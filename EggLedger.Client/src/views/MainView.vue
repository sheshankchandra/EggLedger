<template>
  <div class="main-container">
    <!-- Navigation Header -->
    <header class="main-header">
      <div class="header-content">
        <h1 @click="currentView = 'dashboard'" class="app-title">EggLedger</h1>
        <nav class="main-nav">
          <button
            @click="currentView = 'dashboard'"
            :class="{ active: currentView === 'dashboard' }"
          >
            Dashboard
          </button>
          <button
            @click="currentView = 'room'"
            :class="{ active: currentView === 'room' }"
            v-if="selectedRoom"
          >
            Room
          </button>
          <button @click="currentView = 'profile'" :class="{ active: currentView === 'profile' }">
            Profile
          </button>
          <div class="nav-spacer"></div>
          <button @click="handleLogout" class="logout-btn">Logout</button>
        </nav>
      </div>
    </header>

    <!-- Current Room Indicator -->
    <div class="room-indicator" v-if="selectedRoom">
      <span
        >Current Room: <strong>{{ selectedRoom.roomName }}</strong> ({{
          selectedRoom.roomCode
        }})</span
      >
      <button @click="switchRoom" class="switch-room-btn">Switch Room</button>
    </div>

    <!-- Dynamic Content Area -->
    <main class="main-content">
      <!-- Dashboard Component (Room Selection) -->
      <component
        v-if="currentView === 'dashboard'"
        :is="DashboardComponent"
        @room-selected="handleRoomSelected"
        @room-created="handleRoomCreated"
      />

      <!-- Room Component (Room Dashboard) -->
      <component
        v-else-if="currentView === 'room' && selectedRoom"
        :is="RoomComponent"
        :room="selectedRoom"
        @room-updated="handleRoomUpdated"
      />

      <!-- Profile Component -->
      <component v-else-if="currentView === 'profile'" :is="ProfileComponent" />

      <!-- Fallback to dashboard -->
      <component
        v-else
        :is="RoomSelectionComponent"
        @room-selected="handleRoomSelected"
        @room-created="handleRoomCreated"
      />
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import DashboardComponent from '@/components/DashboardComponent.vue'
import RoomComponent from '@/components/RoomComponent.vue'
import ProfileComponent from '@/components/ProfileComponent.vue'

const authStore = useAuthStore()

const currentView = ref('dashboard')
const selectedRoom = ref(null)

// Load selected room from storage on mount
onMounted(() => {
  const savedRoom = sessionStorage.getItem('selectedRoom')
  if (savedRoom) {
    selectedRoom.value = JSON.parse(savedRoom)
    currentView.value = 'room'
  }
})

const handleRoomSelected = (room) => {
  selectedRoom.value = room
  sessionStorage.setItem('selectedRoom', JSON.stringify(room))
  currentView.value = 'room'
}

const handleRoomCreated = (room) => {
  selectedRoom.value = room
  sessionStorage.setItem('selectedRoom', JSON.stringify(room))
  currentView.value = 'room'
}

const handleRoomUpdated = (updatedRoom) => {
  selectedRoom.value = updatedRoom
  sessionStorage.setItem('selectedRoom', JSON.stringify(updatedRoom))
}

const switchRoom = () => {
  selectedRoom.value = null
  sessionStorage.removeItem('selectedRoom')
  currentView.value = 'dashboard'
}

const handleLogout = () => {
  authStore.logout()
}
</script>

<style scoped>
.main-container {
  min-height: 100vh;
  background: #f5f5f5;
}

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

.main-nav button {
  padding: 0.5rem 1rem;
  border: 1px solid #ddd;
  background: white;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.main-nav button:hover {
  background: #f0f0f0;
}

.main-nav button.active {
  background: #4caf50;
  color: white;
  border-color: #4caf50;
}

.logout-btn {
  background: #f44336 !important;
  color: white !important;
  border-color: #f44336 !important;
}

.room-indicator {
  background: #e8f5e8;
  padding: 0.5rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  max-width: 1200px;
  margin: 0 auto;
}

.switch-room-btn {
  background: #ff9800;
  color: white;
  border: none;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
}

.main-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}
</style>
