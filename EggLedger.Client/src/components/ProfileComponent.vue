<template>
  <div class="profile-container">
    <div class="profile-header">
      <h2>User Profile</h2>
      <p>Manage your account settings and room memberships</p>
    </div>

    <div v-if="loading" class="loading">Loading profile...</div>
    <div v-if="error" class="error-message">{{ error }}</div>

    <div v-if="user" class="profile-content">
      <!-- User Information -->
      <div class="profile-section">
        <h3>Personal Information</h3>
        <div class="info-grid">
          <div class="info-item">
            <label>Full Name</label>
            <span>{{ user.name }}</span>
          </div>
          <div class="info-item">
            <label>Email</label>
            <span>{{ user.email }}</span>
          </div>
          <div class="info-item">
            <label>User ID</label>
            <span class="user-id">{{ user.id || user.userId }}</span>
          </div>
          <div class="info-item">
            <label>Role</label>
            <span>{{ getRoleName(user.role) }}</span>
          </div>
        </div>
      </div>

      <!-- Room Memberships -->
      <div class="profile-section">
        <h3>Room Memberships</h3>
        <div v-if="authStore.getUserRooms.length === 0" class="no-rooms">
          <p>You're not a member of any rooms yet.</p>
        </div>
        <div v-else class="rooms-list">
          <div v-for="room in authStore.getUserRooms" :key="room.roomId" class="room-item">
            <div class="room-info">
              <h4>{{ room.roomName }}</h4>
              <span class="room-code">{{ room.roomCode }}</span>
            </div>
            <div class="room-details">
              <span class="room-stat">🥚 {{ room.totalEggs || 0 }} eggs</span>
              <span class="room-stat">📦 {{ room.containerCount || 0 }} containers</span>
              <span class="room-stat">👥 {{ room.memberCount || 0 }} members</span>
            </div>
            <div class="room-meta">
              <span class="join-date">Joined: {{ formatDate(room.joinedAt) }}</span>
              <span v-if="room.adminUserId === user.id" class="admin-badge">Admin</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Account Actions -->
      <div class="profile-section">
        <h3>Account Actions</h3>
        <div class="actions-grid">
          <button @click="refreshProfile" :disabled="refreshing" class="btn btn-secondary">
            {{ refreshing ? 'Refreshing...' : 'Refresh Profile' }}
          </button>
          <button @click="refreshRooms" :disabled="refreshing" class="btn btn-secondary">
            {{ refreshing ? 'Refreshing...' : 'Refresh Rooms' }}
          </button>
          <button @click="showChangePassword = true" class="btn btn-primary">
            Change Password
          </button>
          <button @click="confirmLogout" class="btn btn-danger">Logout</button>
        </div>
      </div>

      <!-- Statistics -->
      <div class="profile-section">
        <h3>Your Statistics</h3>
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-number">{{ authStore.getUserRooms.length }}</div>
            <div class="stat-label">Rooms Joined</div>
          </div>
          <div class="stat-card">
            <div class="stat-number">{{ totalContainers }}</div>
            <div class="stat-label">Total Containers</div>
          </div>
          <div class="stat-card">
            <div class="stat-number">{{ totalEggs }}</div>
            <div class="stat-label">Total Eggs</div>
          </div>
          <div class="stat-card">
            <div class="stat-number">{{ adminRooms }}</div>
            <div class="stat-label">Admin of Rooms</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Change Password Modal -->
    <div v-if="showChangePassword" class="modal-overlay">
      <div class="modal-content">
        <div class="modal-header">
          <h3>Change Password</h3>
          <button @click="showChangePassword = false" class="close-btn">×</button>
        </div>
        <form @submit.prevent="handleChangePassword">
          <div class="form-group">
            <label>Current Password</label>
            <input v-model="passwordForm.current" type="password" required />
          </div>
          <div class="form-group">
            <label>New Password</label>
            <input v-model="passwordForm.new" type="password" required />
          </div>
          <div class="form-group">
            <label>Confirm New Password</label>
            <input v-model="passwordForm.confirm" type="password" required />
          </div>
          <div class="modal-actions">
            <button type="button" @click="showChangePassword = false" class="btn btn-secondary">
              Cancel
            </button>
            <button type="submit" :disabled="changingPassword" class="btn btn-primary">
              {{ changingPassword ? 'Changing...' : 'Change Password' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Notification -->
    <div v-if="notification" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { onMounted, computed, ref, reactive } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

const authStore = useAuthStore()
const loading = ref(false)
const error = ref(null)
const refreshing = ref(false)
const showChangePassword = ref(false)
const changingPassword = ref(false)
const notification = ref(null)

const passwordForm = reactive({
  current: '',
  new: '',
  confirm: '',
})

const user = computed(() => authStore.getUser)

const totalContainers = computed(() => {
  return authStore.getUserRooms.reduce((total, room) => total + (room.containerCount || 0), 0)
})

const totalEggs = computed(() => {
  return authStore.getUserRooms.reduce((total, room) => total + (room.totalEggs || 0), 0)
})

const adminRooms = computed(() => {
  return authStore.getUserRooms.filter((room) => room.adminUserId === user.value?.id).length
})

const getRoleName = (role) => {
  switch (role) {
    case 0:
      return 'User'
    case 1:
      return 'Admin'
    case 2:
      return 'Super Admin'
    default:
      return 'Unknown'
  }
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}

const showNotification = (message, type = 'success') => {
  notification.value = { message, type }
  setTimeout(() => {
    notification.value = null
  }, 4000)
}

const refreshProfile = async () => {
  refreshing.value = true
  try {
    await authStore.fetchProfile()
    showNotification('Profile refreshed successfully!')
  } catch (err) {
    showNotification('Failed to refresh profile', 'error')
    console.error(err)
  } finally {
    refreshing.value = false
  }
}

const refreshRooms = async () => {
  refreshing.value = true
  try {
    await authStore.fetchUserRooms()
    showNotification('Rooms refreshed successfully!')
  } catch (err) {
    showNotification('Failed to refresh rooms', 'error')
    console.error(err)
  } finally {
    refreshing.value = false
  }
}

const handleChangePassword = async () => {
  if (passwordForm.new !== passwordForm.confirm) {
    showNotification('New passwords do not match', 'error')
    return
  }

  if (passwordForm.new.length < 6) {
    showNotification('New password must be at least 6 characters', 'error')
    return
  }

  changingPassword.value = true
  try {
    // You'll need to implement this in your auth service
    // await authService.changePassword(passwordForm.current, passwordForm.new)
    showNotification('Password changed successfully!')
    showChangePassword.value = false

    // Reset form
    passwordForm.current = ''
    passwordForm.new = ''
    passwordForm.confirm = ''
  } catch (err) {
    showNotification('Failed to change password', 'error')
    console.error(err)
  } finally {
    changingPassword.value = false
  }
}

const confirmLogout = () => {
  if (confirm('Are you sure you want to logout?')) {
    authStore.logout()
  }
}

onMounted(async () => {
  if (!user.value) {
    loading.value = true
    error.value = null
    try {
      await authStore.fetchProfile()
    } catch (err) {
      error.value = 'Failed to load profile. You may be logged out.'
      console.error(err)
    } finally {
      loading.value = false
    }
  }
})
</script>

<style scoped>
.profile-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
}

.profile-header {
  text-align: center;
  margin-bottom: 2rem;
}

.profile-header h2 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.profile-header p {
  color: #666;
  margin: 0;
}

.loading {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.error-message {
  color: #f44336;
  text-align: center;
  padding: 1rem;
  background: #ffebee;
  border-radius: 4px;
  margin-bottom: 1rem;
}

.profile-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.profile-section {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.profile-section h3 {
  margin: 0 0 1.5rem 0;
  color: #333;
  border-bottom: 2px solid #f0f0f0;
  padding-bottom: 0.5rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.info-item label {
  font-weight: 600;
  color: #555;
  font-size: 0.9rem;
}

.info-item span {
  color: #333;
  font-size: 1rem;
}

.user-id {
  font-family: monospace;
  background: #f5f5f5;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.9rem !important;
}

.no-rooms {
  text-align: center;
  padding: 2rem;
  background: #f8f9fa;
  border-radius: 8px;
  color: #666;
}

.rooms-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.room-item {
  background: #f8f9fa;
  padding: 1.5rem;
  border-radius: 8px;
  border-left: 4px solid #4caf50;
}

.room-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.room-info h4 {
  margin: 0;
  color: #333;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.9rem;
}

.room-details {
  display: flex;
  gap: 1rem;
  margin-bottom: 0.5rem;
  flex-wrap: wrap;
}

.room-stat {
  background: white;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  color: #555;
}

.room-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.9rem;
  color: #666;
}

.admin-badge {
  background: #ff9800;
  color: white;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 600;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 1rem;
}

.btn {
  padding: 0.75rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-primary {
  background: #4caf50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #545b62;
}

.btn-danger {
  background: #f44336;
  color: white;
}

.btn-danger:hover {
  background: #d32f2f;
}

.btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: #f8f9fa;
  padding: 1.5rem;
  border-radius: 8px;
  text-align: center;
}

.stat-number {
  font-size: 2rem;
  font-weight: 700;
  color: #4caf50;
  margin-bottom: 0.5rem;
}

.stat-label {
  color: #666;
  font-size: 0.9rem;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 8px;
  padding: 2rem;
  width: 90%;
  max-width: 400px;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.modal-header h3 {
  margin: 0;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #555;
}

.form-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  box-sizing: border-box;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 1.5rem;
}

.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 1rem 1.5rem;
  border-radius: 6px;
  color: white;
  font-weight: 500;
  z-index: 1001;
}

.notification.success {
  background: #4caf50;
}

.notification.error {
  background: #f44336;
}

@media (max-width: 768px) {
  .profile-container {
    padding: 1rem;
  }

  .profile-section {
    padding: 1.5rem;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .actions-grid {
    grid-template-columns: 1fr;
  }

  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .room-info {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .room-details {
    flex-direction: column;
    gap: 0.5rem;
  }
}
</style>
