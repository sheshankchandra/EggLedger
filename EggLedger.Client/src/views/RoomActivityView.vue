<template>
  <div class="activity-view">
    <NavigationHeader />
    <RoomIndicator />
    <main class="page-shell">
      <div v-if="roomStore.isLoading" class="card text-center p-5">
        <h2>Loading...</h2>
        <p class="text-secondary">Fetching room information...</p>
      </div>
      <div v-else-if="!roomStore.selectedRoom" class="card text-center p-5">
        <h2>No Room Selected</h2>
        <p class="text-secondary mb-4">Please select a room from the dashboard first.</p>
        <router-link to="/" class="btn btn-primary">Go to Dashboard</router-link>
      </div>
      <ActivityComponent v-else :room="roomStore.selectedRoom" />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useRoomStore } from '@/stores/room.store'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import ActivityComponent from '@/components/activity/ActivityComponent.vue'

const router = useRouter()
const roomStore = useRoomStore()

// Redirect to dashboard if no room is selected and fetch user rooms
onMounted(async () => {
  if (roomStore.selectedRoomCode == null) {
    router.push('/')
    return
  }

  if (!roomStore.isLoading) {
    await roomStore.fetchUserRooms()
  }

  if (!roomStore.selectedRoom) {
    router.push('/')
  }
})
</script>

<style scoped>
.activity-view {
  min-height: 100vh;
}
</style>
