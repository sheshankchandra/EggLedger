<template>
  <BaseModal :title="container.containerName" content-class="detail-modal" @close="$emit('close')">
    <template #eyebrow>{{ resource.inventorySingular }} details</template>

    <div class="detail-stock">
      <span class="inventory-icon" aria-hidden="true"
        ><component :is="resource.icon" :size="20"
      /></span>
      <div>
        <strong>{{ container.remainingQuantity }}</strong>
        <span>of {{ container.totalQuantity }} {{ resource.plural }} remaining</span>
      </div>
    </div>
    <dl class="detail-list">
      <div>
        <dt>Purchased by</dt>
        <dd>{{ container.buyerName }}</dd>
      </div>
      <div>
        <dt>Purchase date</dt>
        <dd>{{ formatDate(container.purchaseDateTime) }}</dd>
      </div>
      <div>
        <dt>Status</dt>
        <dd>Available</dd>
      </div>
    </dl>

    <template #footer>
      <button @click="$emit('close')" class="btn btn-secondary" type="button">Close</button>
      <button
        v-if="container.buyerId === currentUserId"
        @click="$emit('delete-request')"
        class="btn btn-danger"
        type="button"
      >
        Delete {{ resource.inventorySingular }}
      </button>
    </template>
  </BaseModal>
</template>

<script setup>
import BaseModal from '@/components/common/BaseModal.vue'

defineProps({
  container: { type: Object, required: true },
  resource: { type: Object, required: true },
  currentUserId: { type: String, default: null },
})

defineEmits(['close', 'delete-request'])

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}
</script>

<style scoped>
.detail-modal {
  max-width: 520px;
}

.detail-stock {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
}

.inventory-icon {
  display: grid;
  width: 42px;
  height: 42px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  color: var(--color-primary);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
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
</style>
