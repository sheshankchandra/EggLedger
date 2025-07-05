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

      <div v-if="loading" class="loading">Loading container details...</div>
      <div v-if="error" class="error-message">{{ error }}</div>

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
          <div v-if="loadingOrders" class="loading">Loading orders...</div>
          <div v-else-if="orders.length === 0" class="no-orders">
            <p>No orders found for this container.</p>
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
import NavigationHeader from '@/components/NavigationHeader.vue'
import RoomIndicator from '@/components/RoomIndicator.vue'
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
  return orderType === 1 ? '+' : '-'
}

const getOrderStatusDisplay = (orderStatus) => {
  switch (orderStatus) {
    case 100:
      return 'Completed'
    case 101:
      return 'Processing'
    default:
      return 'Unknown'
  }
}

const getContainerQuantity = (order) => {
  // For this specific container, get the quantity from order details
  // Use containerId from containerInfo first, then fallback to props
  const containerId = containerInfo.value?.containerId || props.containerId
  const containerDetail = order.orderDetails?.find((detail) => detail.containerId === containerId)
  return containerDetail?.detailQuantity || order.quantity || 0
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
  router.back()
}

onMounted(async () => {
  error.value = null

  try {
    const ordersData = await fetchContainerOrders()
    if (ordersData) {
      orders.value = ordersData
    }
  } catch (err) {
    if (err.name === 'AbortError') return
    error.value = 'Failed to load container orders.'
    console.error('Error loading orders:', err)
  }
})

onUnmounted(() => {
  // Clean up the stored container info when leaving the page
  sessionStorage.removeItem('currentContainerInfo')
  abortController.abort()
})
</script>
<style scoped>
.container-detail-view {
  min-height: 100vh;
  background: #f5f5f5;
}

.container-detail-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.container-header {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 2rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.container-title h2 {
  margin: 0 0 0.5rem 0;
  color: #333;
  font-size: 1.8rem;
}

.breadcrumb {
  color: #666;
  font-size: 0.9rem;
}

.breadcrumb a {
  color: #4caf50;
  text-decoration: none;
}

.breadcrumb a:hover {
  text-decoration: underline;
}

.separator {
  margin: 0 0.5rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  border: none;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s ease;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #5a6268;
}

.loading {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.error-message {
  background: #f8d7da;
  color: #721c24;
  padding: 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
}

.container-content {
  display: grid;
  gap: 2rem;
}

.info-section,
.orders-section {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.info-section h3,
.orders-section h3 {
  margin: 0 0 1.5rem 0;
  color: #333;
  border-bottom: 2px solid #e9ecef;
  padding-bottom: 0.5rem;
}

.container-info-grid {
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
  padding: 0.5rem;
  background: #f8f9fa;
  border-radius: 4px;
}

.no-container,
.no-orders {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.order-item {
  border: 1px solid #e9ecef;
  border-radius: 6px;
  padding: 1rem;
  transition: box-shadow 0.2s ease;
}

.order-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.order-type {
  font-weight: 600;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.9rem;
}

.order-type.stock {
  background: #d4edda;
  color: #155724;
}

.order-type.consume {
  background: #f8d7da;
  color: #721c24;
}

.order-type.unknown {
  background: #f8f9fa;
  color: #6c757d;
}

.order-date {
  color: #666;
  font-size: 0.9rem;
}

.order-details {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 0.5rem;
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.order-name {
  font-weight: 600;
  color: #333;
  font-size: 1rem;
}

.order-metrics {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.quantity {
  font-weight: 600;
  font-size: 1.1rem;
}

.amount {
  color: #28a745;
  font-weight: 500;
}

.order-status {
  color: #666;
  font-size: 0.9rem;
}

.order-detail-info {
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid #e9ecef;
}

.detail-item {
  color: #666;
  font-size: 0.85rem;
}

@media (max-width: 768px) {
  .container-detail-container {
    padding: 1rem;
  }

  .header-content {
    flex-direction: column;
    gap: 1rem;
  }

  .container-info-grid {
    grid-template-columns: 1fr;
  }

  .transaction-header,
  .order-details {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .order-metrics {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
}
</style>
