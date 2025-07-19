<template>
  <div class="container-detail-view">
    <NavigationHeader />
    <RoomIndicator />

    <div class="container-detail-container">
      <div class="container-header">
        <div class="header-content">
          <div class="container-title">
            <h2>{{ containerInfo?.containerName || 'Container Details' }}</h2>
            <div class="breadcrumb">
              <router-link to="/profile">Profile</router-link>
              <span class="separator">></span>
              <span>Container Details</span>
            </div>
          </div>
          <button @click="goBack" class="btn btn-secondary">← Back to Profile</button>
        </div>
      </div>

      <div v-if="loading" class="card text-center p-5">
        <p class="text-secondary">Loading container details...</p>
      </div>
      <div v-if="error" class="alert alert-error">{{ error }}</div>

      <!-- Show orders even if container info is not available -->
      <div v-if="!loading && !error" class="container-content">
        <!-- Container Info Section - only show if container info is available -->
        <div v-if="containerInfo" class="info-section">
          <h3>Container Information</h3>
          <div class="container-info-grid">
            <div class="info-item">
              <label>Container Name</label>
              <span>{{ containerInfo.containerName }}</span>
            </div>
            <div class="info-item">
              <label>Owner</label>
              <span>{{ containerInfo.buyerName }}</span>
            </div>
            <div class="info-item">
              <label>Container ID</label>
              <span>{{ containerInfo.containerId }}</span>
            </div>
            <div class="info-item">
              <label>Total Capacity</label>
              <span>{{ containerInfo.totalQuantity || 0 }} eggs</span>
            </div>
            <div class="info-item">
              <label>Current Stock</label>
              <span>{{ containerInfo.remainingQuantity || 0 }} eggs</span>
            </div>
            <div class="info-item">
              <label>Created Date</label>
              <span>{{ formatDate(containerInfo.purchaseDateTime) }}</span>
            </div>
          </div>
        </div>

        <div class="orders-section">
          <h3>Order History</h3>
          <div v-if="loadingOrders" class="card text-center p-5">
            <p class="text-secondary">Loading orders...</p>
          </div>
          <div v-else-if="orders.length === 0" class="card text-center p-5">
            <p class="text-secondary">No orders found for this container.</p>
          </div>
          <div v-else class="orders-list">
            <div v-for="order in orders" :key="order.orderId" class="order-item">
              <div class="order-header">
                <div class="order-type" :class="getOrderTypeClass(order.orderType)">
                  {{ getOrderTypeDisplay(order.orderType) }}
                </div>
                <div class="order-date">{{ formatDateTime(order.datestamp) }}</div>
              </div>
              <div class="order-details">
                <div class="order-info">
                  <div class="order-name">{{ order.orderName }}</div>
                  <div class="order-metrics">
                    <span class="quantity">
                      {{ getOrderTypeSign(order.orderType) }}{{ getContainerQuantity(order) }} eggs
                    </span>
                    <span v-if="order.amount > 0" class="amount"
                      >₹{{ order.amount.toFixed(2) }}</span
                    >
                  </div>
                </div>
                <div class="order-status">
                  Status: {{ getOrderStatusDisplay(order.orderStatus) }}
                </div>
              </div>
              <div
                v-if="order.orderDetails && order.orderDetails.length > 0"
                class="order-detail-info"
              >
                <div
                  v-for="detail in order.orderDetails.filter(
                    (d) => d.containerId === (containerInfo?.containerId || props.containerId),
                  )"
                  :key="detail.orderDetailId"
                  class="detail-item"
                >
                  <span v-if="detail.detailQuantity > 0">
                    Quantity: {{ detail.detailQuantity }} | Price: ₹{{ detail.price.toFixed(2) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import { orderService } from '@/services/order.service'

const props = defineProps({
  containerId: {
    type: String,
    required: true,
  },
})

const router = useRouter()

// Get container info from sessionStorage if available
const containerInfo = computed(() => {
  const stored = sessionStorage.getItem('currentContainerInfo')
  return stored ? JSON.parse(stored) : null
})

const loading = ref(false) // No longer loading container info
const loadingOrders = ref(true)
const error = ref(null)
const orders = ref([])
const selectedRoomCode = computed(() => {
  return sessionStorage.getItem('selectedRoomCode')
})

let abortController = new AbortController()

// Fetch orders for this specific container
const fetchContainerOrders = async () => {
  try {
    loadingOrders.value = true

    // Use containerId from containerInfo first, then fallback to props
    const containerId = containerInfo.value?.containerId || props.containerId

    if (!containerId) {
      error.value = 'Container ID not available.'
      return null
    }

    const response = await orderService.getOrdersByContainer(
      selectedRoomCode.value,
      containerId,
      abortController.signal,
    )

    return response.data
  } catch (err) {
    if (err.name === 'AbortError') return null

    console.error('Error fetching container orders:', err)

    if (err.response?.status === 404) {
      error.value = 'Container not found.'
    } else if (err.response?.status === 403) {
      error.value = 'You do not have permission to view this container.'
    } else {
      error.value = 'Failed to load container orders.'
    }
    return null
  } finally {
    loadingOrders.value = false
  }
}

// Helper functions for order display
const getOrderTypeDisplay = (orderType) => {
  switch (orderType) {
    case 1:
      return '📦 Stock Added'
    case 2:
      return '🍳 Consumed'
    default:
      return 'Unknown'
  }
}

const getOrderTypeClass = (orderType) => {
  switch (orderType) {
    case 1:
      return 'stock'
    case 2:
      return 'consume'
    default:
      return 'unknown'
  }
}

const getOrderTypeSign = (orderType) => {
  switch (orderType) {
    case 1:
      return '+'
    case 2:
      return '-'
    default:
      return ''
  }
}

const getContainerQuantity = (order) => {
  if (!order.orderDetails || order.orderDetails.length === 0) {
    return order.quantity || 0
  }

  const containerDetail = order.orderDetails.find(
    (d) => d.containerId === (containerInfo.value?.containerId || props.containerId),
  )
  return containerDetail ? containerDetail.detailQuantity : 0
}

const getOrderStatusDisplay = (orderStatus) => {
  switch (orderStatus) {
    case 1:
      return 'Active'
    case 2:
      return 'Completed'
    case 3:
      return 'Cancelled'
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

const formatDateTime = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleString()
  } catch {
    return 'Unknown'
  }
}

const goBack = () => {
  router.push('/profile')
}

onMounted(async () => {
  const ordersData = await fetchContainerOrders()
  if (ordersData) {
    orders.value = ordersData
  }
})

onUnmounted(() => {
  abortController.abort()
})
</script>

<style scoped>
.container-detail-view {
  min-height: 100vh;
  background: var(--bg-secondary);
}

.container-detail-container {
  max-width: var(--container-max-width);
  margin: 0 auto;
  padding: var(--spacing-xl);
}

.container-header {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
  box-shadow: var(--shadow-sm);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--spacing-lg);
}

.container-title h2 {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--text-primary);
}

.breadcrumb {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.breadcrumb a {
  color: var(--color-primary);
  text-decoration: none;
}

.breadcrumb a:hover {
  text-decoration: underline;
}

.separator {
  margin: 0 var(--spacing-sm);
}

.container-content {
  display: grid;
  gap: var(--spacing-xl);
}

.info-section,
.orders-section {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
}

.info-section h3,
.orders-section h3 {
  margin: 0 0 var(--spacing-lg) 0;
  color: var(--text-primary);
  border-bottom: 2px solid var(--border-light);
  padding-bottom: var(--spacing-sm);
}

.container-info-grid {
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
  padding: var(--spacing-sm);
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.order-item {
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
  padding: var(--spacing-md);
  transition: box-shadow var(--transition-normal);
}

.order-item:hover {
  box-shadow: var(--shadow-sm);
}

.order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.order-type {
  font-weight: var(--font-weight-semibold);
  padding: var(--spacing-xs) var(--spacing-md);
  border-radius: 20px;
  font-size: var(--font-size-sm);
}

.order-type.stock {
  background: var(--color-primary-light);
  color: var(--color-success);
}

.order-type.consume {
  background: #fed7d7;
  color: var(--color-danger);
}

.order-type.unknown {
  background: var(--bg-tertiary);
  color: var(--text-muted);
}

.order-date {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.order-details {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-sm);
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.order-name {
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  font-size: var(--font-size-base);
}

.order-metrics {
  display: flex;
  gap: var(--spacing-md);
  align-items: center;
}

.quantity {
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-lg);
}

.amount {
  color: var(--color-success);
  font-weight: var(--font-weight-medium);
}

.order-status {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.order-detail-info {
  margin-top: var(--spacing-sm);
  padding-top: var(--spacing-sm);
  border-top: 1px solid var(--border-light);
}

.detail-item {
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
}

@media (max-width: 768px) {
  .container-detail-container {
    padding: var(--spacing-md);
  }

  .header-content {
    flex-direction: column;
    gap: var(--spacing-md);
  }

  .container-info-grid {
    grid-template-columns: 1fr;
  }

  .order-details {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-sm);
  }

  .order-metrics {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }
}
</style>
