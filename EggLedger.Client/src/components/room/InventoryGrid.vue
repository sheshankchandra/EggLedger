<template>
  <section class="inventory-section" aria-labelledby="inventory-heading">
    <div class="section-heading">
      <div>
        <p class="eyebrow">Inventory</p>
        <h2 id="inventory-heading">{{ heading || `Available ${resource.inventoryPlural}` }}</h2>
      </div>
      <span v-if="!loading" class="inventory-count">
        {{ containers.length }}
        {{ containers.length === 1 ? resource.inventorySingular : resource.inventoryPlural }}
      </span>
    </div>

    <LoadingSkeleton
      v-if="loading"
      :count="3"
      height="260px"
      min-column-width="245px"
      aria-label="Loading inventory"
    />

    <EmptyState
      v-else-if="containers.length === 0"
      :icon="resource.icon"
      :title="emptyTitle"
      :description="emptyDescription"
    />

    <div v-else class="inventory-grid">
      <button
        v-for="container in containers"
        :key="container.containerId"
        class="inventory-card"
        type="button"
        @click="$emit('select', container)"
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
</template>

<script setup>
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'
import EmptyState from '@/components/common/EmptyState.vue'

defineProps({
  containers: { type: Array, required: true },
  loading: { type: Boolean, default: false },
  resource: { type: Object, required: true },
  currentUserId: { type: String, default: null },
  heading: { type: String, default: '' },
  emptyTitle: { type: String, default: 'No stock yet' },
  emptyDescription: {
    type: String,
    default: 'Add the first purchase to make shared inventory visible to everyone.',
  },
})

defineEmits(['select'])

const stockPercentage = (container) => {
  if (!container.totalQuantity) return 0
  return Math.max(
    0,
    Math.min(100, Math.round((container.remainingQuantity / container.totalQuantity) * 100)),
  )
}
</script>

<style scoped>
.quick-actions,
.inventory-section {
  display: grid;
  gap: var(--spacing-lg);
}

.section-heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
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
  display: grid;
  width: 42px;
  height: 42px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
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

@media (max-width: 768px) {
  .section-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .section-heading > p {
    text-align: left;
  }
}
</style>
