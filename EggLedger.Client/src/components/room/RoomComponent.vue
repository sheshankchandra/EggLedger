<template>
  <div class="room-workspace">
    <header class="room-header">
      <div class="room-title">
        <div>
          <p class="eyebrow">Shared inventory</p>
          <h1>{{ room.roomName }}</h1>
          <p>Keep purchases and usage in sync for everyone in this room.</p>
        </div>
        <button
          v-if="isRoomAdmin"
          @click="showDeleteConfirm"
          class="room-settings-button"
          type="button"
        >
          Room settings
        </button>
      </div>

      <div class="summary-grid" aria-label="Room summary">
        <div class="summary-card summary-card-primary">
          <span>{{ resource.displayName }} available</span>
          <strong>{{ room.totalEggs || 0 }}</strong>
          <small>Across all active {{ resource.inventoryPlural }}</small>
        </div>
        <div class="summary-card">
          <span>Active {{ resource.inventoryPlural }}</span>
          <strong>{{ containersLoading ? '—' : containers.length }}</strong>
          <small>Purchased stock in this room</small>
        </div>
        <div class="summary-card">
          <span>Room members</span>
          <strong>{{ room.memberCount || 0 }}</strong>
          <small>People sharing this inventory</small>
        </div>
        <div class="summary-card">
          <span>Room code</span>
          <strong class="summary-code">{{ room.roomCode }}</strong>
          <small>Share it with someone you trust</small>
        </div>
      </div>
    </header>

    <section class="quick-actions" aria-labelledby="quick-actions-heading">
      <div class="section-heading">
        <div>
          <p class="eyebrow">Quick actions</p>
          <h2 id="quick-actions-heading">Update inventory</h2>
        </div>
        <p>Every update is recorded in the room ledger.</p>
      </div>

      <div class="action-grid">
        <form class="action-panel" @submit.prevent="handleStock">
          <div class="action-panel-heading">
            <span class="action-icon" aria-hidden="true">＋</span>
            <div>
              <h3>Add a purchase</h3>
              <p>Record a new {{ resource.inventorySingular }} of {{ resource.plural }}.</p>
            </div>
          </div>
          <div class="stock-fields">
            <div class="form-group field-name">
              <label for="batch-name" class="form-label"
                >{{ resource.inventorySingular }} name</label
              >
              <input
                id="batch-name"
                v-model.trim="stockForm.containerName"
                type="text"
                maxlength="100"
                :placeholder="`For example, weekly ${resource.plural}`"
                class="form-input"
              />
            </div>
            <div class="form-group">
              <label for="stock-quantity" class="form-label">Quantity</label>
              <input
                id="stock-quantity"
                v-model.number="stockForm.quantity"
                type="number"
                min="1"
                class="form-input"
                required
              />
            </div>
            <div class="form-group">
              <label for="stock-amount" class="form-label">Total price</label>
              <input
                id="stock-amount"
                v-model.number="stockForm.amount"
                type="number"
                step="0.01"
                min="0"
                class="form-input"
                required
              />
            </div>
          </div>
          <button type="submit" :disabled="loading" class="btn btn-primary">
            {{ loading ? 'Saving purchase…' : 'Add purchase' }}
          </button>
        </form>

        <form class="action-panel consume-panel" @submit.prevent="handleConsume">
          <div class="action-panel-heading">
            <span class="action-icon" aria-hidden="true">−</span>
            <div>
              <h3>Record usage</h3>
              <p>Subtract {{ resource.plural }} using the oldest available stock first.</p>
            </div>
          </div>
          <div class="consume-row">
            <div class="form-group">
              <label for="consume-quantity" class="form-label">Quantity used</label>
              <input
                id="consume-quantity"
                v-model.number="consumeForm.quantity"
                type="number"
                min="1"
                class="form-input quantity-input"
                required
              />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-primary">
              {{ loading ? 'Recording usage…' : 'Record usage' }}
            </button>
          </div>
          <small class="action-hint">
            Available now: {{ room.totalEggs || 0 }} {{ resource.plural }}
          </small>
        </form>
      </div>
      <div v-if="error" class="alert alert-error" role="alert">{{ error }}</div>
    </section>

    <section class="inventory-section" aria-labelledby="inventory-heading">
      <div class="section-heading">
        <div>
          <p class="eyebrow">Inventory</p>
          <h2 id="inventory-heading">Available {{ resource.inventoryPlural }}</h2>
        </div>
        <span v-if="!containersLoading" class="inventory-count">
          {{ containers.length }}
          {{ containers.length === 1 ? resource.inventorySingular : resource.inventoryPlural }}
        </span>
      </div>

      <div v-if="containersLoading" class="inventory-grid" aria-label="Loading inventory">
        <div v-for="index in 3" :key="index" class="inventory-card inventory-skeleton"></div>
      </div>
      <div v-else-if="containers.length === 0" class="empty-inventory">
        <span aria-hidden="true">{{ resource.icon }}</span>
        <h3>No stock yet</h3>
        <p>Add the first purchase to make shared inventory visible to everyone.</p>
      </div>
      <div v-else class="inventory-grid">
        <button
          v-for="container in containers"
          :key="container.containerId"
          class="inventory-card"
          type="button"
          @click="openContainerDetail(container)"
        >
          <span class="inventory-card-top">
            <span class="inventory-icon" aria-hidden="true">{{ resource.icon }}</span>
            <span class="stock-status">{{ stockPercentage(container) }}% remaining</span>
          </span>
          <strong>{{ container.containerName || `Untitled ${resource.inventorySingular}` }}</strong>
          <span class="inventory-quantity">
            <b>{{ container.remainingQuantity }}</b> of {{ container.totalQuantity }}
            {{ resource.plural }}
          </span>
          <span class="stock-track" aria-hidden="true">
            <span :style="{ width: `${stockPercentage(container)}%` }"></span>
          </span>
          <span class="inventory-owner">
            Purchased by {{ container.buyerId === currentUserId ? 'you' : container.buyerName }}
          </span>
          <span class="inventory-details"> View details <span aria-hidden="true">→</span> </span>
        </button>
      </div>
    </section>

    <div v-if="showDetailModal" class="modal" @click.self="closeDetailModal">
      <div
        class="modal-content detail-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="detail-title"
      >
        <div class="modal-header">
          <div>
            <p class="eyebrow">{{ resource.inventorySingular }} details</p>
            <h2 id="detail-title" class="modal-title">{{ selectedContainer.containerName }}</h2>
          </div>
          <button @click="closeDetailModal" class="close-btn" type="button" aria-label="Close">
            ×
          </button>
        </div>
        <div class="modal-body">
          <div class="detail-stock">
            <span class="inventory-icon" aria-hidden="true">{{ resource.icon }}</span>
            <div>
              <strong>{{ selectedContainer.remainingQuantity }}</strong>
              <span>of {{ selectedContainer.totalQuantity }} {{ resource.plural }} remaining</span>
            </div>
          </div>
          <dl class="detail-list">
            <div>
              <dt>Purchased by</dt>
              <dd>{{ selectedContainer.buyerName }}</dd>
            </div>
            <div>
              <dt>Purchase date</dt>
              <dd>{{ formatDate(selectedContainer.purchaseDateTime) }}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>Available</dd>
            </div>
          </dl>
        </div>
        <div class="modal-footer">
          <button @click="closeDetailModal" class="btn btn-secondary" type="button">Close</button>
          <button
            v-if="selectedContainer.buyerId === currentUserId"
            @click="openDeleteContainerConfirm"
            class="btn btn-danger"
            type="button"
          >
            Delete {{ resource.inventorySingular }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="showDeleteContainerModal" class="modal" @click.self="closeDeleteContainerConfirm">
      <div class="modal-content confirm-modal" role="alertdialog" aria-modal="true">
        <div class="modal-header">
          <h2 class="modal-title">Delete this {{ resource.inventorySingular }}?</h2>
          <button
            @click="closeDeleteContainerConfirm"
            class="close-btn"
            type="button"
            aria-label="Close"
          >
            ×
          </button>
        </div>
        <div class="modal-body">
          <p>
            <strong>{{ containerToDelete?.containerName }}</strong> will be removed from available
            inventory. Its ledger history will remain.
          </p>
          <div class="alert alert-warning">
            This is only allowed when none of its {{ resource.plural }} have been used.
          </div>
        </div>
        <div class="modal-footer">
          <button @click="closeDeleteContainerConfirm" class="btn btn-secondary" type="button">
            Keep it
          </button>
          <button
            @click="confirmDeleteContainer"
            :disabled="deletingContainer"
            class="btn btn-danger"
            type="button"
          >
            {{ deletingContainer ? 'Deleting…' : `Delete ${resource.inventorySingular}` }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="showDeleteModal" class="modal" @click.self="closeDeleteModal">
      <div class="modal-content confirm-modal" role="alertdialog" aria-modal="true">
        <div class="modal-header">
          <h2 class="modal-title">Archive this room?</h2>
          <button @click="closeDeleteModal" class="close-btn" type="button" aria-label="Close">
            ×
          </button>
        </div>
        <div class="modal-body">
          <p>
            <strong>{{ room.roomName }}</strong> will no longer be available to its members.
          </p>
          <div class="alert alert-warning">
            Archive the room only when all {{ resource.plural }} are consumed and no orders are in
            progress.
          </div>
        </div>
        <div class="modal-footer">
          <button @click="closeDeleteModal" class="btn btn-secondary" type="button">Cancel</button>
          <button
            @click="confirmDeleteRoom"
            :disabled="loading"
            class="btn btn-danger"
            type="button"
          >
            {{ loading ? 'Archiving…' : 'Archive room' }}
          </button>
        </div>
      </div>
    </div>

    <div
      v-if="notification"
      :class="['notification', notification.type]"
      role="status"
      aria-live="polite"
    >
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { resourceConfig as resource } from '@/config/resource.config'
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
const showDeleteContainerModal = ref(false)
const containerToDelete = ref(null)
const deletingContainer = ref(false)

// Computed properties
const isRoomAdmin = computed(() => {
  const user = authStore.getUser
  if (!user || !user.userId || !props.room || !props.room.adminUserId) {
    return false
  }
  return user.userId === props.room.adminUserId
})

const currentUserId = computed(() => authStore.getUser?.userId)

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

const stockPercentage = (container) => {
  if (!container.totalQuantity) return 0
  return Math.max(
    0,
    Math.min(100, Math.round((container.remainingQuantity / container.totalQuantity) * 100)),
  )
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
    const response = await orderService.consumeOrder(
      props.room.roomCode,
      {
        quantity: consumeForm.value.quantity,
      },
      abortController.signal,
    )

    await authStore.fetchUserRooms()
    await fetchContainers()

    const result = response.data
    // The API returns a message only when the consume was not fulfilled.
    if (result?.message) {
      showNotification(result.message, 'error')
    } else {
      showNotification(
        `Recorded ${result?.requestedQuantity ?? consumeForm.value.quantity} ${resource.plural} used.`,
      )
      consumeForm.value.quantity = 1
    }
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

const openDeleteContainerConfirm = () => {
  containerToDelete.value = selectedContainer.value
  showDeleteContainerModal.value = true
}

const closeDeleteContainerConfirm = () => {
  showDeleteContainerModal.value = false
  containerToDelete.value = null
}

const confirmDeleteContainer = async () => {
  const container = containerToDelete.value
  if (!container) return

  deletingContainer.value = true
  try {
    await containerService.deleteContainer(props.room.roomCode, container.containerId)
    await fetchContainers()
    closeDeleteContainerConfirm()
    closeDetailModal()
    showNotification(`${resource.inventorySingular} deleted.`)
  } catch (err) {
    if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') return
    const message =
      err.response?.status === 409
        ? err.response.data?.[0] ||
          `Some ${resource.plural} have already been used from this ${resource.inventorySingular}.`
        : err.response?.status === 403
          ? `Only the purchaser can delete this ${resource.inventorySingular}.`
          : `Failed to delete the ${resource.inventorySingular}.`
    showNotification(message, 'error')
    closeDeleteContainerConfirm()
  } finally {
    deletingContainer.value = false
  }
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

.room-workspace {
  display: grid;
  gap: var(--spacing-2xl);
}

.room-workspace .btn {
  width: auto;
  margin-top: 0;
}

.room-header {
  display: grid;
  gap: var(--spacing-xl);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, #ffffff, #f1f7f3);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-md);
}

.room-title,
.section-heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
}

.room-title h1 {
  margin-bottom: var(--spacing-sm);
  font-size: clamp(2rem, 5vw, 3rem);
  letter-spacing: -0.04em;
}

.room-title p:last-child,
.section-heading p {
  margin: 0;
}

.room-settings-button {
  flex-shrink: 0;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

.room-settings-button:hover {
  border-color: var(--color-danger);
  color: var(--color-danger);
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
  background: rgba(255, 255, 255, 0.72);
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

.summary-code {
  overflow: hidden;
  color: var(--color-primary);
  font-family: var(--font-family-mono);
  text-overflow: ellipsis;
}

.quick-actions,
.inventory-section {
  display: grid;
  gap: var(--spacing-lg);
}

.section-heading h2 {
  margin: 0;
}

.section-heading > p {
  max-width: 360px;
  color: var(--text-muted);
  font-size: var(--font-size-sm);
  text-align: right;
}

.action-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(280px, 0.75fr);
  gap: var(--spacing-lg);
}

.action-panel {
  display: flex;
  flex-direction: column;
  padding: var(--spacing-lg);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.action-panel-heading {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
}

.action-panel-heading h3 {
  margin-bottom: var(--spacing-xs);
}

.action-panel-heading p {
  margin: 0;
  font-size: var(--font-size-sm);
}

.action-icon,
.inventory-icon {
  display: grid;
  flex: 0 0 auto;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.action-icon {
  width: 44px;
  height: 44px;
}

.stock-fields {
  display: grid;
  grid-template-columns: minmax(0, 1.5fr) minmax(90px, 0.6fr) minmax(110px, 0.7fr);
  gap: var(--spacing-sm);
}

.action-panel .form-group {
  margin-bottom: var(--spacing-md);
}

.action-panel .form-group input {
  padding: var(--input-padding);
}

.action-panel > .btn {
  align-self: flex-end;
}

.consume-panel {
  background: #f5f8fb;
}

.consume-row {
  display: grid;
  grid-template-columns: 120px 1fr;
  align-items: end;
  gap: var(--spacing-sm);
}

.consume-row .btn {
  margin-bottom: var(--spacing-md);
}

.quantity-input {
  text-align: center;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.action-hint {
  margin-top: auto;
  color: var(--text-muted);
}

.inventory-count {
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: 999px;
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.inventory-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 245px), 1fr));
  gap: var(--spacing-md);
}

.inventory-card {
  display: flex;
  min-height: 260px;
  flex-direction: column;
  padding: var(--spacing-lg);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
  color: var(--text-primary);
  text-align: left;
  cursor: pointer;
  transition:
    border-color var(--transition-normal),
    box-shadow var(--transition-normal),
    transform var(--transition-normal);
}

.inventory-card:hover {
  border-color: rgba(23, 107, 82, 0.4);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.inventory-card-top,
.inventory-details {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.inventory-icon {
  width: 42px;
  height: 42px;
}

.stock-status {
  color: var(--color-primary);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}

.inventory-card > strong {
  margin-top: var(--spacing-lg);
  font-size: var(--font-size-lg);
}

.inventory-quantity {
  margin-top: var(--spacing-xs);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.inventory-quantity b {
  color: var(--text-primary);
  font-size: var(--font-size-xl);
}

.stock-track {
  height: 6px;
  margin-top: var(--spacing-md);
  overflow: hidden;
  border-radius: 999px;
  background: var(--bg-tertiary);
}

.stock-track span {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: var(--color-primary);
}

.inventory-owner {
  margin-top: var(--spacing-md);
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.inventory-details {
  margin-top: auto;
  padding-top: var(--spacing-md);
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}

.inventory-skeleton {
  border-color: transparent;
  background: linear-gradient(90deg, #edf2ef 25%, #f8faf9 50%, #edf2ef 75%);
  background-size: 200% 100%;
  animation: inventory-shimmer 1.4s infinite;
  cursor: default;
}

.empty-inventory {
  padding: var(--spacing-2xl) var(--spacing-lg);
  border: 1px dashed var(--border-medium);
  border-radius: var(--radius-xl);
  text-align: center;
}

.empty-inventory > span {
  font-size: var(--font-size-3xl);
}

.empty-inventory h3 {
  margin: var(--spacing-sm) 0;
}

.empty-inventory p {
  margin: 0;
}

.detail-modal {
  max-width: 520px;
}

.confirm-modal {
  max-width: 480px;
}

.detail-stock {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
}

.detail-stock .inventory-icon {
  background: var(--bg-primary);
}

.detail-stock strong,
.detail-stock span {
  display: block;
}

.detail-stock strong {
  color: var(--color-primary);
  font-size: var(--font-size-3xl);
}

.detail-stock span {
  color: var(--text-secondary);
}

.detail-list {
  margin: var(--spacing-lg) 0 0;
}

.detail-list > div {
  display: flex;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding-block: var(--spacing-sm);
  border-bottom: 1px solid var(--border-light);
}

.detail-list dt {
  color: var(--text-muted);
}

.detail-list dd {
  margin: 0;
  color: var(--text-primary);
  font-weight: var(--font-weight-semibold);
  text-align: right;
}

@keyframes inventory-shimmer {
  to {
    background-position: -200% 0;
  }
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

  .room-title,
  .section-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .room-settings-button {
    align-self: flex-start;
  }

  .summary-grid {
    grid-template-columns: 1fr 1fr;
  }

  .action-grid {
    grid-template-columns: 1fr;
  }

  .section-heading > p {
    text-align: left;
  }
}

@media (max-width: 520px) {
  .room-header {
    padding: var(--spacing-lg);
  }

  .summary-grid,
  .stock-fields,
  .consume-row {
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

  .action-panel > .btn,
  .consume-row .btn,
  .modal-footer .btn {
    width: 100%;
  }

  .modal-footer {
    flex-direction: column-reverse;
  }
}
</style>
