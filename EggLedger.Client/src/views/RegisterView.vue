<template>
  <div class="register-container">
    <h2>Create an Account</h2>
    <form @submit.prevent="handleRegister">
      <div class="form-group">
        <label for="firstName">First Name:</label>
        <input type="text" v-model="form.firstName" required />
      </div>
      <div class="form-group">
        <label for="lastName">Last Name:</label>
        <input type="text" v-model="form.lastName" required />
      </div>
      <div class="form-group">
        <label for="email">Email:</label>
        <input type="email" v-model="form.email" required />
      </div>
      <div class="form-group">
        <label for="password">Password:</label>
        <input type="password" v-model="form.password" required />
      </div>
      <button type="submit" :disabled="loading">
        {{ loading ? 'Registering...' : 'Register' }}
      </button>
      <div v-if="error" class="error-message">{{ error }}</div>
      <div v-if="success" class="success-message">
        Registration successful! You can now log in.
      </div>
    </form>
    <div class="login-link">
      <p>Already have an account? <router-link to="/login">Log In</router-link></p>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { userService } from '@/services/user.service'; 

const router = useRouter();
const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  role: 0 // Assuming 0 is the default 'User' role
});

const loading = ref(false);
const error = ref(null);
const success = ref(false);

const handleRegister = async () => {
  loading.value = true;
  error.value = null;
  success.value = false;
  try {
    await userService.register(form);
    success.value = true;
    setTimeout(() => {
        router.push('/login');
    }, 2000);
  } catch (err) {
    if (err.response && err.response.data) {
      error.value = err.response.data;
    } else {
      error.value = 'An unexpected error occurred.';
    }
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.register-container { max-width: 400px; margin: 50px auto; padding: 20px; border: 1px solid #ccc; border-radius: 8px; }
.form-group { margin-bottom: 15px; }
label { display: block; margin-bottom: 5px; }
input { width: 100%; padding: 8px; box-sizing: border-box; }
button { width: 100%; padding: 10px; background-color: #28a745; color: white; border: none; border-radius: 4px; cursor: pointer; }
button:disabled { background-color: #aaa; }
.error-message { color: red; margin-top: 10px; }
.success-message { color: green; margin-top: 10px; }
.login-link { text-align: center; margin-top: 20px; }
</style>