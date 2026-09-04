<template>
  <header class="main-header">
    <div class="header-content">
      <router-link to="/dashboard" class="app-branding" aria-label="EggLedger dashboard">
        <img src="/eggledger.png" alt="EggLedger Logo" class="app-logo" />
        <span class="app-title">EggLedger</span>
      </router-link>
      <nav class="main-nav" aria-label="Primary navigation">
        <router-link to="/dashboard" class="nav-link" active-class="active">
          <span aria-hidden="true">⌂</span>
          <span>Rooms</span>
        </router-link>
        <router-link to="/room" class="nav-btn" active-class="active" v-if="selectedRoom">
          <span aria-hidden="true">▦</span>
          <span class="nav-room-name">{{ selectedRoom.roomName }}</span>
        </router-link>
        <router-link to="/profile" class="nav-link" active-class="active">
          <span aria-hidden="true">○</span>
          <span>Profile</span>
        </router-link>
        <button @click="handleLogout" class="logout-button" type="button" aria-label="Sign out">
          <span aria-hidden="true">↪</span>
          <span class="logout-label">Sign out</span>
        </button>
      </nav>
    </div>
  </header>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

const authStore = useAuthStore()

const selectedRoomCode = computed(() => {
  return sessionStorage.getItem('selectedRoomCode')
})

const selectedRoom = computed(() => {
  if (!selectedRoomCode.value) return null
  // Convert stored room code to number to match the data type from API
  const roomCodeToFind = Number(selectedRoomCode.value)
  return authStore.getUserRooms.find((room) => room.roomCode === roomCodeToFind) || null
})

const handleLogout = () => {
  authStore.logout()
}
</script>

<style scoped>
.main-header {
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
  min-height: var(--header-height);
  background: rgba(255, 255, 255, 0.94);
  border-bottom: 1px solid var(--border-light);
  backdrop-filter: blur(14px);
}

.header-content {
  max-width: var(--container-max-width);
  margin: 0 auto;
  min-height: var(--header-height);
  padding: var(--spacing-sm) var(--spacing-xl);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-branding {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--text-primary);
  text-decoration: none;
}

.app-logo {
  width: 38px;
  height: 38px;
  object-fit: contain;
}

.app-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  letter-spacing: -0.02em;
  transition: color var(--transition-fast);
}

.app-branding:hover .app-title {
  color: var(--color-primary);
}

.main-nav {
  display: flex;
  gap: var(--spacing-xs);
  align-items: center;
}

.nav-link,
.nav-btn,
.logout-button {
  display: inline-flex;
  min-height: 42px;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: var(--font-weight-medium);
  transition:
    color var(--transition-fast),
    background-color var(--transition-fast);
}

.nav-link:hover,
.nav-btn:hover,
.logout-button:hover {
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

.nav-link.active,
.nav-btn.active {
  background: var(--color-primary-light);
  color: var(--color-primary);
}

.logout-button {
  background: transparent;
  cursor: pointer;
}

@media (max-width: 768px) {
  .header-content {
    padding-inline: var(--spacing-md);
  }

  .main-nav {
    gap: 0;
  }

  .nav-link,
  .nav-btn,
  .logout-button {
    padding-inline: var(--spacing-sm);
    font-size: var(--font-size-sm);
  }

  .nav-room-name,
  .logout-label {
    max-width: 0;
    overflow: hidden;
    white-space: nowrap;
  }

  .app-title {
    display: none;
  }
}
</style>
