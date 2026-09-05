<template>
  <div class="accounts-container">
    <div class="accounts-card">
      <router-link to="/" class="brand-header">
        <img src="/eggledger.png" alt="" class="brand-logo" />
        <div>
          <p class="brand-title">EggLedger</p>
          <p class="brand-subtitle">Manage your orders and inventory</p>
        </div>
      </router-link>

      <!-- Mode Toggle Tabs -->
      <div class="mode-toggle" role="tablist" aria-label="Sign in or sign up">
        <button
          @click="switchToLogin"
          :class="['mode-tab', { active: currentMode === 'login' }]"
          type="button"
          role="tab"
          :aria-selected="currentMode === 'login'"
        >
          Sign In
        </button>
        <button
          @click="switchToSignup"
          :class="['mode-tab', { active: currentMode === 'signup' }]"
          type="button"
          role="tab"
          :aria-selected="currentMode === 'signup'"
        >
          Sign Up
        </button>
      </div>

      <div class="form-container">
        <Transition name="slide" mode="out-in">
          <LoginForm v-if="currentMode === 'login'" />
          <SignupForm v-else />
        </Transition>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watchEffect } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LoginForm from '@/components/auth/LoginForm.vue'
import SignupForm from '@/components/auth/SignupForm.vue'

// Composables
const route = useRoute()
const router = useRouter()

// Reactive data
const currentMode = ref('login')

// Methods
const switchToLogin = () => {
  currentMode.value = 'login'
  router.push('/accounts/login')
}

const switchToSignup = () => {
  currentMode.value = 'signup'
  router.push('/accounts/signup')
}

// Initialize mode based on route and react to changes
watchEffect(() => {
  if (route.path.includes('signup')) {
    currentMode.value = 'signup'
  } else {
    currentMode.value = 'login'
  }
})
</script>

<style scoped>
.accounts-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  background: var(--bg-secondary);
}

.accounts-card {
  background: var(--bg-primary);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-xl);
  padding: var(--spacing-2xl);
  width: 100%;
  max-width: 440px;
}

.brand-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xl);
  color: var(--text-primary);
  text-decoration: none;
}

.brand-logo {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
  object-fit: contain;
}

.brand-title {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: -0.02em;
}

.brand-subtitle {
  margin: 2px 0 0;
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.mode-toggle {
  display: grid;
  grid-template-columns: 1fr 1fr;
  margin-bottom: var(--spacing-xl);
  padding: var(--spacing-xs);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
}

.mode-tab {
  padding: var(--spacing-sm);
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.mode-tab.active {
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
  color: var(--color-primary);
}

.mode-tab:hover:not(.active) {
  color: var(--text-primary);
}

.form-container {
  display: flex;
  justify-content: center;
}

/* Transition styles */
.slide-enter-active,
.slide-leave-active {
  transition: all 0.3s ease-in-out;
}

.slide-enter-from {
  opacity: 0;
  transform: translateX(20px);
}

.slide-leave-to {
  opacity: 0;
  transform: translateX(-20px);
}

/* Responsive design */
@media (max-width: 640px) {
  .accounts-card {
    padding: var(--spacing-lg);
  }
}
</style>
