<template>
  <div class="room-view">
    <!-- Navigation Header -->
    <NavigationHeader />

    <!-- Current Room Indicator -->
    <RoomIndicator />

    <!-- Room Content -->
    <main class="main-content">
      <div v-if="authStore.isLoadingRooms" class="card text-center p-5">
        <h2>Loading...</h2>
        <p class="text-secondary">Fetching room information...</p>
      </div>
      <div v-else-if="!selectedRoom" class="card text-center p-5">
        <h2>No Room Selected</h2>
        <p class="text-secondary mb-4">Please select a room from the dashboard first.</p>
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
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import RoomComponent from '@/components/room/RoomComponent.vue'

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
  background: var(--bg-secondary);
}

.main-content {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}
</style>
