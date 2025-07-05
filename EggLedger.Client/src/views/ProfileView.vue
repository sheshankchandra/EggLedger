<template>
  <div class="profile-view">
    <!-- Navigation Header -->
    <NavigationHeader />

    <!-- Current Room Indicator -->
    <RoomIndicator />

    <!-- Profile Content -->
    <main class="main-content">
      <ProfileComponent />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import NavigationHeader from '@/components/NavigationHeader.vue'
import RoomIndicator from '@/components/RoomIndicator.vue'
import ProfileComponent from '@/components/ProfileComponent.vue'

const authStore = useAuthStore()

// Load user data and rooms when component mounts
onMounted(async () => {
  await authStore.fetchUserRooms()
})
</script>

<style scoped>
.profile-view {
  min-height: 100vh;
  background: #f5f5f5;
}

.main-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}
</style>
