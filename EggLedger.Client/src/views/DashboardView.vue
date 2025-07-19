<template>
  <div class="dashboard-view">
    <!-- Navigation Header -->
    <NavigationHeader />

    <!-- Current Room Indicator -->
    <RoomIndicator />

    <!-- Dashboard Content -->
    <main class="main-content">
      <DashboardComponent @room-selected="handleRoomSelected" />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import DashboardComponent from '@/components/dashboard/DashboardComponent.vue'

const router = useRouter()
const authStore = useAuthStore()

// Load user data and rooms when component mounts
onMounted(async () => {
  await authStore.fetchUserRooms()
})

const handleRoomSelected = (roomCode) => {
  sessionStorage.setItem('selectedRoomCode', roomCode)
  router.push('/room')
}
</script>

<style scoped>
.dashboard-view {
  min-height: 100vh;
  background: var(--bg-secondary);
}

.main-content {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}
</style>
