<template>
  <div
    class="skeleton-grid"
    :style="{ '--skeleton-min-col': minColumnWidth }"
    :aria-label="ariaLabel"
  >
    <div
      v-for="index in count"
      :key="index"
      class="skeleton-card"
      :style="{ minHeight: height }"
    ></div>
  </div>
</template>

<script setup>
defineProps({
  count: { type: Number, default: 3 },
  height: { type: String, default: '220px' },
  minColumnWidth: { type: String, default: '290px' },
  ariaLabel: { type: String, default: 'Loading' },
})
</script>

<style scoped>
.skeleton-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, var(--skeleton-min-col)), 1fr));
  gap: var(--spacing-lg);
}

.skeleton-card {
  border-radius: var(--radius-xl);
  background: linear-gradient(
    90deg,
    var(--bg-tertiary) 25%,
    var(--bg-secondary) 50%,
    var(--bg-tertiary) 75%
  );
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.4s infinite;
}

@keyframes skeleton-shimmer {
  to {
    background-position: -200% 0;
  }
}
</style>
