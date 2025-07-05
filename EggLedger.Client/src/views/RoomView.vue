<template>
  <div class="room-view">
    <!-- Navigation Header -->
    <NavigationHeader />

    <!-- Current Room Indicator -->
    <RoomIndicator />

    <!-- Room Content -->
    <main class="main-content">
      <div v-if="authStore.isLoadingRooms" class="loading">
        <h2>Loading...</h2>
        <p>Fetching room information...</p>
      </div>
      <div v-else-if="!selectedRoom" class="no-room">
        <h2>No Room Selected</h2>
        <p>Please select a room from the dashboard first.</p>
        <router-link to="/" class="btn btn-primary">Go to Dashboard</router-link>
      </div>
      <RoomComponent v-else :room="selectedRoom" />
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import NavigationHeader from '@/components/NavigationHeader.vue'
import RoomIndicator from '@/components/RoomIndicator.vue'
import RoomComponent from '@/components/RoomComponent.vue'

const router = useRouter()
const authStore = useAuthStore()

const selectedRoomCode = computed(() => {
  return sessionStorage.getItem('selectedRoomCode')
})

// Computed property to get the current room data from auth store
const selectedRoom = computed(() => {
  if (!selectedRoomCode.value) return null
  const rooms = authStore.getUserRooms

  // Convert stored room code to number to match the data type from API
  const roomCodeToFind = Number(selectedRoomCode.value)

  const room = rooms.find((room) => room.roomCode === roomCodeToFind)
  return room || null
})

// Redirect to dashboard if no room is selected and fetch user rooms
onMounted(async () => {
  // If no room code is stored, redirect immediately
  if (!selectedRoomCode.value) {
    router.push('/')
    return
  }

  // Fetch user rooms if not already loading
  if (!authStore.isLoadingRooms) {
    await authStore.fetchUserRooms()
  }

  // Check again after fetching rooms
  setTimeout(() => {
    if (!selectedRoom.value) {
      router.push('/')
    }
  }, 100) // Small delay to allow for reactivity
})
</script>

<style scoped>
.room-view {
  min-height: 100vh;
  background: #f5f5f5;
}

.main-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.no-room {
  text-align: center;
  background: white;
  padding: 3rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.loading {
  text-align: center;
  background: white;
  padding: 3rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.no-room h2,
.loading h2 {
  margin: 0 0 1rem 0;
  color: #333;
}

.no-room p,
.loading p {
  margin: 0 0 2rem 0;
  color: #666;
}

.btn {
  display: inline-block;
  padding: 0.75rem 1.5rem;
  border-radius: 4px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-primary {
  background: #4caf50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
}
</style>
