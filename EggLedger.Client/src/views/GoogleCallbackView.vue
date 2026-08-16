<template>
  <div>
    <p>Authenticating, please wait...</p>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const authStore = useAuthStore()

onMounted(() => {
  // No tokens are in the URL anymore. The API set an HttpOnly refresh cookie
  // during the redirect; we exchange it for an in-memory access token.
  const isNewRegistration = route.query.isNewRegistration === 'True'
  authStore.handleGoogleLoginCallback(isNewRegistration)
})
</script>
