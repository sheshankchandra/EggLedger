<template>
  <div class="auth-form">
    <div class="auth-header">
      <h2 class="auth-title">Create Account</h2>
      <p class="auth-subtitle">Sign up to get started</p>
    </div>

    <form @submit.prevent="handleRegister" class="form">
      <div class="form-group">
        <label for="firstName" class="form-label">First Name</label>
        <input
          type="text"
          id="firstName"
          v-model="form.firstName"
          class="form-input"
          required
          :disabled="loading"
          placeholder="Enter your first name"
        />
      </div>

      <div class="form-group">
        <label for="lastName" class="form-label">Last Name</label>
        <input
          type="text"
          id="lastName"
          v-model="form.lastName"
          class="form-input"
          required
          :disabled="loading"
          placeholder="Enter your last name"
        />
      </div>

      <div class="form-group">
        <label for="email" class="form-label">Email</label>
        <input
          type="email"
          id="email"
          v-model="form.email"
          class="form-input"
          required
          :disabled="loading"
          placeholder="Enter your email"
        />
      </div>

      <div class="form-group">
        <label for="password" class="form-label">Password</label>
        <input
          type="password"
          id="password"
          v-model="form.password"
          class="form-input"
          required
          :disabled="loading"
          placeholder="Create a password"
        />
      </div>

      <button type="submit" class="btn btn-primary w-full" :disabled="loading">
        <span v-if="loading" class="spinner"></span>
        {{ loading ? 'Creating Account...' : 'Create Account' }}
      </button>

      <div v-if="error" class="alert alert-error">
        {{ error }}
      </div>

      <div v-if="success" class="alert alert-success">
        Registration successful! You can now log in.
      </div>
    </form>

    <div class="divider">
      <span>or</span>
    </div>

    <button @click="handleGoogleRegister" class="btn-google" :disabled="loading">
      <svg class="google-icon" viewBox="0 0 24 24">
        <path fill="#4285f4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
        <path fill="#34a853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
        <path fill="#fbbc05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
        <path fill="#ea4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
      </svg>
      Continue with Google
    </button>

    <div class="auth-switch">
      <p>Already have an account?
        <button @click="$emit('switch-mode')" class="link-button">Sign in here</button>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

// Emits
defineEmits(['switch-mode'])

// Reactive data
const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  password: ''
})
const loading = ref(false)
const error = ref('')
const success = ref(false)

// Composables
const router = useRouter()
const authStore = useAuthStore()

// Methods
const handleRegister = async () => {
  if (!form.firstName || !form.lastName || !form.email || !form.password) {
    error.value = 'Please fill in all fields'
    return
  }

  loading.value = true
  error.value = ''
  success.value = false

  try {
    await authStore.register(form)
    success.value = true

    // Reset form
    Object.keys(form).forEach(key => {
      form[key] = ''
    })

    // Auto-redirect to login after 2 seconds
    setTimeout(() => {
      router.push('/eggledger/accounts/login')
    }, 2000)
  } catch (err) {
    error.value = err.message || 'Registration failed. Please try again.'
  } finally {
    loading.value = false
  }
}

const handleGoogleRegister = async () => {
  loading.value = true
  try {
    await authStore.googleRegister()
  } catch (err) {
    error.value = err.message || 'Google registration failed. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-form {
  width: 100%;
  max-width: 400px;
}

.auth-header {
  text-align: center;
  margin-bottom: 2rem;
}

.auth-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.auth-subtitle {
  font-size: 1rem;
  color: #6b7280;
}

.form {
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 1rem;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

.form-input:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-input:disabled {
  background-color: #f9fafb;
  color: #6b7280;
}

.btn {
  padding: 0.75rem 1rem;
  border-radius: 0.375rem;
  font-weight: 500;
  font-size: 1rem;
  border: none;
  cursor: pointer;
  transition: all 0.15s ease-in-out;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-primary {
  background-color: #3b82f6;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #2563eb;
}

.btn-primary:disabled {
  background-color: #9ca3af;
  cursor: not-allowed;
}

.w-full {
  width: 100%;
}

.btn-google {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  background-color: white;
  color: #374151;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease-in-out;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-google:hover:not(:disabled) {
  background-color: #f9fafb;
}

.btn-google:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.google-icon {
  width: 20px;
  height: 20px;
}

.divider {
  position: relative;
  text-align: center;
  margin: 1.5rem 0;
}

.divider::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background-color: #e5e7eb;
  z-index: 1;
}

.divider span {
  position: relative;
  background-color: white;
  padding: 0 1rem;
  color: #6b7280;
  font-size: 0.875rem;
  z-index: 2;
}

.alert {
  padding: 0.75rem;
  border-radius: 0.375rem;
  margin-top: 1rem;
}

.alert-error {
  background-color: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.alert-success {
  background-color: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.auth-switch {
  text-align: center;
  margin-top: 1.5rem;
}

.auth-switch p {
  color: #6b7280;
  font-size: 0.875rem;
}

.link-button {
  background: none;
  border: none;
  color: #3b82f6;
  text-decoration: underline;
  cursor: pointer;
  font-size: inherit;
}

.link-button:hover {
  color: #2563eb;
}
</style>
