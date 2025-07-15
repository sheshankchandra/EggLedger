<template>
  <div class="home-container">
    <div class="welcome-section">
      <h1 class="welcome-title">Welcome to EggLedger</h1>
      <p class="welcome-description">Manage your orders and inventory efficiently</p>

      <div class="action-buttons">
        <router-link to="/eggledger/accounts/login" class="btn btn-primary">
          Sign In
        </router-link>
        <router-link to="/eggledger/accounts/signup" class="btn btn-secondary">
          Create Account
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
// This is now a simple landing page that directs users to the accounts view
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
}

.welcome-section {
  text-align: center;
  color: white;
  max-width: 600px;
}

.welcome-title {
  font-size: 3rem;
  font-weight: 800;
  margin-bottom: 1rem;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}

.welcome-description {
  font-size: 1.25rem;
  margin-bottom: 2rem;
  opacity: 0.9;
}

.action-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.btn {
  padding: 0.875rem 2rem;
  border-radius: 0.5rem;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s ease-in-out;
  display: inline-block;
}

.btn-primary {
  background-color: white;
  color: #667eea;
}

.btn-primary:hover {
  background-color: #f8fafc;
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.2);
}

.btn-secondary {
  background-color: transparent;
  color: white;
  border: 2px solid white;
}

.btn-secondary:hover {
  background-color: white;
  color: #667eea;
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.2);
}

@media (max-width: 640px) {
  .welcome-title {
    font-size: 2rem;
  }

  .welcome-description {
    font-size: 1rem;
  }

  .action-buttons {
    flex-direction: column;
    align-items: center;
  }

  .btn {
    width: 100%;
    max-width: 280px;
  }
}
</style>
