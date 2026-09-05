<template>
  <div class="container-detail-view">
    <NavigationHeader />
    <RoomIndicator />

    <main class="page-shell detail-shell">
      <header
        class="detail-header"
        :class="{ 'detail-header-inactive': lifecycleState !== 'active' }"
      >
        <div>
          <div class="breadcrumb">
            <router-link to="/profile">Profile</router-link>
            <span aria-hidden="true">/</span>
            <span>{{ resource.inventorySingular }} details</span>
          </div>
          <div class="title-row">
            <h1>{{ containerInfo?.containerName || `Untitled ${resource.inventorySingular}` }}</h1>
            <span
              v-if="lifecycleState"
              class="lifecycle-badge"
              :class="`lifecycle-${lifecycleState}`"
            >
              {{ lifecycleLabel }}
            </span>
          </div>
          <p v-if="containerInfo?.deletedAt" class="lifecycle-meta">
            {{ lifecycleState === 'suspended' ? 'Suspended' : 'Archived' }} on
            {{ formatDate(containerInfo.deletedAt) }}
            <template v-if="containerInfo.deletionReason">
              · {{ containerInfo.deletionReason }}</template
            >
          </p>
        </div>
        <button type="button" @click="goBack" class="btn btn-secondary">
          <ArrowLeft :size="16" aria-hidden="true" /> Back to profile
        </button>
      </header>

      <LoadingSkeleton v-if="loading" :count="1" height="160px" aria-label="Loading container" />
      <div v-if="error" class="alert alert-error">{{ error }}</div>

      <template v-if="!loading && !error">
        <!-- Container Info Section - only show if container info is available -->
        <section v-if="containerInfo" class="summary-grid" aria-label="Container summary">
          <div
            class="summary-card"
            :class="lifecycleState === 'active' ? 'summary-card-primary' : 'summary-card-muted'"
          >
            <span>Current stock</span>
            <strong>{{ containerInfo.remainingQuantity || 0 }}</strong>
            <small>of {{ containerInfo.totalQuantity || 0 }} {{ resource.plural }}</small>
          </div>
          <div class="summary-card">
            <span>Owner</span>
            <strong class="summary-text">{{ containerInfo.buyerName }}</strong>
            <small>Purchased this {{ resource.inventorySingular }}</small>
          </div>
          <div class="summary-card">
            <span>Purchased</span>
            <strong class="summary-text">{{ formatDate(containerInfo.purchaseDateTime) }}</strong>
            <small>Creation date</small>
          </div>
          <div class="summary-card">
            <span>{{ resource.inventorySingular }} ID</span>
            <strong class="summary-code" :title="containerInfo.containerId">
              {{ containerInfo.containerId }}
            </strong>
            <small>Reference code</small>
          </div>
        </section>

        <section class="orders-section" aria-labelledby="orders-heading">
          <div class="section-heading">
            <div>
              <p class="eyebrow">Activity</p>
              <h2 id="orders-heading">Order history</h2>
            </div>
          </div>

          <LoadingSkeleton
            v-if="loadingOrders"
            :count="3"
            height="110px"
            aria-label="Loading orders"
          />
          <EmptyState
            v-else-if="orders.length === 0"
            :icon="resource.icon"
            title="No orders yet"
            :description="`Stock or usage updates for this ${resource.inventorySingular} will show up here.`"
          />
          <ul v-else class="orders-list">
            <li v-for="order in orders" :key="order.orderId" class="order-item">
              <div class="order-header">
                <span class="order-type" :class="getOrderTypeClass(order.orderType)">
                  {{ getOrderTypeDisplay(order.orderType) }}
                </span>
                <span class="order-date">{{ formatDateTime(order.datestamp) }}</span>
              </div>

              <div class="order-details">
                <div class="order-info">
                  <span class="order-name">{{ order.orderName }}</span>
                  <span class="order-metrics">
                    <b
                      >{{ getOrderTypeSign(order.orderType) }}{{ getContainerQuantity(order) }}
                      {{ resource.plural }}</b
                    >
                    <span v-if="order.amount > 0" class="amount">
                      ₹{{ getContainerAmount(order).toFixed(2) }}
                    </span>
                  </span>
                </div>
                <span class="order-status">{{ getOrderStatusDisplay(order.orderStatus) }}</span>
              </div>

              <div
                v-if="order.orderDetails && order.orderDetails.length > 0"
                class="order-detail-info"
              >
                <span
                  v-for="detail in order.orderDetails.filter(
                    (d) => d.containerId === (containerInfo?.containerId || props.containerId),
                  )"
                  :key="detail.orderDetailId"
                  class="detail-item"
                >
                  <template v-if="detail.detailQuantity > 0">
                    Quantity: {{ detail.detailQuantity }} | Price: ₹{{ detail.price.toFixed(2) }}
                  </template>
                </span>
              </div>
            </li>
          </ul>
        </section>
      </template>
    </main>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowLeft } from '@lucide/vue'
import { useRoomStore } from '@/stores/room.store'
import { resourceConfig as resource } from '@/config/resource.config'
import NavigationHeader from '@/components/common/NavigationHeader.vue'
import RoomIndicator from '@/components/room/RoomIndicator.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'
import { orderService } from '@/services/order.service'

const props = defineProps({
  containerId: {
    type: String,
    required: true,
  },
})

const router = useRouter()
const roomStore = useRoomStore()

// Get container info from sessionStorage if available
const containerInfo = computed(() => {
  const stored = sessionStorage.getItem('currentContainerInfo')
  return stored ? JSON.parse(stored) : null
})

// Mirrors EggLedger.Models.Enums.ContainerStatus - enums serialize as their numeric value.
// Note: consumption never actually flips Status to "Depleted" (only RemainingQuantity drops),
// so "fully consumed" must be derived from quantity, not the Status field, for anything that
// isn't explicitly Archived/Suspended by an admin action.
const CONTAINER_STATUS = { ARCHIVED: 3, SUSPENDED: 4 }

const lifecycleState = computed(() => {
  const info = containerInfo.value
  if (!info) return null
  if (info.status === CONTAINER_STATUS.ARCHIVED) return 'archived'
  if (info.status === CONTAINER_STATUS.SUSPENDED) return 'suspended'
  if ((info.remainingQuantity ?? 0) <= 0) return 'consumed'
  return 'active'
})

const lifecycleLabel = computed(() => {
  switch (lifecycleState.value) {
    case 'archived':
      return 'Archived'
    case 'suspended':
      return 'Suspended'
    case 'consumed':
      return 'Fully consumed'
    case 'active':
      return 'Active'
    default:
      return ''
  }
})

const loading = ref(false) // No longer loading container info
const loadingOrders = ref(true)
const error = ref(null)
const orders = ref([])
const selectedRoomCode = computed(() => roomStore.selectedRoomCode)

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
      return 'Stock Added'
    case 2:
      return 'Consumed'
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

const getContainerAmount = (order) => {
  if (!order.orderDetails || order.orderDetails.length === 0) {
    return order.amount || 0
  }

  const containerDetail = order.orderDetails.find(
    (d) => d.containerId === (containerInfo.value?.containerId || props.containerId),
  )
  return containerDetail ? containerDetail.price * containerDetail.detailQuantity : 0
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

.detail-shell {
  display: grid;
  gap: var(--spacing-2xl);
}

.detail-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, var(--bg-primary), var(--bg-tertiary));
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-md);
  transition: border-color var(--transition-normal);
}

.detail-header-inactive {
  border-left: 4px solid var(--border-dark);
}

.detail-header h1 {
  margin: 0;
  font-size: clamp(1.5rem, 4vw, 2.25rem);
  letter-spacing: -0.03em;
}

.title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--spacing-md);
  margin-top: var(--spacing-sm);
}

.lifecycle-badge {
  padding: var(--spacing-xs) var(--spacing-md);
  border-radius: 999px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.lifecycle-active {
  background: var(--color-success-light);
  color: var(--color-success);
}

.lifecycle-consumed {
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  border: 1px solid var(--border-medium);
}

.lifecycle-archived {
  background: var(--color-danger-light);
  color: var(--color-danger);
}

.lifecycle-suspended {
  background: var(--color-warning-light);
  color: var(--color-warning);
}

.lifecycle-meta {
  margin: var(--spacing-sm) 0 0;
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.breadcrumb {
  display: flex;
  gap: var(--spacing-sm);
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.breadcrumb a {
  color: var(--color-primary);
  text-decoration: none;
}

.breadcrumb a:hover {
  text-decoration: underline;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-sm);
}

.summary-card {
  min-width: 0;
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--bg-primary) 72%, transparent);
}

.summary-card > span,
.summary-card small {
  display: block;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.summary-card strong {
  display: block;
  margin-block: var(--spacing-xs);
  font-size: var(--font-size-2xl);
}

.summary-card strong.summary-text {
  overflow: hidden;
  font-size: var(--font-size-lg);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.summary-card-primary {
  background: var(--color-primary);
}

.summary-card-primary span,
.summary-card-primary small {
  color: rgba(255, 255, 255, 0.72);
}

.summary-card-primary strong {
  color: var(--text-inverse);
}

.summary-card-muted {
  background: var(--bg-tertiary);
}

.summary-code {
  display: block;
  overflow: hidden;
  color: var(--color-primary);
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm) !important;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.orders-section {
  display: grid;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.section-heading h2 {
  margin: 0;
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  margin: 0;
  padding: 0;
  list-style: none;
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
  background: var(--color-danger-light);
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

.order-metrics b {
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
  display: block;
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
}

@media (max-width: 768px) {
  .detail-header {
    align-items: stretch;
    flex-direction: column;
  }

  .summary-grid {
    grid-template-columns: 1fr 1fr;
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

@media (max-width: 520px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }

  .summary-card {
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
  }

  .summary-card strong {
    grid-row: span 2;
    margin: 0;
  }
}
</style>
