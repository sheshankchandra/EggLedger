<template>
  <Transition name="toast-fade">
    <div
      v-if="notification"
      :class="['notification', notification.type]"
      role="status"
      aria-live="polite"
    >
      {{ notification.message }}
    </div>
  </Transition>
</template>

<script setup>
// Presentation-only: pass the `notification` ref from useNotification() straight through.
defineProps({
  notification: { type: Object, default: null },
})
</script>

<style scoped>
.toast-fade-enter-active,
.toast-fade-leave-active {
  transition:
    opacity var(--transition-normal),
    transform var(--transition-normal);
}

.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
