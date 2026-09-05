<template>
  <div class="room-workspace">
    <header class="room-header">
      <div class="room-title">
        <div>
          <p class="eyebrow">Shared inventory</p>
          <h1>{{ room.roomName }}</h1>
          <p>Keep purchases and usage in sync for everyone in this room.</p>
        </div>
        <div class="room-title-actions">
          <router-link to="/room/activity" class="btn btn-secondary btn-sm"> Activity </router-link>
          <router-link to="/room/balances" class="btn btn-secondary btn-sm">
            View balances
          </router-link>
          <button
            v-if="isRoomAdmin"
            @click="showDeleteModal = true"
            class="room-settings-button"
            type="button"
          >
            Room settings
          </button>
        </div>
      </div>

      <div class="summary-grid" aria-label="Room summary">
        <div class="summary-card summary-card-primary">
          <span>{{ resource.displayName }} available</span>
          <strong>{{ room.totalEggs || 0 }}</strong>
          <small>Across all active {{ resource.inventoryPlural }}</small>
        </div>
        <div class="summary-card">
          <span>Active {{ resource.inventoryPlural }}</span>
          <strong>{{ inventoryStore.loading ? '—' : inventoryStore.containers.length }}</strong>
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
          <button type="button" class="copy-invite-link" @click="copyInviteLink">
            {{ copied ? 'Link copied!' : 'Copy invite link' }}
          </button>
        </div>
      </div>
    </header>

    <section
      v-if="isRoomAdmin && !room.isOpen"
      class="pending-section"
      aria-labelledby="pending-heading"
    >
      <div class="section-heading">
        <div>
          <p class="eyebrow">Membership</p>
          <h2 id="pending-heading">Pending join requests</h2>
        </div>
        <span v-if="pendingMembers.length > 0" class="pending-count">
          {{ pendingMembers.length }}
        </span>
      </div>

      <LoadingSkeleton
        v-if="pendingLoading"
        :count="1"
        height="70px"
        aria-label="Loading pending requests"
      />
      <EmptyState
        v-else-if="pendingMembers.length === 0"
        icon="✅"
        title="No pending requests"
        description="Everyone who has asked to join this room has already been approved."
      />
      <ul v-else class="pending-list">
        <li v-for="member in pendingMembers" :key="member.userId" class="pending-item">
          <div class="pending-info">
            <strong>{{ member.name }}</strong>
            <small>{{ member.email }} · Requested {{ formatDate(member.requestedAt) }}</small>
          </div>
          <div class="pending-actions">
            <button
              type="button"
              class="btn btn-secondary btn-sm"
              :disabled="processingMemberId === member.userId"
              @click="handleRejectMember(member.userId)"
            >
              Reject
            </button>
            <button
              type="button"
              class="btn btn-primary btn-sm"
              :disabled="processingMemberId === member.userId"
              @click="handleApproveMember(member.userId)"
            >
              Approve
            </button>
          </div>
        </li>
      </ul>
    </section>

    <section class="quick-actions" aria-labelledby="quick-actions-heading">
      <div class="section-heading">
        <div>
          <p class="eyebrow">Quick actions</p>
          <h2 id="quick-actions-heading">Update inventory</h2>
        </div>
        <p>Every update is recorded in the room ledger.</p>
      </div>

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
import StockOrderForm from './StockOrderForm.vue'
import ConsumeOrderForm from './ConsumeOrderForm.vue'
import InventoryGrid from './InventoryGrid.vue'
import ContainerDetailModal from './ContainerDetailModal.vue'
import ConfirmModal from '@/components/common/ConfirmModal.vue'
import Toast from '@/components/common/ToastNotification.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'

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
const showDeleteModal = ref(false)
const archivingRoom = ref(false)
const showDeleteContainerModal = ref(false)
const containerToDelete = ref(null)
const deletingContainer = ref(false)
const copied = ref(false)

const isRoomAdmin = computed(() => {
  const user = authStore.getUser
  if (!user || !user.userId || !props.room || !props.room.adminUserId) {
    return false
  }
  return user.userId === props.room.adminUserId
})

const copyInviteLink = async () => {
  const link = `${window.location.origin}/join?code=${props.room.roomCode}`
  try {
    await navigator.clipboard.writeText(link)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch (err) {
    console.error('Failed to copy invite link:', err)
    showNotification('Could not copy link. Please copy the room code manually.', 'error')
  }
}

const pendingMembers = ref([])
const pendingLoading = ref(false)
const processingMemberId = ref(null)

const formatDate = (dateString) => {
  if (!dateString) return 'recently'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'recently'
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

.room-header {
  display: grid;
  gap: var(--spacing-xl);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, var(--bg-primary), var(--bg-tertiary));
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

.room-title-actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: var(--spacing-sm);
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

.summary-code {
  overflow: hidden;
  color: var(--color-primary);
  font-family: var(--font-family-mono);
  text-overflow: ellipsis;
}

.copy-invite-link {
  margin-top: var(--spacing-xs);
  padding: 0;
  border: none;
  background: none;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
  text-align: left;
  text-decoration: underline;
  cursor: pointer;
}

.copy-invite-link:hover {
  color: var(--color-primary);
}

.quick-actions {
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

.pending-section {
  display: grid;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.pending-count {
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: 999px;
  background: var(--color-warning-light);
  color: var(--color-warning);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.pending-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  margin: 0;
  padding: 0;
  list-style: none;
}

.pending-item {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
}

.pending-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.pending-info strong {
  font-size: var(--font-size-sm);
}

.pending-info small {
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.pending-actions {
  display: flex;
  flex-shrink: 0;
  gap: var(--spacing-sm);
}

.action-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(280px, 0.75fr);
  gap: var(--spacing-lg);
}

@media (max-width: 768px) {
  .room-title,
  .section-heading {
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

  .section-heading > p {
    text-align: left;
  }
}

@media (max-width: 520px) {
  .room-header {
    padding: var(--spacing-lg);
  }

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
