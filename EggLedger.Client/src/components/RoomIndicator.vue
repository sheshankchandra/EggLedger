<template>
  <div class="room-indicator" v-if="selectedRoom">
    <span
      >Current Room: <strong>{{ selectedRoom.roomName }}</strong> ({{
        selectedRoom.roomCode
      }})</span
    >
    <button @click="switchRoom" class="btn btn-warning btn-sm">Switch Room</button>
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
  background: var(--color-primary-light);
  padding: var(--spacing-sm) var(--spacing-xl);
  display: flex;
  justify-content: space-between;
  align-items: center;
  max-width: var(--container-max-width);
  margin: 0 auto;
}
</style>
