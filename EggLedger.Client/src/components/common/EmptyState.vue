<template>
  <div class="empty-state">
    <div class="empty-icon" aria-hidden="true">
      <component :is="icon" :size="28" />
    </div>
    <h3>{{ title }}</h3>
    <p v-if="description">{{ description }}</p>
    <div v-if="$slots.actions" class="empty-actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup>
defineProps({
  icon: { type: [Object, Function], required: true },
  title: { type: String, required: true },
  description: { type: String, default: '' },
})
</script>

<style scoped>
.empty-state {
  padding: var(--spacing-2xl) var(--spacing-lg);
  border: 1px dashed var(--border-medium);
  border-radius: var(--radius-xl);
  background: color-mix(in srgb, var(--bg-primary) 64%, transparent);
  text-align: center;
}

.empty-state h3 {
  margin-bottom: var(--spacing-sm);
}

.empty-state p {
  max-width: 520px;
  margin: 0 auto var(--spacing-lg);
}

.empty-icon {
  display: grid;
  width: 56px;
  height: 56px;
  margin: 0 auto var(--spacing-md);
  place-items: center;
  border-radius: 50%;
  background: var(--color-primary-light);
  color: var(--color-primary);
}

.empty-actions {
  display: flex;
  justify-content: center;
  gap: var(--spacing-sm);
}

@media (max-width: 640px) {
  .empty-actions {
    flex-direction: column;
  }
}
</style>
