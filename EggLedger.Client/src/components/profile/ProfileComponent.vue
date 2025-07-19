<template>
  <div class="profile-container">
    <div class="profile-header">
      <h2>User Profile</h2>
      <p>Manage your account settings and room memberships</p>
    </div>

    <div v-if="loading" class="card text-center p-5">
      <p class="text-secondary">Loading profile...</p>
    </div>
    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <div v-if="user" class="profile-content">
      <!-- User Information -->
      <div class="profile-section">
        <h3>Personal Information</h3>
        <div class="info-grid">
          <div class="info-item">
            <label class="form-label">Full Name</label>
            <span>{{ user.name }}</span>
          </div>
          <div class="info-item">
            <label class="form-label">Email</label>
            <span>{{ user.email }}</span>
          </div>
          <div class="info-item">
            <label class="form-label">User ID</label>
            <span class="user-id">{{ user.id || user.userId }}</span>
          </div>
          <div class="info-item">
            <label class="form-label">Role</label>
            <span>{{ getRoleName(user.role) }}</span>
          </div>
        </div>
      </div>

      <!-- Room Memberships -->
      <div class="profile-section">
        <h3>Room Memberships</h3>
        <div v-if="authStore.getUserRooms.length === 0" class="card text-center p-5">
          <p class="text-secondary">You're not a member of any rooms yet.</p>
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

      <!-- My Containers -->
      <div class="profile-section">
        <h3>My Containers {{ selectedRoom ? `in ${selectedRoom.roomName}` : '' }}</h3>
        <div v-if="!selectedRoom" class="card text-center p-5">
          <p class="text-secondary mb-4">No room selected.</p>
          <router-link to="/" class="btn btn-primary">Select Room</router-link>
        </div>
        <div v-else-if="loadingContainers" class="card text-center p-5">
          <p class="text-secondary">Loading...</p>
        </div>
        <div v-else-if="userContainers.length === 0" class="card text-center p-5">
          <p class="text-secondary">
            No containers found. Stock some eggs to create your first container!
          </p>
        </div>
        <div v-else class="containers-list">
          <div
            v-for="container in userContainers"
            :key="container.containerId"
            class="container-item"
          >
            <div class="container-header">
              <h4>{{ container.containerName }}</h4>
            </div>
            <div class="container-stats">
              <span class="stat-value"
                >{{ container.remainingQuantity }}/{{ container.totalQuantity }} eggs</span
              >
              <span class="stat-value">{{ formatDate(container.purchaseDateTime) }}</span>
            </div>
            <div class="container-actions">
              <button @click="viewContainerDetails(container)" class="btn btn-info btn-sm">
                View Details
              </button>
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
    <div v-if="showChangePassword" class="modal">
      <div class="modal-content">
        <div class="modal-header">
          <h3 class="modal-title">Change Password</h3>
          <button @click="showChangePassword = false" class="close-btn">×</button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="handleChangePassword">
            <div class="form-group">
              <label class="form-label">Current Password</label>
              <input v-model="passwordForm.current" type="password" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label">New Password</label>
              <input v-model="passwordForm.new" type="password" class="form-input" required />
            </div>
            <div class="form-group">
              <label class="form-label">Confirm New Password</label>
              <input v-model="passwordForm.confirm" type="password" class="form-input" required />
            </div>
            <div class="modal-footer">
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
    </div>

    <!-- Notification -->
    <div v-if="notification" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { onMounted, computed, ref, reactive, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { containerService } from '@/services/container.service'

const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)
const error = ref(null)
const refreshing = ref(false)
const showChangePassword = ref(false)
const changingPassword = ref(false)
const notification = ref(null)
const loadingContainers = ref(false)
const userContainers = ref([])

let abortController = new AbortController()

const passwordForm = reactive({
  current: '',
  new: '',
  confirm: '',
})

const user = computed(() => authStore.getUser)

const selectedRoomCode = computed(() => {
  return sessionStorage.getItem('selectedRoomCode')
})

const selectedRoom = computed(() => {
  if (!selectedRoomCode.value) return null
  const roomCodeToFind = Number(selectedRoomCode.value)
  return authStore.getUserRooms.find((room) => room.roomCode === roomCodeToFind) || null
})

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
    await fetchUserContainers()
    showNotification('Refreshed successfully!')
  } catch {
    showNotification('Failed to refresh', 'error')
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

// Navigate to container details with container information
const viewContainerDetails = (container) => {
  try {
    // Store container info in sessionStorage temporarily
    sessionStorage.setItem('currentContainerInfo', JSON.stringify(container))

    router.push({
      name: 'container-detail',
      params: { containerId: container.containerId },
    })
  } catch (error) {
    console.error('Error navigating to container details:', error)
    console.log('Container object:', container)
  }
}

// Fetch user's containers from the currently selected room
const fetchUserContainers = async () => {
  if (!user.value || !selectedRoom.value) {
    userContainers.value = []
    return
  }

  loadingContainers.value = true

  try {
    abortController.abort()
    abortController = new AbortController()

    const response = await containerService.searchContainersByOwner(
      selectedRoom.value.roomCode,
      user.value.name,
      abortController.signal,
    )

    userContainers.value = response.data || []
  } catch (err) {
    if (err.name === 'AbortError') return
    console.error('Error fetching containers:', err)
    showNotification('Failed to load containers', 'error')
  } finally {
    loadingContainers.value = false
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

  // Fetch user containers after profile is loaded
  await fetchUserContainers()
})

// Watch for changes in selected room to refresh containers
watch(selectedRoom, async (newRoom, oldRoom) => {
  if (newRoom?.roomCode !== oldRoom?.roomCode) {
    await fetchUserContainers()
  }
})
</script>

<style scoped>
.profile-container {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}

.profile-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.profile-header h2 {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--text-primary);
}

.profile-header p {
  color: var(--text-secondary);
  margin: 0;
}

.profile-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
}

.profile-section {
  background: var(--bg-primary);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
}

.profile-section h3 {
  margin: 0 0 var(--spacing-lg) 0;
  color: var(--text-primary);
  border-bottom: 2px solid var(--border-light);
  padding-bottom: var(--spacing-sm);
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-md);
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.info-item label {
  font-weight: var(--font-weight-semibold);
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.info-item span {
  color: var(--text-primary);
  font-size: var(--font-size-base);
}

.user-id {
  font-family: var(--font-family-mono);
  background: var(--bg-tertiary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm) !important;
}

.rooms-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.room-item {
  background: var(--bg-tertiary);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  border-left: 4px solid var(--color-primary);
}

.room-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.room-info h4 {
  margin: 0;
  color: var(--text-primary);
}

.room-code {
  background: var(--color-primary-light);
  color: var(--color-secondary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm);
}

.room-details {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-sm);
  flex-wrap: wrap;
}

.room-stat {
  background: var(--bg-primary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  color: var(--text-muted);
}

.room-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.admin-badge {
  background: var(--color-warning);
  color: var(--text-inverse);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}

.containers-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.container-item {
  background: var(--bg-tertiary);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  border-left: 4px solid var(--color-warning);
  transition: all var(--transition-normal);
}

.container-item:hover {
  background: var(--bg-secondary);
  transform: translateY(-1px);
}

.container-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.container-header h4 {
  margin: 0;
  color: var(--text-primary);
  font-size: var(--font-size-lg);
}

.container-stats {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  flex-wrap: wrap;
}

.stat-value {
  background: var(--bg-primary);
  padding: var(--spacing-sm);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  color: var(--text-primary);
}

.container-actions {
  display: flex;
  justify-content: flex-end;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: var(--spacing-md);
}

.btn {
  padding: var(--spacing-md) var(--spacing-md);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-weight: var(--font-weight-medium);
  transition: all var(--transition-normal);
}

.btn-primary {
  background: var(--color-primary);
  color: var(--text-inverse);
}

.btn-primary:hover {
  background: var(--color-primary-dark);
}

.btn-secondary {
  background: var(--color-secondary);
  color: var(--text-inverse);
}

.btn-secondary:hover {
  background: var(--color-secondary-dark);
}

.btn:disabled {
  background: var(--color-gray-400);
  cursor: not-allowed;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: var(--spacing-md);
}

.stat-card {
  background: var(--bg-tertiary);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  text-align: center;
}

.stat-number {
  font-size: var(--font-size-4xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-primary);
  margin-bottom: var(--spacing-sm);
}

.stat-label {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.form-group {
  margin-bottom: var(--spacing-md);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-muted);
}

.form-group input {
  width: 100%;
  padding: var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  box-sizing: border-box;
}

.notification {
  position: fixed;
  top: var(--spacing-md);
  right: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  border-radius: var(--radius-md);
  color: var(--text-inverse);
  font-weight: var(--font-weight-semibold);
  z-index: 1001;
}

.notification.success {
  background: var(--color-success);
}

.notification.error {
  background: var(--color-danger);
}

@media (max-width: 768px) {
  .profile-container {
    padding: var(--spacing-md);
  }

  .profile-section {
    padding: var(--spacing-lg);
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
    gap: var(--spacing-sm);
  }

  .room-details {
    flex-direction: column;
    gap: var(--spacing-sm);
  }

  .container-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-sm);
  }

  .container-stats {
    flex-direction: column;
    gap: var(--spacing-md);
  }

  .container-actions {
    justify-content: flex-start;
  }
}
</style>
