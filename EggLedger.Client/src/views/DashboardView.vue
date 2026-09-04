<template>
  <div class="dashboard-view">
    <NavigationHeader />
    <main class="page-shell">
      <DashboardComponent @room-selected="handleRoomSelected" />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useRoomStore } from '@/stores/room.store'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import DashboardComponent from '@/components/dashboard/DashboardComponent.vue'

const router = useRouter()
const roomStore = useRoomStore()

// Load user's rooms when component mounts
onMounted(async () => {
  await roomStore.fetchUserRooms()
})

const handleRoomSelected = (roomCode) => {
  roomStore.selectRoom(roomCode)
  router.push('/room')
}
</script>

<style scoped>
.dashboard-view {
  min-height: 100vh;
}
</style>
