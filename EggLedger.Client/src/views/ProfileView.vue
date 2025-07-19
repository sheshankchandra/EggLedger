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
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import ProfileComponent from '@/components/profile/ProfileComponent.vue'

const authStore = useAuthStore()

// Load user data and rooms when component mounts
onMounted(async () => {
  await authStore.fetchUserRooms()
})
</script>

<style scoped>
.profile-view {
  min-height: 100vh;
  background: var(--bg-secondary);
}

.main-content {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}
</style>
