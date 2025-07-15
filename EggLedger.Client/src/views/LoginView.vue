<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <h1 class="login-title">EggLedger</h1>
        <p class="login-subtitle">Sign in to your account</p>
      </div>

      <form @submit.prevent="handleLogin" class="login-form">
        <div class="form-group">
          <label for="email" class="form-label">Email</label>
          <input
            type="email"
            id="email"
            v-model="email"
            class="form-input"
            required
            :disabled="loading"
          />
        </div>

        <div class="form-group">
          <label for="password" class="form-label">Password</label>
          <input
            type="password"
            id="password"
            v-model="password"
            class="form-input"
            required
            :disabled="loading"
          />
        </div>

        <button type="submit" class="btn btn-primary w-full" :disabled="loading">
          <span v-if="loading" class="spinner"></span>
          {{ loading ? 'Signing in...' : 'Sign In' }}
        </button>

        <div v-if="error" class="alert alert-error">
          {{ error }}
        </div>
      </form>

      <div class="divider">
        <span>or</span>
      </div>

      <button @click="handleGoogleLogin" class="btn-google" :disabled="loading">
        <svg class="google-icon" viewBox="0 0 24 24">
          <path fill="#4285f4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
          <path fill="#34a853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
          <path fill="#fbbc05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
          <path fill="#ea4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
        </svg>
        Continue with Google
      </button>

      <div class="register-link">
        <p>
          Don't have an account?
          <router-link to="/register" class="link">Create one here</router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import authService from '@/services/auth.service'

const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref(null)

const authStore = useAuthStore()

const handleLogin = async () => {
  loading.value = true
  error.value = null
  try {
    await authStore.login({ email: email.value, password: password.value })
    // The store will handle redirection on success
  } catch (err) {
    error.value = 'Invalid email or password.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const handleGoogleLogin = () => {
  authService.googleLogin()
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  background: linear-gradient(135deg, var(--color-secondary) 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-md);
}

.login-card {
  background: var(--bg-primary);
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-xl);
  padding: var(--spacing-2xl) var(--spacing-xl);
  width: 100%;
  max-width: 400px;
  transition: transform var(--transition-slow);
}

.login-card:hover {
  transform: translateY(-5px);
}

.login-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.login-title {
  font-size: var(--font-size-4xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin: 0 0 var(--spacing-sm) 0;
}

.login-subtitle {
  color: var(--text-secondary);
  font-size: var(--font-size-base);
  margin: 0;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.form-label {
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  font-size: var(--font-size-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.form-input {
  padding: var(--spacing-md) var(--spacing-md);
  border: 2px solid var(--border-light);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-base);
  transition: all var(--transition-normal);
  background: var(--bg-tertiary);
}

.form-input:focus {
  outline: none;
  border-color: var(--color-secondary);
  background: var(--bg-primary);
  box-shadow: var(--input-focus-ring);
}

.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: linear-gradient(135deg, var(--color-secondary) 0%, #764ba2 100%);
  color: var(--text-inverse);
  border: none;
  padding: var(--spacing-md) var(--spacing-lg);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition: all var(--transition-normal);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(102, 126, 234, 0.3);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid transparent;
  border-top: 2px solid var(--text-inverse);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.divider {
  margin: var(--spacing-xl) 0 var(--spacing-lg) 0;
  position: relative;
  text-align: center;
}

.divider::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background: var(--border-light);
}

.divider span {
  background: var(--bg-primary);
  padding: 0 var(--spacing-md);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.btn-google {
  width: 100%;
  padding: var(--spacing-md);
  border: 2px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all var(--transition-normal);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xl);
}

.btn-google:hover:not(:disabled) {
  border-color: var(--border-medium);
  background: var(--bg-tertiary);
}

.btn-google:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.google-icon {
  width: 20px;
  height: 20px;
}

.register-link {
  text-align: center;
}

.register-link p {
  color: var(--text-secondary);
  margin: 0;
}

.link {
  color: var(--color-secondary);
  text-decoration: none;
  font-weight: var(--font-weight-semibold);
}

.link:hover {
  text-decoration: underline;
}

.w-full {
  width: 100%;
}

@media (max-width: 480px) {
  .login-card {
    padding: var(--spacing-xl) var(--spacing-lg);
    margin: var(--spacing-md);
  }

  .login-title {
    font-size: var(--font-size-3xl);
  }
}
</style>
