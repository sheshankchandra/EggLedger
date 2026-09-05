<template>
  <div class="visibility-toggle">
    <div class="visibility-toggle-track" role="radiogroup" aria-label="Who can join">
      <button
        type="button"
        role="radio"
        :aria-checked="!modelValue"
        :class="{ active: !modelValue }"
        :disabled="disabled"
        @click="$emit('update:modelValue', false)"
      >
        <Lock :size="14" aria-hidden="true" />
        Private
      </button>
      <button
        type="button"
        role="radio"
        :aria-checked="modelValue"
        :class="{ active: modelValue }"
        :disabled="disabled"
        @click="$emit('update:modelValue', true)"
      >
        <Globe :size="14" aria-hidden="true" />
        Open
      </button>
    </div>
    <p class="visibility-toggle-hint">
      {{
        modelValue
          ? 'Anyone with the code joins instantly.'
          : 'New members need admin approval to join.'
      }}
    </p>
  </div>
</template>

<script setup>
import { Lock, Globe } from '@lucide/vue'

defineProps({
  modelValue: { type: Boolean, required: true },
  disabled: { type: Boolean, default: false },
})
defineEmits(['update:modelValue'])
</script>

<style scoped>
.visibility-toggle {
  display: grid;
  gap: var(--spacing-sm);
}

.visibility-toggle-track {
  display: grid;
  grid-template-columns: 1fr 1fr;
  padding: var(--spacing-xs);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
}

.visibility-toggle-track button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm);
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.visibility-toggle-track button.active {
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
  color: var(--color-primary);
}

.visibility-toggle-track button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.visibility-toggle-hint {
  margin: 0;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}
</style>
