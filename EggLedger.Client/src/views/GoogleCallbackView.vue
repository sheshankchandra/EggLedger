<template>
  <div>
    <p>Authenticating, please wait...</p>
  </div>
</template>

<script setup>
import { onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';

onMounted(() => {
  const route = useRoute();
  const authStore = useAuthStore();

  // Extract the token from the URL query parameters
  const token = route.query.token;

  if (token) {
    // Use the store action to handle the token
    authStore.handleGoogleLoginCallback(token);
  } else {
    // Handle error - maybe redirect to login with an error message
    console.error("Google login failed: No token provided.");
    authStore.logout(); // Go back to the login page
  }
});
</script>