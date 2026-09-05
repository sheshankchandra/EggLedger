<template>
  <div class="modal" @click.self="handleOverlayClick" @keydown.esc="$emit('close')">
    <div
      ref="contentRef"
      class="modal-content"
      :class="contentClass"
      :role="role"
      aria-modal="true"
      :aria-labelledby="titleId"
      tabindex="-1"
    >
      <div class="modal-header">
        <div>
          <p v-if="$slots.eyebrow" class="eyebrow"><slot name="eyebrow" /></p>
          <h2 :id="titleId" class="modal-title">{{ title }}</h2>
        </div>
        <button
          v-if="closable"
          @click="$emit('close')"
          class="close-btn"
          type="button"
          aria-label="Close"
        >
          <X :size="18" aria-hidden="true" />
        </button>
      </div>
      <div class="modal-body">
        <slot />
      </div>
      <div v-if="$slots.footer" class="modal-footer">
        <slot name="footer" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref, useId } from 'vue'
import { X } from '@lucide/vue'

const props = defineProps({
  title: { type: String, default: '' },
  // 'dialog' for informational modals, 'alertdialog' for confirmations that demand a response.
  role: { type: String, default: 'dialog' },
  closable: { type: Boolean, default: true },
  closeOnOverlayClick: { type: Boolean, default: true },
  contentClass: { type: [String, Array, Object], default: '' }, // e.g. 'detail-modal', 'confirm-modal'
})

const emit = defineEmits(['close'])
const titleId = `modal-title-${useId()}`
const contentRef = ref(null)

const handleOverlayClick = () => {
  if (props.closeOnOverlayClick) emit('close')
}

// Move focus into the dialog so Escape/Tab work immediately and screen readers announce it.
onMounted(() => {
  contentRef.value?.focus()
})
</script>

<style scoped>
.modal {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  background: var(--bg-overlay);
  backdrop-filter: blur(2px);
}

.modal-content {
  width: 100%;
  max-width: 480px;
  max-height: min(88vh, 720px);
  overflow-y: auto;
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-xl);
  outline: none;
}

.modal-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-lg) 0;
}

.modal-title {
  margin: 0;
}

.close-btn {
  display: grid;
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  place-items: center;
  border: 0;
  border-radius: var(--radius-md);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
}

.close-btn:hover {
  background: var(--bg-tertiary);
}

.modal-body {
  padding: var(--spacing-lg);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  padding: 0 var(--spacing-lg) var(--spacing-lg);
}

@media (max-width: 520px) {
  .modal {
    padding: var(--spacing-sm);
    align-items: flex-end;
  }

  .modal-content {
    max-width: none;
    max-height: 92vh;
  }

  .modal-footer {
    flex-direction: column-reverse;
  }
}
</style>
