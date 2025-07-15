<template>
  <div class="accounts-container">
    <div class="accounts-card">
      <div class="brand-header">
        <h1 class="brand-title">EggLedger</h1>
        <p class="brand-subtitle">Manage your orders and inventory</p>
      </div>

      <!-- Mode Toggle Tabs -->
      <div class="mode-toggle">
        <button
          @click="setMode('login')"
          :class="['mode-tab', { active: currentMode === 'login' }]"
        >
          Sign In
        </button>
        <button
          @click="setMode('signup')"
          :class="['mode-tab', { active: currentMode === 'signup' }]"
        >
          Sign Up
        </button>
      </div>

      <!-- Form Components -->
      <div class="form-container">
        <Transition name="slide" mode="out-in">
          <LoginForm
            v-if="currentMode === 'login'"
            @switch-mode="switchToSignup"
            key="login"
          />
          <SignupForm
            v-else
            @switch-mode="switchToLogin"
            key="signup"
          />
        </Transition>
      </div>
    </div>

    <!-- Background decoration -->
    <div class="background-pattern"></div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LoginForm from '@/components/auth/LoginForm.vue'
import SignupForm from '@/components/auth/SignupForm.vue'

// Composables
const route = useRoute()
const router = useRouter()

// Reactive data
const currentMode = ref('login')

// Methods
const setMode = (mode) => {
  currentMode.value = mode
  // Update the URL without triggering navigation
  const newPath = mode === 'login' ? '/eggledger/accounts/login' : '/eggledger/accounts/signup'
  router.replace(newPath)
}

const switchToLogin = () => {
  setMode('login')
}

const switchToSignup = () => {
  setMode('signup')
}

// Initialize mode based on route
onMounted(() => {
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
  padding: 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  position: relative;
  overflow: hidden;
}

.background-pattern {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Ccircle cx='30' cy='30' r='4'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
  z-index: 0;
}

.accounts-card {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  padding: 2.5rem;
  width: 100%;
  max-width: 480px;
  position: relative;
  z-index: 1;
}

.brand-header {
  text-align: center;
  margin-bottom: 2rem;
}

.brand-title {
  font-size: 2.5rem;
  font-weight: 800;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.brand-subtitle {
  font-size: 1rem;
  color: #6b7280;
  font-weight: 400;
}

.mode-toggle {
  display: flex;
  background-color: #f3f4f6;
  border-radius: 0.5rem;
  padding: 0.25rem;
  margin-bottom: 2rem;
}

.mode-tab {
  flex: 1;
  padding: 0.75rem 1rem;
  border: none;
  background: none;
  border-radius: 0.375rem;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
}

.mode-tab.active {
  background-color: white;
  color: #3b82f6;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
}

.mode-tab:hover:not(.active) {
  color: #374151;
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
  .accounts-container {
    padding: 0.5rem;
  }

  .accounts-card {
    padding: 1.5rem;
    border-radius: 0.75rem;
  }

  .brand-title {
    font-size: 2rem;
  }

  .mode-tab {
    padding: 0.625rem 0.75rem;
    font-size: 0.875rem;
  }
}
</style>
