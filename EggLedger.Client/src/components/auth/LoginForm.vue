<template>
  <div class="auth-form">
    <div class="auth-header">
      <h2 class="auth-title">Welcome Back</h2>
      <p class="auth-subtitle">Sign in to your account</p>
    </div>

    <form @submit.prevent="handleLogin" class="form" novalidate>
      <div class="form-group">
        <label for="email" class="form-label">Email</label>
        <input
          type="email"
          id="email"
          v-model="email"
          class="form-input"
          :class="{ 'is-invalid': touched.email && emailError }"
          :aria-invalid="touched.email && !!emailError"
          aria-describedby="email-feedback"
          required
          :disabled="loading"
          placeholder="Enter your email"
          @blur="touched.email = true"
        />
        <small
          v-if="touched.email && emailError"
          id="email-feedback"
          class="form-feedback is-invalid"
        >
          {{ emailError }}
        </small>
      </div>

      <div class="form-group">
        <label for="password" class="form-label">Password</label>
        <input
          type="password"
          id="password"
          v-model="password"
          class="form-input"
          :class="{ 'is-invalid': touched.password && passwordError }"
          :aria-invalid="touched.password && !!passwordError"
          aria-describedby="password-feedback"
          required
          :disabled="loading"
          placeholder="Enter your password"
          @blur="touched.password = true"
        />
        <small
          v-if="touched.password && passwordError"
          id="password-feedback"
          class="form-feedback is-invalid"
        >
          {{ passwordError }}
        </small>
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

    <button @click="handleGoogleLogin" class="btn btn-google" :disabled="loading">
      <svg class="google-icon" viewBox="0 0 24 24">
        <path
          fill="#4285f4"
          d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
        />
        <path
          fill="#34a853"
          d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
        />
        <path
          fill="#fbbc05"
          d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
        />
        <path
          fill="#ea4335"
          d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
        />
      </svg>
      Continue with Google
    </button>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import authService from '@/services/auth.service'
import { consumeRedirect } from '@/utils/postLoginRedirect'

// Reactive data
const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')
const touched = reactive({ email: false, password: false })

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const emailError = computed(() => {
  if (!email.value) return 'Email is required.'
  if (!EMAIL_PATTERN.test(email.value)) return 'Enter a valid email address.'
  return ''
})
const passwordError = computed(() => (!password.value ? 'Password is required.' : ''))

// Composables
const router = useRouter()
const authStore = useAuthStore()

// Methods
const handleLogin = async () => {
  touched.email = true
  touched.password = true
  if (emailError.value || passwordError.value) return

  loading.value = true
  error.value = ''

  try {
    await authStore.login({
      email: email.value,
      password: password.value,
    })

    router.push(consumeRedirect() || '/dashboard')
  } catch (err) {
    error.value = err.response.data.join(' ') || 'Login failed. Please try again.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const handleGoogleLogin = async () => {
  loading.value = true
  try {
    await authService.googleLogin()
  } catch (err) {
    error.value = err.response.data.join(' ') || 'Google login failed. Please try again.'
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
  margin-bottom: var(--spacing-xl);
}

.auth-title {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin-bottom: var(--spacing-sm);
}

.auth-subtitle {
  font-size: var(--font-size-base);
  color: var(--text-muted);
}

.form {
  margin-bottom: var(--spacing-lg);
}

.btn-google {
  width: 100%;
  background: var(--bg-primary);
  border-color: var(--border-medium);
  color: var(--text-secondary);
}

.btn-google:hover:not(:disabled) {
  background: var(--bg-tertiary);
  box-shadow: var(--shadow-sm);
}

.google-icon {
  width: 20px;
  height: 20px;
}
</style>
