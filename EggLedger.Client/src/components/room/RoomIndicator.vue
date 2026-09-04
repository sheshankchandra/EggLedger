<template>
  <div class="room-indicator" v-if="selectedRoom">
    <div>
      <span class="indicator-label">Current room</span>
      <strong>{{ selectedRoom.roomName }}</strong>
      <span class="room-code">{{ selectedRoom.roomCode }}</span>
    </div>
    <button @click="switchRoom" class="switch-button" type="button">Switch room</button>
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
  width: min(100% - 2rem, var(--container-max-width));
  margin: var(--spacing-md) auto 0;
  padding: var(--spacing-sm) var(--spacing-md);
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--spacing-md);
  background: var(--color-primary-light);
  border: 1px solid rgba(23, 107, 82, 0.14);
  border-radius: var(--radius-lg);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.indicator-label {
  margin-right: var(--spacing-sm);
}

.room-code {
  margin-left: var(--spacing-sm);
  color: var(--color-primary);
  font-family: var(--font-family-mono);
}

.switch-button {
  padding: var(--spacing-xs) var(--spacing-sm);
  border: 0;
  background: transparent;
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

@media (max-width: 520px) {
  .room-indicator {
    width: min(100% - 1.25rem, var(--container-max-width));
  }

  .indicator-label {
    display: none;
  }
}
</style>
