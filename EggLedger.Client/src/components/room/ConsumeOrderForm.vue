<template>
  <form class="action-panel consume-panel" @submit.prevent="handleSubmit">
    <div class="action-panel-heading">
      <span class="action-icon" aria-hidden="true"><PackageMinus :size="20" /></span>
      <div>
        <h3>Record usage</h3>
        <p>Subtract {{ resource.plural }} using the oldest available stock first.</p>
      </div>
    </div>
    <div class="consume-row">
      <div class="form-group">
        <label for="consume-quantity" class="form-label">Quantity used</label>
        <input
          id="consume-quantity"
          v-model.number="form.quantity"
          type="number"
          min="1"
          class="form-input quantity-input"
          :class="{ 'is-invalid': touched && quantityError }"
          :aria-invalid="touched && !!quantityError"
          required
          @blur="touched = true"
        />
      </div>
      <button type="submit" :disabled="loading" class="btn btn-primary">
        {{ loading ? 'Recording usage…' : 'Record usage' }}
      </button>
    </div>
    <small v-if="touched && quantityError" class="form-feedback is-invalid">
      {{ quantityError }}
    </small>
    <small v-else class="action-hint">
      Available now: {{ availableCount }} {{ resource.plural }}
    </small>
  </form>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { PackageMinus } from '@lucide/vue'

const props = defineProps({
  resource: { type: Object, required: true },
  loading: { type: Boolean, default: false },
  availableCount: { type: Number, default: 0 },
})

const emit = defineEmits(['submit'])

const form = reactive({ quantity: 1 })
const touched = ref(false)

const quantityError = computed(() => {
  if (!form.quantity || form.quantity < 1) return 'Quantity must be at least 1.'
  return ''
})

const handleSubmit = () => {
  touched.value = true
  if (quantityError.value || props.loading) return
  emit('submit', { quantity: form.quantity })
}

const reset = () => {
  form.quantity = 1
  touched.value = false
}

defineExpose({ reset })
</script>

<style scoped>
.action-panel {
  display: flex;
  flex-direction: column;
  padding: var(--spacing-lg);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.action-panel-heading {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
}

.action-panel-heading h3 {
  margin-bottom: var(--spacing-xs);
}

.action-panel-heading p {
  margin: 0;
  font-size: var(--font-size-sm);
}

.action-icon {
  display: grid;
  width: 44px;
  height: 44px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.action-panel .form-group {
  margin-bottom: var(--spacing-md);
}

.action-panel .form-group input {
  padding: var(--input-padding);
}

.consume-panel {
  background: var(--bg-tertiary);
}

.consume-row {
  display: grid;
  grid-template-columns: 120px 1fr;
  align-items: end;
  gap: var(--spacing-sm);
}

.consume-row .btn {
  margin-bottom: var(--spacing-md);
}

.quantity-input {
  text-align: center;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.action-hint {
  margin-top: auto;
  color: var(--text-muted);
}

@media (max-width: 520px) {
  .consume-row {
    grid-template-columns: 1fr;
  }

  .consume-row .btn {
    width: 100%;
  }
}
</style>
