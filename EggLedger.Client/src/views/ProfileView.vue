<template>
  <div class="profile-view">
    <!-- Navigation Header -->
    <NavigationHeader />

    <!-- Current Room Indicator -->
    <RoomIndicator />

    <!-- Profile Content -->
    <main class="page-shell">
      <ProfileComponent />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRoomStore } from '@/stores/room.store'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import ProfileComponent from '@/components/profile/ProfileComponent.vue'

const roomStore = useRoomStore()

// Load the user's rooms when component mounts
onMounted(async () => {
  await roomStore.fetchUserRooms()
})
</script>

<style scoped>
.profile-view {
  min-height: 100vh;
  background: var(--bg-secondary);
}
</style>
