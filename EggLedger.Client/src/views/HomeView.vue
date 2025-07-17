<template>
  <div class="home-container">
    <div class="welcome-section">
      <h1 class="welcome-title">Welcome to EggLedger</h1>
      <p class="welcome-description">Manage your orders and inventory efficiently</p>

      <div class="action-buttons">
        <router-link to="/accounts/login" class="btn btn-primary"> Sign In </router-link>
        <router-link to="/accounts/signup" class="btn btn-secondary"> Create Account </router-link>
      </div>
    </div>

    <div class="background-pattern"></div>
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
  position: relative;
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

.welcome-section {
  text-align: center;
  color: white;
  max-width: 600px;
  position: relative;
  z-index: 1;
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
