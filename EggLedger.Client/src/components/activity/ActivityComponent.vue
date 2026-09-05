<template>
  <div class="activity-workspace">
    <header class="activity-header">
      <div>
        <p class="eyebrow">Activity</p>
        <h1>Recent activity</h1>
        <p>What's been happening with {{ resource.plural }} in {{ room.roomName }}.</p>
      </div>
      <router-link to="/room" class="btn btn-secondary">
        <ArrowLeft :size="16" aria-hidden="true" /> Back to room
      </router-link>
    </header>

    <LoadingSkeleton
      v-if="activityStore.loading"
      :count="4"
      height="70px"
      aria-label="Loading activity"
    />
    <div v-if="activityStore.error" class="alert alert-error">{{ activityStore.error }}</div>

    <template v-else>
      <EmptyState
        v-if="activityStore.events.length === 0"
        :icon="ClipboardList"
        title="No activity yet"
        description="Stock updates, consumption, settlements, and new members will show up here."
      />
      <section v-else class="feed-section" aria-label="Activity feed">
        <ul class="feed-list">
          <li v-for="(event, index) in activityStore.events" :key="index" class="feed-item">
            <span
              class="feed-icon"
              :class="`feed-icon-${eventClass(event.eventType)}`"
              aria-hidden="true"
            >
              <component
                :is="resource.icon"
                v-if="event.eventType === EVENT_TYPE.CONSUME"
                :size="18"
              />
              <component :is="eventIcon(event.eventType)" v-else :size="18" />
            </span>
            <div class="feed-body">
              <p class="feed-text">{{ describeEvent(event) }}</p>
              <span class="feed-time">{{ formatDateTime(event.timestamp) }}</span>
            </div>
          </li>
        </ul>

        <button
          v-if="activityStore.hasMore"
          type="button"
          class="btn btn-secondary load-more"
          :disabled="activityStore.loadingMore"
          @click="activityStore.loadMore()"
        >
          {{ activityStore.loadingMore ? 'Loading…' : 'Load more' }}
        </button>
      </section>
    </template>
  </div>
</template>

<script setup>
import { onMounted, watch } from 'vue'
import { ClipboardList, Package, HandCoins, UserPlus, Circle, ArrowLeft } from '@lucide/vue'
import { useActivityStore } from '@/stores/activity.store'
import { resourceConfig as resource } from '@/config/resource.config'
import EmptyState from '@/components/common/EmptyState.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'

const props = defineProps({
  room: { type: Object, required: true },
})

const activityStore = useActivityStore()

// Mirrors EggLedger.Models.Enums.ActivityEventType - enums serialize as their numeric value.
const EVENT_TYPE = { STOCK: 1, CONSUME: 2, SETTLEMENT: 3, MEMBER_JOINED: 4 }

const eventIcon = (eventType) => {
  switch (eventType) {
    case EVENT_TYPE.STOCK:
      return Package
    case EVENT_TYPE.SETTLEMENT:
      return HandCoins
    case EVENT_TYPE.MEMBER_JOINED:
      return UserPlus
    default:
      return Circle
  }
}

const eventClass = (eventType) => {
  switch (eventType) {
    case EVENT_TYPE.STOCK:
      return 'stock'
    case EVENT_TYPE.CONSUME:
      return 'consume'
    case EVENT_TYPE.SETTLEMENT:
      return 'settlement'
    case EVENT_TYPE.MEMBER_JOINED:
      return 'joined'
    default:
      return 'default'
  }
}

const describeEvent = (event) => {
  const amount = event.amount != null ? `₹${Number(event.amount).toFixed(2)}` : null
  switch (event.eventType) {
    case EVENT_TYPE.STOCK: {
      const container = event.containerName ? ` · ${event.containerName}` : ''
      return `${event.actorName} stocked ${event.quantity} ${resource.plural}${container}${amount ? ` (${amount})` : ''}`
    }
    case EVENT_TYPE.CONSUME:
      return `${event.actorName} used ${event.quantity} ${resource.plural}`
    case EVENT_TYPE.SETTLEMENT:
      return `${event.actorName} paid ${event.counterpartyName} ${amount || ''}`.trim()
    case EVENT_TYPE.MEMBER_JOINED:
      return `${event.actorName} joined the room`
    default:
      return `${event.actorName} did something`
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

const loadActivity = () => activityStore.fetchActivity(props.room.roomCode)

onMounted(loadActivity)

watch(
  () => props.room.roomCode,
  (newCode, oldCode) => {
    if (newCode && newCode !== oldCode) loadActivity()
  },
)
</script>

<style scoped>
.activity-workspace {
  display: grid;
  gap: var(--spacing-2xl);
}

.activity-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, var(--bg-primary), var(--bg-tertiary));
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-md);
}

.activity-header h1 {
  margin-bottom: var(--spacing-sm);
  font-size: clamp(2rem, 5vw, 3rem);
  letter-spacing: -0.04em;
}

.activity-header p:last-child {
  margin: 0;
}

.feed-section {
  display: grid;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.feed-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  margin: 0;
  padding: 0;
  list-style: none;
}

.feed-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
}

.feed-icon {
  display: grid;
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--bg-tertiary);
}

.feed-icon-settlement {
  background: var(--color-success-light);
}

.feed-icon-joined {
  background: var(--color-info-light);
}

.feed-body {
  display: flex;
  flex-direction: column;
  min-width: 0;
  gap: 2px;
}

.feed-text {
  margin: 0;
  overflow: hidden;
  color: var(--text-primary);
  font-size: var(--font-size-sm);
  text-overflow: ellipsis;
}

.feed-time {
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.load-more {
  justify-self: center;
}

@media (max-width: 768px) {
  .activity-header {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
