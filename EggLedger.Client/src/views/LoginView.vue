<template>
  <div class="login-container">
    <h2>Login</h2>
    <form @submit.prevent="handleLogin">
      <div class="form-group">
        <label for="email">Email:</label>
        <input type="email" v-model="email" required />
      </div>
      <div class="form-group">
        <label for="password">Password:</label>
        <input type="password" v-model="password" required />
      </div>
      <button type="submit" :disabled="loading">
        {{ loading ? 'Logging in...' : 'Login' }}
      </button>
      <div v-if="error" class="error-message">{{ error }}</div>
    </form>
    <hr />
    <button @click="handleGoogleLogin" class="google-btn">
      Login with Google
    </button>
    <div class="register-link">
      <p>Don't have an account? <router-link to="/register">Create one</router-link></p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useAuthStore } from '@/stores/auth.store';

const email = ref('');
const password = ref('');
const loading = ref(false);
const error = ref(null);

const authStore = useAuthStore();

const handleLogin = async () => {
  loading.value = true;
  error.value = null;
  try {
    await authStore.login({ email: email.value, password: password.value });
    // The store will handle redirection on success
  } catch (err) {
    error.value = 'Invalid email or password.';
    console.error(err);
  } finally {
    loading.value = false;
  }
};

const handleGoogleLogin = () => {
  // Redirect to the backend endpoint that starts the Google OAuth flow
  const googleLoginUrl = `${import.meta.env.VITE_API_BASE_URL}/api/auth/google-login`;
  window.location.href = googleLoginUrl;
};
</script>

<style scoped>
.login-container { max-width: 400px; margin: 50px auto; padding: 20px; border: 1px solid #ccc; border-radius: 8px; }
.form-group { margin-bottom: 15px; }
label { display: block; margin-bottom: 5px; }
input { width: 100%; padding: 8px; box-sizing: border-box; }
button { width: 100%; padding: 10px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; }
button:disabled { background-color: #aaa; }
.google-btn { background-color: #db4437; margin-top: 10px; }
hr { margin: 20px 0; }
.error-message { color: red; margin-top: 10px; }
.register-link { text-align: center; margin-top: 20px; }
</style>