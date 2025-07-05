<template>
  <div class="dashboard-container">
    <div class="dashboard-header">
      <h2>{{ room.roomName }}</h2>
      <div v-if="containersLoading">Loading containers...</div>
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
              <label>Container Name</label>
              <input v-model="stockForm.containerName" type="text" placeholder="e.g., Fresh Eggs" />
            </div>
            <div class="form-group">
              <label>Quantity <span class="required">*</span></label>
              <input v-model.number="stockForm.quantity" type="number" min="1" required />
            </div>
            <div class="form-group">
              <label>Price per container <span class="required">*</span></label>
              <input v-model.number="stockForm.amount" type="number" step="0.01" min="0" required />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-success">
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
              <label>Quantity <span class="required">*</span></label>
              <input v-model.number="consumeForm.quantity" type="number" min="1" required />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-primary">
              {{ loading ? 'Recording...' : 'Record Consumption' }}
            </button>
          </form>
        </div>
      </div>
      <p v-if="error" class="error-message">{{ error }}</p>
    </div>

    <div class="container-list">
      <h3>Containers in this Room</h3>
      <div v-if="containersLoading">Loading containers...</div>
      <div v-else-if="containers.length === 0">No containers found. Add one above!</div>
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
            <button @click="openContainerDetail(container)" class="btn-details">
              View Details
            </button>
          </div>
        </li>
      </ul>
    </div>

    <!-- Container Detail Modal -->
    <div v-if="showDetailModal" class="detail-modal">
      <div class="detail-content">
        <div class="detail-header">
          <h3>{{ selectedContainer.containerName }} - Details</h3>
          <button @click="closeDetailModal" class="close-btn">×</button>
        </div>
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

    <!-- Notification -->
    <div v-if="notification" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import { containerService } from '@/services/container.service'
import { orderService } from '@/services/order.service'

const authStore = useAuthStore()
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
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem;
}

.dashboard-header {
  margin-bottom: 2rem;
  text-align: center;
}

.dashboard-header h2 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.room-info {
  display: flex;
  justify-content: center;
  gap: 1rem;
  flex-wrap: wrap;
  margin-top: 0.5rem;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.9rem;
}

.room-stats {
  color: #666;
  font-size: 0.9rem;
}

.add-container-form {
  background: #f9f9f9;
  padding: 1.5rem;
  border-radius: 8px;
  margin-bottom: 2rem;
}

.add-container-form h3 {
  margin: 0 0 1rem 0;
  color: #333;
}

.add-container-form form {
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}

.add-container-form input {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  flex: 1;
  min-width: 150px;
}

.add-container-form button {
  padding: 0.5rem 1rem;
  background: #4caf50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  white-space: nowrap;
}

.add-container-form button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.container-list {
  margin-bottom: 2rem;
}

.container-list h3 {
  margin: 0 0 1rem 0;
  color: #333;
}

.container-list ul {
  list-style: none;
  padding: 0;
}

.container-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  margin-bottom: 0.5rem;
  background: white;
}

.container-info {
  flex: 1;
}

.container-info strong {
  display: block;
  margin-bottom: 0.25rem;
}

.owner-info {
  color: #666;
  font-size: 0.8rem;
  display: block;
  margin-top: 0.25rem;
}

.container-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.container-actions button {
  padding: 0.5rem 0.75rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  white-space: nowrap;
}

.btn-details {
  background: #007bff;
  color: white;
}

.main-actions {
  margin-bottom: 2rem;
}

.main-actions h3 {
  margin: 0 0 1rem 0;
  color: #333;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1rem;
}

.action-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.action-card h4 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.action-card p {
  margin: 0 0 1rem 0;
  color: #666;
  font-size: 0.9rem;
}

.form-row {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.form-row select,
.form-row input {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  flex: 1;
}

.form-row button {
  padding: 0.5rem 1rem;
  background: #4caf50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  white-space: nowrap;
}

.form-row button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #333;
}

.form-group label .required {
  color: #f44336;
  font-weight: bold;
  margin-left: 2px;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  box-sizing: border-box;
}

.btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  width: 100%;
  margin-top: 0.5rem;
}

.btn-success {
  background: #4caf50;
  color: white;
}

.btn-primary {
  background: #007bff;
  color: white;
}

.btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.detail-modal {
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

.detail-content {
  background: white;
  border-radius: 8px;
  padding: 2rem;
  width: 90%;
  max-width: 400px;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.detail-header h3 {
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

.detail-info p {
  margin: 0.5rem 0;
  color: #555;
}

.error-message {
  color: #f44336;
  font-size: 0.9rem;
  margin-top: 0.5rem;
}

.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 1rem 1.5rem;
  border-radius: 6px;
  color: white;
  font-weight: 500;
  z-index: 1000;
}

.notification.success {
  background: #4caf50;
}

.notification.error {
  background: #f44336;
}

@media (max-width: 768px) {
  .add-container-form form {
    flex-direction: column;
    align-items: stretch;
  }

  .container-item {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }

  .actions-grid {
    grid-template-columns: 1fr;
  }

  .form-row {
    flex-direction: column;
  }
}
</style>
