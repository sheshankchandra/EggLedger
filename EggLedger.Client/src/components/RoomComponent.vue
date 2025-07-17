<template>
  <div class="dashboard-container">
    <div class="dashboard-header">
      <div class="header-top">
        <h2>{{ room.roomName }}</h2>
        <button
          v-if="isRoomAdmin"
          @click="showDeleteConfirm"
          class="btn btn-danger btn-sm"
          title="Delete Room"
        >
          🗑️ Delete Room
        </button>
      </div>
      <div v-if="containersLoading" class="text-secondary">Loading containers...</div>
      <div v-else class="room-info">
        <span class="room-code">Room Code: {{ room.roomCode }}</span>
        <span class="room-stats">
          🥚 {{ room.totalEggs || 0 }} eggs | 📦 {{ room.containerCount || 0 }} containers | 👥
          {{ room.memberCount || 0 }} members
        </span>
      </div>
    </div>

    <!-- Main Actions -->
    <div class="main-actions">
      <h3>Main Actions</h3>
      <div class="actions-grid">
        <!-- Stock Eggs -->
        <div class="action-card">
          <h4>🛒 Stock Eggs</h4>
          <p>Add new eggs to the room</p>
          <form @submit.prevent="handleStock">
            <div class="form-group">
              <label class="form-label">Container Name</label>
              <input
                v-model="stockForm.containerName"
                type="text"
                placeholder="e.g., Fresh Eggs"
                class="form-input"
              />
            </div>
            <div class="form-group">
              <label class="form-label">Quantity <span class="required">*</span></label>
              <input
                v-model.number="stockForm.quantity"
                type="number"
                min="1"
                class="form-input"
                required
              />
            </div>
            <div class="form-group">
              <label class="form-label">Price per container <span class="required">*</span></label>
              <input
                v-model.number="stockForm.amount"
                type="number"
                step="0.01"
                min="0"
                class="form-input"
                required
              />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-success w-full">
              {{ loading ? 'Adding...' : 'Add Stock' }}
            </button>
          </form>
        </div>

        <!-- Consume Eggs -->
        <div class="action-card">
          <h4>🍳 Consume Eggs</h4>
          <p>Record eggs you've used</p>
          <form @submit.prevent="handleConsume">
            <div class="form-group">
              <label class="form-label">Quantity <span class="required">*</span></label>
              <input
                v-model.number="consumeForm.quantity"
                type="number"
                min="1"
                class="form-input"
                required
              />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-primary w-full">
              {{ loading ? 'Recording...' : 'Record Consumption' }}
            </button>
          </form>
        </div>
      </div>
      <div v-if="error" class="alert alert-error">{{ error }}</div>
    </div>

    <div class="container-list">
      <h3>Containers in this Room</h3>
      <div v-if="containersLoading" class="text-secondary">Loading containers...</div>
      <div v-else-if="containers.length === 0" class="text-secondary">
        No containers found. Add one above!
      </div>
      <ul v-else>
        <li v-for="container in containers" :key="container.containerId" class="container-item">
          <div class="container-info">
            <strong>{{ container.containerName }}</strong>
            <span>
              - Eggs Left: {{ container.remainingQuantity }} / {{ container.totalQuantity }}</span
            >
            <small class="owner-info">Owner: {{ container.buyerName }}</small>
          </div>
          <div class="container-actions">
            <button @click="openContainerDetail(container)" class="btn btn-info btn-sm">
              View Details
            </button>
          </div>
        </li>
      </ul>
    </div>

    <!-- Container Detail Modal -->
    <div v-if="showDetailModal" class="modal">
      <div class="modal-content">
        <div class="modal-header">
          <h3 class="modal-title">{{ selectedContainer.containerName }} - Details</h3>
          <button @click="closeDetailModal" class="close-btn">×</button>
        </div>
        <div class="modal-body">
          <div class="detail-info">
            <p><strong>Owner:</strong> {{ selectedContainer.buyerName }}</p>
            <p>
              <strong>Capacity:</strong>
              {{ selectedContainer.TotalQuantity || selectedContainer.totalQuantity }}
            </p>
            <p>
              <strong>Current Stock:</strong>
              {{ selectedContainer.RemainingQuantity || selectedContainer.remainingQuantity }}
            </p>
            <p><strong>Created:</strong> {{ formatDate(selectedContainer.purchaseDateTime) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Room Confirmation Modal -->
    <div v-if="showDeleteModal" class="modal">
      <div class="modal-content">
        <div class="modal-header">
          <h3 class="modal-title text-danger">⚠️ Delete Room</h3>
          <button @click="closeDeleteModal" class="close-btn">×</button>
        </div>
        <div class="modal-body">
          <p>
            Are you sure you want to delete "<strong>{{ room.roomName }}</strong
            >"?
          </p>
          <div class="alert alert-warning">
            This action cannot be undone. All data associated with this room will be permanently
            deleted.
          </div>
          <div class="room-stats-summary">
            <p><strong>This will permanently delete:</strong></p>
            <ul>
              <li>{{ room.containerCount || 0 }} containers</li>
              <li>{{ room.totalEggs || 0 }} eggs worth of data</li>
              <li>All transaction history</li>
              <li>{{ room.memberCount || 0 }} member associations</li>
            </ul>
          </div>
        </div>
        <div class="modal-footer">
          <button @click="closeDeleteModal" class="btn btn-secondary">Cancel</button>
          <button @click="confirmDeleteRoom" :disabled="loading" class="btn btn-danger">
            {{ loading ? 'Deleting...' : 'Delete Room' }}
          </button>
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
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { containerService } from '@/services/container.service'
import { orderService } from '@/services/order.service'
import roomService from '@/services/room.service'

const authStore = useAuthStore()
const router = useRouter()
const props = defineProps({
  room: {
    type: Object,
    required: true,
  },
})

let abortController = new AbortController()

const containers = ref([])
const containersLoading = ref(true)
const loading = ref(false)
const error = ref(null)
const notification = ref(null)

const showDetailModal = ref(false)
const selectedContainer = ref(null)
const showDeleteModal = ref(false)

// Computed properties
const isRoomAdmin = computed(() => {
  const user = authStore.getUser
  if (!user || !user.userId || !props.room || !props.room.adminUserId) {
    return false
  }
  return user.userId === props.room.adminUserId
})

const stockForm = ref({
  containerName: '',
  quantity: 30,
  amount: 200,
})

const consumeForm = ref({
  quantity: 1,
})

// Methods
const showNotification = (message, type = 'success') => {
  notification.value = { message, type }
  setTimeout(() => {
    notification.value = null
  }, 4000)
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}

const showDeleteConfirm = () => {
  showDeleteModal.value = true
}

const closeDeleteModal = () => {
  showDeleteModal.value = false
}

const confirmDeleteRoom = async () => {
  if (loading.value) return

  // Additional safety check
  if (!isRoomAdmin.value) {
    showNotification('You do not have permission to delete this room.', 'error')
    closeDeleteModal()
    return
  }

  abortController.abort()
  abortController = new AbortController()
  loading.value = true

  try {
    await roomService.deleteRoom(props.room.roomCode, abortController.signal)
    showNotification('Room deleted successfully!', 'success')

    // Clear the selected room from session storage
    sessionStorage.removeItem('selectedRoomCode')

    // Refresh user rooms in the auth store
    await authStore.fetchUserRooms()

    // Navigate back to dashboard
    setTimeout(() => {
      router.push('/')
    }, 1500)

    closeDeleteModal()
  } catch (error) {
    if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') return

    let errorMessage = 'Could not delete the room. Please try again.'
    if (Array.isArray(error.response?.data)) {
      errorMessage = error.response.data.join(', ')
    } else if (typeof error.response?.data === 'string') {
      errorMessage = error.response.data
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    }
    showNotification(errorMessage, 'error')
  } finally {
    loading.value = false
  }
}

const fetchContainers = async () => {
  abortController.abort()
  abortController = new AbortController()

  containersLoading.value = true
  try {
    const response = await containerService.getContainers(
      props.room.roomCode,
      abortController.signal,
    )
    containers.value = response.data
  } catch (err) {
    if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
    console.error('Failed to fetch containers:', err)
    error.value = 'Could not load containers.'
  } finally {
    containersLoading.value = false
  }
}

const handleStock = async () => {
  abortController.abort()
  abortController = new AbortController()

  loading.value = true
  error.value = null

  try {
    await orderService.stockOrder(
      props.room.roomCode,
      {
        containerName: stockForm.value.containerName,
        quantity: stockForm.value.quantity,
        amount: stockForm.value.amount,
      },
      abortController.signal,
    )

    await authStore.fetchUserRooms()
    await fetchContainers()
    showNotification('Stock added successfully!')

    // Reset form
    stockForm.value.containerName = ''
    stockForm.value.quantity = 30
    stockForm.value.amount = 200
  } catch (err) {
    if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
    error.value = 'Failed to add stock.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const handleConsume = async () => {
  abortController.abort()
  abortController = new AbortController()

  loading.value = true
  error.value = null
  try {
    // Import orderService here since we need it for consumption
    const { orderService } = await import('@/services/order.service')

    await orderService.consumeOrder(
      props.room.roomCode,
      {
        quantity: consumeForm.value.quantity,
      },
      abortController.signal,
    )

    await authStore.fetchUserRooms()
    await fetchContainers()
    showNotification(`Recorded consumption of ${consumeForm.value.quantity} eggs!`)

    // Reset form
    consumeForm.value.quantity = 1
  } catch (err) {
    if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
    error.value = 'Failed to record consumption.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const openContainerDetail = (container) => {
  selectedContainer.value = container
  showDetailModal.value = true
}

const closeDetailModal = () => {
  showDetailModal.value = false
  selectedContainer.value = null
}

onMounted(fetchContainers)

onUnmounted(() => {
  abortController.abort()
})
</script>

<style scoped>
.dashboard-container {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}

.dashboard-header {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
  box-shadow: var(--shadow-sm);
}

.header-top {
  display: flex;
  margin-bottom: var(--spacing-md);
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.header-top .btn {
  width: auto;
  flex-shrink: 0;
}

.dashboard-header h2 {
  margin: 0;
  color: var(--text-primary);
  flex: 1;
  text-align: left;
}

.room-info {
  display: flex;
  justify-content: center;
  gap: var(--spacing-md);
  flex-wrap: wrap;
  margin-top: var(--spacing-sm);
}

.room-code {
  background: var(--color-primary-light);
  color: var(--color-secondary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm);
}

.room-stats {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.container-list {
  margin-bottom: var(--spacing-xl);
}

.container-list h3 {
  margin: 0 0 var(--spacing-md) 0;
  color: var(--text-primary);
}

.container-list ul {
  list-style: none;
  padding: 0;
}

.container-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-sm);
  background: var(--bg-primary);
}

.container-info {
  flex: 1;
}

.container-info strong {
  display: block;
  margin-bottom: var(--spacing-xs);
}

.owner-info {
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
  display: block;
  margin-top: var(--spacing-xs);
}

.container-actions {
  display: flex;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.container-actions button {
  padding: var(--spacing-sm) var(--spacing-md);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-size: var(--font-size-sm);
  white-space: nowrap;
}

.main-actions {
  margin-bottom: var(--spacing-xl);
}

.main-actions h3 {
  margin: 0 0 var(--spacing-md) 0;
  color: var(--text-primary);
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--spacing-md);
}

.action-card {
  background: var(--bg-primary);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
}

.action-card h4 {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--text-primary);
}

.action-card p {
  margin: 0 0 var(--spacing-md) 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.form-row {
  display: flex;
  gap: var(--spacing-sm);
  align-items: center;
}

.form-row select,
.form-row input {
  padding: var(--spacing-sm);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  flex: 1;
}

.form-row button {
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--color-primary);
  color: var(--text-inverse);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  white-space: nowrap;
}

.form-row button:disabled {
  background: var(--color-gray-400);
  cursor: not-allowed;
}

.form-group {
  margin-bottom: var(--spacing-md);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
}

.form-group label .required {
  color: var(--color-danger);
  font-weight: var(--font-weight-bold);
  margin-left: 2px;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: var(--spacing-sm);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  box-sizing: border-box;
}

.btn {
  padding: var(--spacing-sm) var(--spacing-md);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-weight: var(--font-weight-medium);
  width: 100%;
  margin-top: var(--spacing-sm);
}

.btn-success {
  background: var(--color-success);
  color: var(--text-inverse);
}

.btn-primary {
  background: var(--color-primary);
  color: var(--text-inverse);
}

.btn:disabled {
  background: var(--color-gray-400);
  cursor: not-allowed;
}

.detail-info p {
  margin: var(--spacing-sm) 0;
  color: var(--text-secondary);
}

.room-stats-summary {
  background: var(--bg-tertiary);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  margin-top: var(--spacing-md);
}

.room-stats-summary ul {
  margin: var(--spacing-sm) 0 0 0;
  padding-left: var(--spacing-lg);
}

.room-stats-summary li {
  margin: var(--spacing-xs) 0;
  color: var(--text-secondary);
}

.w-full {
  width: 100%;
}

@media (max-width: 768px) {
  .header-top {
    flex-direction: column;
    align-items: center;
  }

  .dashboard-header h2 {
    text-align: center;
  }

  .container-item {
    flex-direction: column;
    align-items: stretch;
    gap: var(--spacing-md);
  }

  .actions-grid {
    grid-template-columns: 1fr;
  }

  .form-row {
    flex-direction: column;
  }
}
</style>
