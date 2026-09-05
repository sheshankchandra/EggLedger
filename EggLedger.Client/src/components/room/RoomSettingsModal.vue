<template>
  <BaseModal title="Room settings" content-class="room-settings-modal" @close="$emit('close')">
    <section class="settings-section">
      <h3 class="settings-section-title">Invite people</h3>
      <div class="invite-row">
        <div class="invite-code">
          <span>Room code</span>
          <strong>{{ room.roomCode }}</strong>
        </div>
        <button type="button" class="btn btn-secondary btn-sm" @click="copyInviteLink">
          <Check v-if="copied" :size="16" aria-hidden="true" />
          <Copy v-else :size="16" aria-hidden="true" />
          {{ copied ? 'Copied!' : 'Copy invite link' }}
        </button>
      </div>
    </section>

    <section v-if="isRoomAdmin && !room.isOpen" class="settings-section">
      <div class="settings-section-heading">
        <h3 class="settings-section-title">Pending join requests</h3>
        <span v-if="pendingMembers.length > 0" class="pending-count">
          {{ pendingMembers.length }}
        </span>
      </div>

      <LoadingSkeleton
        v-if="pendingLoading"
        :count="1"
        height="64px"
        aria-label="Loading pending requests"
      />
      <p v-else-if="pendingMembers.length === 0" class="settings-empty-hint">
        No one is waiting for approval right now.
      </p>
      <ul v-else class="pending-list">
        <li v-for="member in pendingMembers" :key="member.userId" class="pending-item">
          <div class="pending-info">
            <strong>{{ member.name }}</strong>
            <small>{{ member.email }} · Requested {{ formatDate(member.requestedAt) }}</small>
          </div>
          <div class="pending-actions">
            <button
              type="button"
              class="icon-action icon-action-reject"
              :disabled="processingMemberId === member.userId"
              aria-label="Reject request"
              title="Reject"
              @click="$emit('reject-member', member.userId)"
            >
              <UserX :size="16" aria-hidden="true" />
            </button>
            <button
              type="button"
              class="icon-action icon-action-approve"
              :disabled="processingMemberId === member.userId"
              aria-label="Approve request"
              title="Approve"
              @click="$emit('approve-member', member.userId)"
            >
              <UserCheck :size="16" aria-hidden="true" />
            </button>
          </div>
        </li>
      </ul>
    </section>

    <section v-if="isRoomAdmin" class="settings-section settings-danger-zone">
      <h3 class="settings-section-title">Danger zone</h3>
      <p class="settings-danger-hint">
        Archive this room once everything is settled. Members will no longer be able to access it.
      </p>
      <button type="button" class="btn btn-danger" @click="$emit('archive-room')">
        <Archive :size="16" aria-hidden="true" />
        Archive room
      </button>
    </section>
  </BaseModal>
</template>

<script setup>
import { ref } from 'vue'
import { Copy, Check, UserCheck, UserX, Archive } from '@lucide/vue'
import BaseModal from '@/components/common/BaseModal.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'

const props = defineProps({
  room: { type: Object, required: true },
  isRoomAdmin: { type: Boolean, default: false },
  pendingMembers: { type: Array, default: () => [] },
  pendingLoading: { type: Boolean, default: false },
  processingMemberId: { type: [String, Number], default: null },
})

defineEmits(['close', 'approve-member', 'reject-member', 'archive-room'])

const copied = ref(false)

const copyInviteLink = async () => {
  const link = `${window.location.origin}/join?code=${props.room.roomCode}`
  try {
    await navigator.clipboard.writeText(link)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    copied.value = false
  }
}

const formatDate = (dateString) => {
  if (!dateString) return 'recently'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'recently'
  }
}
</script>

<style scoped>
.settings-section {
  padding-block: var(--spacing-lg);
  border-top: 1px solid var(--border-light);
}

.settings-section:first-child {
  padding-top: 0;
  border-top: none;
}

.settings-section-title {
  margin: 0 0 var(--spacing-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.settings-section-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-md);
}

.settings-section-heading .settings-section-title {
  margin: 0;
}

.invite-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  border-radius: var(--radius-lg);
  background: var(--bg-tertiary);
}

.invite-code {
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.invite-code strong {
  font-family: var(--font-family-mono);
  font-size: var(--font-size-xl);
  color: var(--text-primary);
}

.settings-empty-hint {
  margin: 0;
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.pending-count {
  display: inline-grid;
  min-width: 22px;
  height: 22px;
  padding-inline: 6px;
  place-items: center;
  border-radius: 999px;
  background: var(--color-warning-light);
  color: var(--color-warning);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
}

.pending-list {
  display: grid;
  gap: var(--spacing-sm);
  padding: 0;
  list-style: none;
}

.pending-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
}

.pending-info {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.pending-info small {
  color: var(--text-muted);
}

.pending-actions {
  display: flex;
  flex-shrink: 0;
  gap: var(--spacing-xs);
}

.icon-action {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  background: var(--bg-primary);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.icon-action-reject {
  color: var(--color-danger);
}

.icon-action-reject:hover:not(:disabled) {
  background: var(--color-danger-light);
}

.icon-action-approve {
  color: var(--color-primary);
}

.icon-action-approve:hover:not(:disabled) {
  background: var(--color-primary-light);
}

.icon-action:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.settings-danger-zone {
  border-top-color: var(--color-danger-light);
}

.settings-danger-zone .settings-section-title {
  color: var(--color-danger);
}

.settings-danger-hint {
  margin: 0 0 var(--spacing-md);
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}
</style>
