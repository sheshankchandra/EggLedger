<template>
  <div class="room-workspace">
    <header class="room-title-bar">
      <div>
        <p class="eyebrow">Shared inventory</p>
        <h1>{{ room.roomName }}</h1>
      </div>
      <div class="room-title-actions">
        <nav class="room-nav-links" aria-label="Room views">
          <router-link to="/room/activity" class="room-nav-link">
            <Clock :size="16" aria-hidden="true" /> Activity
          </router-link>
          <router-link to="/room/balances" class="room-nav-link">
            <Scale :size="16" aria-hidden="true" /> Balances
          </router-link>
        </nav>
        <button
          v-if="isRoomAdmin"
          @click="showSettingsModal = true"
          class="room-icon-button"
          type="button"
          aria-label="Room settings"
          title="Room settings"
        >
          <Settings :size="18" aria-hidden="true" />
        </button>
      </div>
    </header>

    <section class="quick-actions" aria-label="Quick actions">
      <p class="eyebrow">Quick actions</p>
      <div class="action-grid">
        <StockOrderForm
          ref="stockFormRef"
          :resource="resource"
          :loading="stockLoading"
          @submit="handleStock"
        />
        <ConsumeOrderForm
          ref="consumeFormRef"
          :resource="resource"
          :loading="consumeLoading"
          :available-count="room.totalEggs || 0"
          @submit="handleConsume"
        />
      </div>
      <div v-if="error" class="alert alert-error" role="alert">{{ error }}</div>
    </section>

    <div class="summary-grid" aria-label="Room summary">
      <div class="summary-card summary-card-primary">
        <span>{{ resource.displayName }} available</span>
        <strong>{{ room.totalEggs || 0 }}</strong>
      </div>
      <div class="summary-card">
        <span>Active {{ resource.inventoryPlural }}</span>
        <strong>{{ inventoryStore.loading ? '…' : inventoryStore.containers.length }}</strong>
      </div>
      <div class="summary-card">
        <span>Room members</span>
        <strong>{{ room.memberCount || 0 }}</strong>
      </div>
    </div>

    <InventoryGrid
      :containers="inventoryStore.containers"
      :loading="inventoryStore.loading"
      :resource="resource"
      :current-user-id="currentUserId"
      @select="openContainerDetail"
    />

    <ContainerDetailModal
      v-if="selectedContainer"
      :container="selectedContainer"
      :resource="resource"
      :current-user-id="currentUserId"
      @close="closeDetailModal"
      @delete-request="openDeleteContainerConfirm"
    />

    <ConfirmModal
      v-if="showDeleteContainerModal"
      :title="`Delete this ${resource.inventorySingular}?`"
      :warning="`This is only allowed when none of its ${resource.plural} have been used.`"
      cancel-label="Keep it"
      :confirm-label="`Delete ${resource.inventorySingular}`"
      busy-label="Deleting…"
      :busy="deletingContainer"
      @cancel="closeDeleteContainerConfirm"
      @confirm="confirmDeleteContainer"
    >
      <strong>{{ containerToDelete?.containerName }}</strong> will be removed from available
      inventory. Its ledger history will remain.
    </ConfirmModal>

    <RoomSettingsModal
      v-if="showSettingsModal"
      :room="room"
      :is-room-admin="isRoomAdmin"
      :pending-members="pendingMembers"
      :pending-loading="pendingLoading"
      :processing-member-id="processingMemberId"
      :updating-visibility="updatingVisibility"
      @close="showSettingsModal = false"
      @approve-member="handleApproveMember"
      @reject-member="handleRejectMember"
      @archive-room="openArchiveConfirm"
      @update-visibility="handleUpdateVisibility"
    />

    <ConfirmModal
      v-if="showDeleteModal"
      title="Archive this room?"
      :warning="`Archive the room only when all ${resource.plural} are consumed and no orders are in progress.`"
      confirm-label="Archive room"
      busy-label="Archiving…"
      :busy="archivingRoom"
      @cancel="showDeleteModal = false"
      @confirm="confirmDeleteRoom"
    >
      <strong>{{ room.roomName }}</strong> will no longer be available to its members.
    </ConfirmModal>

    <Toast :notification="notification" />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { resourceConfig as resource } from '@/config/resource.config'
import { useAuthStore } from '@/stores/auth.store'
import { useRoomStore } from '@/stores/room.store'
import { useInventoryStore } from '@/stores/inventory.store'
import { useNotification } from '@/composables/useNotification'
import { errorMessage, isCanceled } from '@/utils/httpError'
import { Clock, Scale, Settings } from '@lucide/vue'
import StockOrderForm from './StockOrderForm.vue'
import ConsumeOrderForm from './ConsumeOrderForm.vue'
import InventoryGrid from './InventoryGrid.vue'
import ContainerDetailModal from './ContainerDetailModal.vue'
import RoomSettingsModal from './RoomSettingsModal.vue'
import ConfirmModal from '@/components/common/ConfirmModal.vue'
import Toast from '@/components/common/ToastNotification.vue'

const authStore = useAuthStore()
const roomStore = useRoomStore()
const inventoryStore = useInventoryStore()
const router = useRouter()
const { notification, showNotification } = useNotification()

const props = defineProps({
  room: {
    type: Object,
    required: true,
  },
})

const stockFormRef = ref(null)
const consumeFormRef = ref(null)
const stockLoading = ref(false)
const consumeLoading = ref(false)
const error = ref(null)

const selectedContainer = ref(null)
const showSettingsModal = ref(false)
const showDeleteModal = ref(false)
const archivingRoom = ref(false)
const showDeleteContainerModal = ref(false)
const containerToDelete = ref(null)
const deletingContainer = ref(false)

const isRoomAdmin = computed(() => {
  const user = authStore.getUser
  if (!user || !user.userId || !props.room || !props.room.adminUserId) {
    return false
  }
  return user.userId === props.room.adminUserId
})

const openArchiveConfirm = () => {
  showSettingsModal.value = false
  showDeleteModal.value = true
}

const pendingMembers = ref([])
const pendingLoading = ref(false)
const processingMemberId = ref(null)
const updatingVisibility = ref(false)

const handleUpdateVisibility = async (isOpen) => {
  updatingVisibility.value = true
  try {
    await roomStore.updateRoomVisibility(props.room.roomCode, isOpen)
    showNotification(`Room is now ${isOpen ? 'open' : 'private'}.`)
    await fetchPendingMembers()
  } catch (err) {
    if (isCanceled(err)) return
    showNotification(errorMessage(err, 'Could not update who can join.'), 'error')
  } finally {
    updatingVisibility.value = false
  }
}

const fetchPendingMembers = async () => {
  if (!isRoomAdmin.value || props.room.isOpen) return
  pendingLoading.value = true
  try {
    pendingMembers.value = await roomStore.fetchPendingMembers(props.room.roomCode)
  } catch (err) {
    if (isCanceled(err)) return
    console.error('Failed to fetch pending members:', err)
  } finally {
    pendingLoading.value = false
  }
}

const handleApproveMember = async (memberUserId) => {
  processingMemberId.value = memberUserId
  try {
    await roomStore.approveMember(props.room.roomCode, memberUserId)
    pendingMembers.value = pendingMembers.value.filter((m) => m.userId !== memberUserId)
    showNotification('Member approved.')
  } catch (err) {
    if (isCanceled(err)) return
    showNotification(errorMessage(err, 'Failed to approve member.'), 'error')
  } finally {
    processingMemberId.value = null
  }
}

const handleRejectMember = async (memberUserId) => {
  processingMemberId.value = memberUserId
  try {
    await roomStore.rejectMember(props.room.roomCode, memberUserId)
    pendingMembers.value = pendingMembers.value.filter((m) => m.userId !== memberUserId)
    showNotification('Request rejected.')
  } catch (err) {
    if (isCanceled(err)) return
    showNotification(errorMessage(err, 'Failed to reject member.'), 'error')
  } finally {
    processingMemberId.value = null
  }
}

const currentUserId = computed(() => authStore.getUser?.userId)

const confirmDeleteRoom = async () => {
  if (archivingRoom.value) return

  if (!isRoomAdmin.value) {
    showNotification('You do not have permission to delete this room.', 'error')
    showDeleteModal.value = false
    return
  }

  archivingRoom.value = true
  try {
    await roomStore.deleteRoom(props.room.roomCode)
    showNotification('Room deleted successfully!', 'success')
    showDeleteModal.value = false
    setTimeout(() => router.push('/'), 1500)
  } catch (err) {
    if (isCanceled(err)) return
    showNotification(errorMessage(err, 'Could not delete the room. Please try again.'), 'error')
  } finally {
    archivingRoom.value = false
  }
}

const handleStock = async (payload) => {
  stockLoading.value = true
  error.value = null
  try {
    await inventoryStore.stockOrder(props.room.roomCode, payload)
    await roomStore.fetchUserRooms()
    showNotification('Stock added successfully!')
    stockFormRef.value?.reset()
  } catch (err) {
    if (isCanceled(err)) return
    error.value = 'Failed to add stock.'
    console.error(err)
  } finally {
    stockLoading.value = false
  }
}

const handleConsume = async (payload) => {
  consumeLoading.value = true
  error.value = null
  try {
    const result = await inventoryStore.consumeOrder(props.room.roomCode, payload)
    await roomStore.fetchUserRooms()

    // The API returns a message only when the consume was not fulfilled.
    if (result?.message) {
      showNotification(result.message, 'error')
    } else {
      showNotification(
        `Recorded ${result?.requestedQuantity ?? payload.quantity} ${resource.plural} used.`,
      )
      consumeFormRef.value?.reset()
    }
  } catch (err) {
    if (isCanceled(err)) return
    error.value = 'Failed to record consumption.'
    console.error(err)
  } finally {
    consumeLoading.value = false
  }
}

const openContainerDetail = (container) => {
  selectedContainer.value = container
}

const closeDetailModal = () => {
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
    await inventoryStore.deleteContainer(props.room.roomCode, container.containerId)
    closeDeleteContainerConfirm()
    closeDetailModal()
    showNotification(`${resource.inventorySingular} deleted.`)
  } catch (err) {
    if (isCanceled(err)) return
    const message =
      err.response?.status === 409
        ? errorMessage(
            err,
            `Some ${resource.plural} have already been used from this ${resource.inventorySingular}.`,
          )
        : err.response?.status === 403
          ? `Only the purchaser can delete this ${resource.inventorySingular}.`
          : `Failed to delete the ${resource.inventorySingular}.`
    showNotification(message, 'error')
    closeDeleteContainerConfirm()
  } finally {
    deletingContainer.value = false
  }
}

onMounted(() => {
  inventoryStore.fetchContainers(props.room.roomCode)
  fetchPendingMembers()
})
</script>

<style scoped>
.room-workspace {
  display: grid;
  gap: var(--spacing-2xl);
}

.room-workspace .btn {
  width: auto;
  margin-top: 0;
}

.room-title-bar {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
}

.room-title-bar h1 {
  margin-bottom: 0;
  font-size: clamp(2rem, 5vw, 3rem);
  letter-spacing: -0.04em;
}

.room-title-actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: var(--spacing-sm);
}

.room-nav-links {
  display: flex;
  flex-shrink: 0;
  gap: var(--spacing-xs);
}

.room-nav-link {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  text-decoration: none;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.room-nav-link:hover {
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

.room-icon-button {
  display: grid;
  flex-shrink: 0;
  width: 42px;
  height: 42px;
  place-items: center;
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: var(--font-size-lg);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.room-icon-button:hover {
  border-color: var(--color-danger);
  background: var(--bg-tertiary);
  color: var(--color-danger);
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
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

.quick-actions {
  display: grid;
  gap: var(--spacing-lg);
}

.action-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(280px, 0.75fr);
  gap: var(--spacing-lg);
}

@media (max-width: 768px) {
  .room-title-bar {
    align-items: stretch;
    flex-direction: column;
  }

  .room-title-actions {
    align-self: flex-start;
  }

  .summary-grid {
    grid-template-columns: 1fr 1fr;
  }

  .action-grid {
    grid-template-columns: 1fr;
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
