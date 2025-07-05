<template>
  <div class="room-indicator" v-if="selectedRoom">
    <span
      >Current Room: <strong>{{ selectedRoom.roomName }}</strong> ({{
        selectedRoom.roomCode
      }})</span
    >
    <button @click="switchRoom" class="switch-room-btn">Switch Room</button>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
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

const switchRoom = () => {
  sessionStorage.removeItem('selectedRoomCode')
  router.push('/')
}
</script>

<style scoped>
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
</style>
