<template>
  <div class="profile-container">
    <h2>User Profile</h2>
    <div v-if="loading">Loading profile...</div>
    <div v-if="error" class="error-message">{{ error }}</div>
    <div v-if="user" class="profile-details">
      <p><strong>ID:</strong> {{ user.id }}</p>
      <p><strong>Name:</strong> {{ user.name }}</p>
      <p><strong>Email:</strong> {{ user.email }}</p>
      <!-- Add other profile fields as needed -->
    </div>
  </div>
</template>

<script setup>
import { onMounted, computed, ref } from 'vue';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const loading = ref(false);
const error = ref(null);

const user = computed(() => authStore.getUser);

onMounted(async () => {
  // If user data is not already in the store, fetch it.
  if (!user.value) {
    loading.value = true;
    error.value = null;
    try {
      await authStore.fetchProfile();
    } catch (err) {
      error.value = "Failed to load profile. You may be logged out.";
      console.error(err);
    } finally {
      loading.value = false;
    }
  }
});
</script>

<style scoped>
.profile-container { max-width: 600px; margin: 50px auto; padding: 20px; border: 1px solid #ccc; border-radius: 8px; }
.profile-details p { font-size: 1.1em; }
.error-message { color: red; }
</style>