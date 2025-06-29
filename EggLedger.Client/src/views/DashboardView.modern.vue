<template>
  <div class="dashboard-container">
    <!-- Header -->
    <div class="dashboard-header">
      <div class="room-info">
        <h1>{{ roomData?.roomName || 'Loading...' }}</h1>
        <div class="room-meta">
          <span class="room-code">Room: {{ roomCode }}</span>
          <span class="member-count">{{ memberCount }} members</span>
        </div>
      </div>
      <div class="header-actions">
        <button @click="showAddContainerModal = true" class="btn-primary">
          <svg class="icon" viewBox="0 0 24 24">
            <path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
          </svg>
          Add Container
        </button>
        <button @click="showRoomSettings = true" class="btn-secondary">
          <svg class="icon" viewBox="0 0 24 24">
            <path fill="currentColor" d="M12 15.5A3.5 3.5 0 0 1 8.5 12A3.5 3.5 0 0 1 12 8.5a3.5 3.5 0 0 1 3.5 3.5 3.5 3.5 0 0 1-3.5 3.5m7.43-2.53c.04-.32.07-.64.07-.97c0-.33-.03-.66-.07-1l2.11-1.63c.19-.15.24-.42.12-.64l-2-3.46c-.12-.22-.39-.31-.61-.22l-2.49 1c-.52-.39-1.06-.73-1.69-.98l-.37-2.65A.506.506 0 0 0 14 2h-4c-.25 0-.46.18-.5.42l-.37 2.65c-.63.25-1.17.59-1.69.98l-2.49-1c-.22-.09-.49 0-.61.22l-2 3.46c-.13.22-.07.49.12.64L4.57 11c-.04.34-.07.67-.07 1c0 .33.03.65.07.97l-2.11 1.66c-.19.15-.25.42-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1.01c.52.4 1.06.74 1.69.99l.37 2.65c.04.24.25.42.5.42h4c.25 0 .46-.18.5-.42l.37-2.65c.63-.26 1.17-.59 1.69-.99l2.49 1.01c.22.08.49 0 .61-.22l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.66Z"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">📦</div>
        <div class="stat-content">
          <h3>{{ totalContainers }}</h3>
          <p>Total Containers</p>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">🥚</div>
        <div class="stat-content">
          <h3>{{ totalItems }}</h3>
          <p>Items Available</p>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">💰</div>
        <div class="stat-content">
          <h3>${{ totalValue }}</h3>
          <p>Total Value</p>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">⚡</div>
        <div class="stat-content">
          <h3>{{ recentActivity }}</h3>
          <p>Recent Activity</p>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="main-content">
      <!-- Containers Section -->
      <div class="content-section">
        <div class="section-header">
          <h2>
            <svg class="section-icon" viewBox="0 0 24 24">
              <path fill="currentColor" d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2Z"/>
            </svg>
            Active Containers
          </h2>
          <div class="section-actions">
            <button @click="refreshContainers" class="btn-refresh">
              <svg class="icon" viewBox="0 0 24 24">
                <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
              </svg>
            </button>
          </div>
        </div>

        <div v-if="loadingContainers" class="loading-state">
          <div class="spinner"></div>
          <p>Loading containers...</p>
        </div>

        <div v-else-if="containers.length === 0" class="empty-state">
          <div class="empty-icon">📦</div>
          <h3>No containers yet</h3>
          <p>Add your first container to start tracking items!</p>
          <button @click="showAddContainerModal = true" class="btn-primary">Add Container</button>
        </div>

        <div v-else class="containers-grid">
          <div
            v-for="container in containers"
            :key="container.containerId"
            class="container-card"
            @click="viewContainer(container)"
          >
            <div class="container-header">
              <h3>{{ container.containerName }}</h3>
              <div class="container-progress">
                <div class="progress-bar">
                  <div
                    class="progress-fill"
                    :style="{ width: (container.remainingQuantity / container.totalQuantity * 100) + '%' }"
                  ></div>
                </div>
                <span class="progress-text">
                  {{ container.remainingQuantity }}/{{ container.totalQuantity }}
                </span>
              </div>
            </div>

            <div class="container-meta">
              <div class="meta-item">
                <span class="meta-label">Owner:</span>
                <span class="meta-value">{{ container.buyerName }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">Value:</span>
                <span class="meta-value">${{ container.amount }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">Per item:</span>
                <span class="meta-value">${{ container.price }}</span>
              </div>
            </div>

            <div class="container-actions">
              <button @click.stop="consumeItem(container)" class="btn-consume">
                <svg class="icon" viewBox="0 0 24 24">
                  <path fill="currentColor" d="M11 9h2V6h3V4h-3V1h-2v3H8v2h3v3zm-4 9c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zm10 0c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2zm-9.83-3.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03L21.7 4H5.21l-.94-2H1v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.13 0-.25-.11-.25-.25z"/>
                </svg>
                Use Item
              </button>
              <button @click.stop="viewContainer(container)" class="btn-view">
                View Details
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Activity Section -->
      <div class="content-section">
        <div class="section-header">
          <h2>
            <svg class="section-icon" viewBox="0 0 24 24">
              <path fill="currentColor" d="M13 3c-4.97 0-9 4.03-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42C8.27 19.99 10.51 21 13 21c4.97 0 9-4.03 9-9s-4.03-9-9-9zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8H12z"/>
            </svg>
            Recent Activity
          </h2>
        </div>

        <div v-if="recentOrders.length === 0" class="empty-state small">
          <p>No recent activity</p>
        </div>

        <div v-else class="activity-list">
          <div
            v-for="order in recentOrders.slice(0, 5)"
            :key="order.orderId"
            class="activity-item"
          >
            <div class="activity-icon" :class="order.orderType.toLowerCase()">
              <svg v-if="order.orderType === 'Stock'" viewBox="0 0 24 24">
                <path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
              </svg>
              <svg v-else viewBox="0 0 24 24">
                <path fill="currentColor" d="M19 7H5v10h14V7z"/>
              </svg>
            </div>
            <div class="activity-content">
              <p class="activity-title">
                {{ order.orderName }}
              </p>
              <p class="activity-meta">
                {{ order.orderType }} • {{ formatDate(order.datestamp) }} • ${{ order.amount }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Container Modal -->
    <div v-if="showAddContainerModal" class="modal-overlay" @click.self="closeModals">
      <div class="modal">
        <div class="modal-header">
          <h3>Add New Container</h3>
          <button @click="closeModals" class="btn-close">×</button>
        </div>
        <form @submit.prevent="addContainer" class="modal-form">
          <div class="form-row">
            <div class="form-group">
              <label for="containerName">Container Name</label>
              <input
                type="text"
                id="containerName"
                v-model="newContainer.containerName"
                class="form-input"
                placeholder="e.g., Organic Eggs"
                required
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="totalQuantity">Total Quantity</label>
              <input
                type="number"
                id="totalQuantity"
                v-model="newContainer.totalQuantity"
                class="form-input"
                placeholder="12"
                min="1"
                required
              />
            </div>
            <div class="form-group">
              <label for="amount">Total Amount ($)</label>
              <input
                type="number"
                id="amount"
                v-model="newContainer.amount"
                class="form-input"
                placeholder="6.99"
                step="0.01"
                min="0"
                required
              />
            </div>
          </div>

          <div class="calculated-price" v-if="newContainer.totalQuantity && newContainer.amount">
            <p>Price per item: <strong>${{ (newContainer.amount / newContainer.totalQuantity).toFixed(2) }}</strong></p>
          </div>

          <div class="modal-actions">
            <button type="button" @click="closeModals" class="btn-secondary">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="addingContainer">
              <span v-if="addingContainer" class="spinner"></span>
              {{ addingContainer ? 'Adding...' : 'Add Container' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Error/Success Toast -->
    <div v-if="toast.show" class="toast" :class="toast.type">
      <span>{{ toast.message }}</span>
      <button @click="hideToast" class="toast-close">×</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import containerService from '@/services/container.service'
import orderService from '@/services/order.service'
import roomService from '@/services/room.service'

const route = useRoute()
const router = useRouter()
const roomCode = ref(parseInt(route.params.roomCode))

// Reactive data
const roomData = ref(null)
const containers = ref([])
const recentOrders = ref([])
const memberCount = ref(0)
const loadingContainers = ref(false)

// Modal states
const showAddContainerModal = ref(false)
const showRoomSettings = ref(false)

// Form data
const newContainer = ref({
  containerName: '',
  totalQuantity: '',
  amount: '',
  buyerId: '' // Will be set from auth
})

// Loading states
const addingContainer = ref(false)

// Toast
const toast = ref({
  show: false,
  type: 'success',
  message: ''
})

// Computed properties
const totalContainers = computed(() => containers.value.length)
const totalItems = computed(() =>
  containers.value.reduce((sum, c) => sum + c.remainingQuantity, 0)
)
const totalValue = computed(() =>
  containers.value.reduce((sum, c) => sum + c.amount, 0).toFixed(2)
)
const recentActivity = computed(() => recentOrders.value.length)

// Methods
const loadRoomData = async () => {
  try {
    // Load room users to get member count
    const users = await roomService.getAllRoomUsers(roomCode.value)
    memberCount.value = users.length
  } catch (err) {
    console.error('Error loading room data:', err)
  }
}

const loadContainers = async () => {
  loadingContainers.value = true
  try {
    const data = await containerService.getContainers(roomCode.value)
    containers.value = data
  } catch (err) {
    showToast('Failed to load containers', 'error')
    console.error('Error loading containers:', err)
  } finally {
    loadingContainers.value = false
  }
}

const loadRecentOrders = async () => {
  try {
    // Get orders for all containers (simplified)
    const allOrders = []
    for (const container of containers.value) {
      const orders = await orderService.getOrdersByContainer(roomCode.value, container.containerId)
      allOrders.push(...orders)
    }
    recentOrders.value = allOrders.sort((a, b) => new Date(b.datestamp) - new Date(a.datestamp))
  } catch (err) {
    console.error('Error loading recent orders:', err)
  }
}

const refreshContainers = () => {
  loadContainers()
  loadRecentOrders()
}

const addContainer = async () => {
  addingContainer.value = true
  try {
    // TODO: Get user ID from auth store
    const userId = 'current-user-id'
    const containerData = {
      ...newContainer.value,
      buyerId: userId,
      totalQuantity: parseInt(newContainer.value.totalQuantity),
      amount: parseFloat(newContainer.value.amount)
    }

    await containerService.createContainer(roomCode.value, containerData)
    showToast('Container added successfully!', 'success')
    closeModals()
    resetContainerForm()
    await loadContainers()
  } catch (err) {
    showToast('Failed to add container', 'error')
    console.error('Error adding container:', err)
  } finally {
    addingContainer.value = false
  }
}

const consumeItem = async (container) => {
  try {
    const orderData = {
      containerId: container.containerId,
      quantity: 1,
      orderName: `Consumed 1 ${container.containerName}`
    }

    await orderService.consumeOrder(roomCode.value, orderData)
    showToast('Item consumed successfully!', 'success')
    await loadContainers()
    await loadRecentOrders()
  } catch (err) {
    showToast('Failed to consume item', 'error')
    console.error('Error consuming item:', err)
  }
}

const viewContainer = (container) => {
  router.push(`/room/${roomCode.value}/container/${container.containerId}`)
}

const closeModals = () => {
  showAddContainerModal.value = false
  showRoomSettings.value = false
}

const resetContainerForm = () => {
  newContainer.value = {
    containerName: '',
    totalQuantity: '',
    amount: '',
    buyerId: ''
  }
}

const showToast = (message, type = 'success') => {
  toast.value = {
    show: true,
    message,
    type
  }
  setTimeout(() => {
    hideToast()
  }, 5000)
}

const hideToast = () => {
  toast.value.show = false
}

const formatDate = (dateString) => {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Lifecycle
onMounted(async () => {
  await loadRoomData()
  await loadContainers()
  await loadRecentOrders()
})
</script>

<style scoped>
.dashboard-container {
  min-height: 100vh;
  background: #f8fafc;
  padding: 2rem;
}

.dashboard-header {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  margin-bottom: 2rem;
  box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.room-info h1 {
  margin: 0 0 0.5rem 0;
  color: #1a202c;
  font-size: 2rem;
  font-weight: 700;
}

.room-meta {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.room-code {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  font-weight: 600;
  font-size: 0.875rem;
}

.member-count {
  color: #718096;
  font-size: 0.875rem;
}

.header-actions {
  display: flex;
  gap: 1rem;
}

.btn-primary, .btn-secondary {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  border: none;
  text-decoration: none;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(102, 126, 234, 0.3);
}

.btn-secondary {
  background: #e2e8f0;
  color: #4a5568;
}

.btn-secondary:hover {
  background: #cbd5e0;
}

.icon {
  width: 20px;
  height: 20px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.stat-icon {
  font-size: 3rem;
  opacity: 0.8;
}

.stat-content h3 {
  margin: 0 0 0.25rem 0;
  color: #1a202c;
  font-size: 2rem;
  font-weight: 700;
}

.stat-content p {
  margin: 0;
  color: #718096;
  font-size: 0.875rem;
}

.main-content {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 2rem;
}

.content-section {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 4px 20px rgba(0,0,0,0.08);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.section-header h2 {
  margin: 0;
  color: #1a202c;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.5rem;
}

.section-icon {
  width: 24px;
  height: 24px;
  color: #667eea;
}

.btn-refresh {
  background: none;
  border: 2px solid #e2e8f0;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.btn-refresh:hover {
  border-color: #667eea;
  color: #667eea;
}

.loading-state, .empty-state {
  text-align: center;
  padding: 3rem 2rem;
}

.empty-state.small {
  padding: 2rem 1rem;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e2e8f0;
  border-top: 3px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem auto;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty-icon {
  font-size: 4rem;
  opacity: 0.5;
  margin-bottom: 1rem;
}

.empty-state h3 {
  color: #1a202c;
  margin: 0 0 0.5rem 0;
}

.empty-state p {
  color: #718096;
  margin: 0 0 1.5rem 0;
}

.containers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1.5rem;
}

.container-card {
  background: #f7fafc;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.container-card:hover {
  transform: translateY(-5px);
  border-color: #667eea;
  box-shadow: 0 10px 25px rgba(102, 126, 234, 0.15);
}

.container-header {
  margin-bottom: 1rem;
}

.container-header h3 {
  margin: 0 0 1rem 0;
  color: #1a202c;
  font-size: 1.25rem;
}

.container-progress {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.progress-bar {
  flex: 1;
  height: 8px;
  background: #e2e8f0;
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(135deg, #48bb78 0%, #38a169 100%);
  transition: width 0.3s ease;
}

.progress-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: #4a5568;
  min-width: 60px;
}

.container-meta {
  margin: 1.5rem 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.meta-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.875rem;
}

.meta-label {
  color: #718096;
}

.meta-value {
  color: #1a202c;
  font-weight: 600;
}

.container-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1.5rem;
}

.btn-consume, .btn-view {
  flex: 1;
  padding: 0.75rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-consume {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  color: white;
}

.btn-consume:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(240, 147, 251, 0.3);
}

.btn-view {
  background: #e2e8f0;
  color: #4a5568;
}

.btn-view:hover {
  background: #cbd5e0;
}

.activity-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #f7fafc;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

.activity-icon {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.activity-icon.stock {
  background: linear-gradient(135deg, #48bb78 0%, #38a169 100%);
  color: white;
}

.activity-icon.consume {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  color: white;
}

.activity-icon svg {
  width: 20px;
  height: 20px;
}

.activity-content {
  flex: 1;
}

.activity-title {
  margin: 0 0 0.25rem 0;
  color: #1a202c;
  font-weight: 600;
  font-size: 0.875rem;
}

.activity-meta {
  margin: 0;
  color: #718096;
  font-size: 0.75rem;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 600px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  padding: 2rem 2rem 0 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  color: #1a202c;
  font-size: 1.5rem;
}

.btn-close {
  background: none;
  border: none;
  font-size: 2rem;
  cursor: pointer;
  color: #718096;
  padding: 0;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-close:hover {
  color: #1a202c;
}

.modal-form {
  padding: 2rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.form-row:last-of-type {
  margin-bottom: 0;
}

.form-row .form-group:only-child {
  grid-column: 1 / -1;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 600;
  color: #374151;
  font-size: 0.875rem;
}

.form-input {
  padding: 0.875rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.calculated-price {
  background: #f7fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1.5rem;
  text-align: center;
}

.calculated-price p {
  margin: 0;
  color: #4a5568;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.toast {
  position: fixed;
  top: 2rem;
  right: 2rem;
  padding: 1rem 1.5rem;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 1rem;
  z-index: 1001;
  animation: slideIn 0.3s ease;
  color: white;
  font-weight: 600;
}

.toast.success {
  background: linear-gradient(135deg, #48bb78 0%, #38a169 100%);
}

.toast.error {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.toast-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: currentColor;
  padding: 0;
}

@media (max-width: 1024px) {
  .main-content {
    grid-template-columns: 1fr;
  }

  .dashboard-header {
    flex-direction: column;
    gap: 1.5rem;
    text-align: center;
  }

  .header-actions {
    width: 100%;
    justify-content: center;
  }
}

@media (max-width: 768px) {
  .dashboard-container {
    padding: 1rem;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }

  .containers-grid {
    grid-template-columns: 1fr;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .container-actions {
    flex-direction: column;
  }

  .toast {
    left: 1rem;
    right: 1rem;
    top: 1rem;
  }
}
</style>
