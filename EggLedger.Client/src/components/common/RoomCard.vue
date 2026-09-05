<template>
  <component
    :is="effectiveClickable ? 'button' : 'div'"
    class="room-card"
    :class="{ 'room-card-pending': room.isPending }"
    :type="effectiveClickable ? 'button' : undefined"
    @click="effectiveClickable && $emit('select', room.roomCode)"
  >
    <span class="room-card-top">
      <span class="room-avatar" aria-hidden="true">{{ initials }}</span>
      <span class="room-code">Code {{ room.roomCode }}</span>
    </span>
    <span class="room-name">
      {{ room.roomName }}
      <span v-if="room.isPending" class="pending-badge">Pending approval</span>
      <span v-else-if="isAdmin" class="admin-badge">Admin</span>
    </span>
    <span class="room-meta">
      <span>{{ room.memberCount || 0 }} members</span>
      <span v-if="dateLabel">{{ dateLabel }}</span>
    </span>
    <template v-if="room.isPending">
      <p class="pending-hint">Waiting for the room admin to approve your request to join.</p>
    </template>
    <span v-else class="room-stat-grid">
      <span>
        <strong>{{ room.totalEggs || 0 }}</strong>
        {{ resource.plural }} available
      </span>
      <span>
        <strong>{{ room.containerCount || 0 }}</strong>
        active {{ resource.inventoryPlural }}
      </span>
    </span>
    <span v-if="effectiveClickable" class="open-room"
      >{{ ctaLabel }} <span aria-hidden="true">→</span></span
    >
  </component>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  room: { type: Object, required: true },
  resource: { type: Object, required: true },
  clickable: { type: Boolean, default: true },
  isAdmin: { type: Boolean, default: false },
  dateLabel: { type: String, default: '' },
  ctaLabel: { type: String, default: 'Open room' },
})

defineEmits(['select'])

const effectiveClickable = computed(() => props.clickable && !props.room.isPending)

const initials = computed(() =>
  (props.room.roomName || 'Room')
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase(),
)
</script>

<style scoped>
.room-card {
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
  transition:
    border-color var(--transition-normal),
    box-shadow var(--transition-normal),
    transform var(--transition-normal);
}

button.room-card {
  cursor: pointer;
}

button.room-card:hover {
  border-color: rgba(23, 107, 82, 0.4);
  box-shadow: var(--shadow-md);
  transform: translateY(-3px);
}

.room-card-top,
.room-meta,
.room-stat-grid,
.open-room {
  display: flex;
  align-items: center;
}

.room-card-top {
  justify-content: space-between;
}

.room-avatar {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-weight: var(--font-weight-bold);
}

.room-code {
  color: var(--text-muted);
  font-family: var(--font-family-mono);
  font-size: var(--font-size-xs);
}

.room-name {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-lg);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.admin-badge {
  padding: 2px var(--spacing-sm);
  border-radius: 999px;
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}

.pending-badge {
  padding: 2px var(--spacing-sm);
  border-radius: 999px;
  background: var(--color-warning-light);
  color: var(--color-warning);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}

.room-card-pending {
  opacity: 0.85;
}

.pending-hint {
  margin: var(--spacing-lg) 0 0;
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.room-meta {
  gap: var(--spacing-md);
  margin-top: var(--spacing-xs);
  color: var(--text-muted);
  font-size: var(--font-size-sm);
}

.room-stat-grid {
  gap: var(--spacing-sm);
  margin-top: var(--spacing-lg);
}

.room-stat-grid > span {
  flex: 1;
  padding: var(--spacing-sm);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
}

.room-stat-grid strong {
  display: block;
  color: var(--text-primary);
  font-size: var(--font-size-lg);
}

.open-room {
  justify-content: space-between;
  margin-top: auto;
  padding-top: var(--spacing-lg);
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}
</style>
