<template>
  <form class="action-panel" @submit.prevent="handleSubmit">
    <div class="action-panel-heading">
      <span class="action-icon" aria-hidden="true"><PackagePlus :size="20" /></span>
      <div>
        <h3>Add a purchase</h3>
        <p>Record a new {{ resource.inventorySingular }} of {{ resource.plural }}.</p>
      </div>
    </div>
    <div class="stock-fields">
      <div class="form-group field-name">
        <label for="batch-name" class="form-label">{{ resource.inventorySingular }} name</label>
        <input
          id="batch-name"
          v-model.trim="form.containerName"
          type="text"
          maxlength="100"
          :placeholder="`For example, weekly ${resource.plural}`"
          class="form-input"
        />
      </div>
      <div class="form-group">
        <label for="stock-quantity" class="form-label">Quantity</label>
        <input
          id="stock-quantity"
          v-model.number="form.quantity"
          type="number"
          min="1"
          class="form-input"
          :class="{ 'is-invalid': touched && quantityError }"
          :aria-invalid="touched && !!quantityError"
          required
          @blur="touched = true"
        />
      </div>
      <div class="form-group">
        <label for="stock-amount" class="form-label">Total price</label>
        <input
          id="stock-amount"
          v-model.number="form.amount"
          type="number"
          step="0.01"
          min="0"
          class="form-input"
          :class="{ 'is-invalid': touched && amountError }"
          :aria-invalid="touched && !!amountError"
          required
          @blur="touched = true"
        />
      </div>
    </div>
    <small v-if="touched && (quantityError || amountError)" class="form-feedback is-invalid">
      {{ quantityError || amountError }}
    </small>
    <button type="submit" :disabled="loading" class="btn btn-primary">
      {{ loading ? 'Saving purchase…' : 'Add purchase' }}
    </button>
  </form>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { PackagePlus } from '@lucide/vue'

const props = defineProps({
  resource: { type: Object, required: true },
  loading: { type: Boolean, default: false },
})

const emit = defineEmits(['submit'])

const form = reactive({ containerName: '', quantity: 30, amount: 200 })
const touched = ref(false)

const quantityError = computed(() => {
  if (!form.quantity || form.quantity < 1) return 'Quantity must be at least 1.'
  return ''
})
const amountError = computed(() => {
  if (form.amount === '' || form.amount == null || form.amount < 0)
    return 'Total price cannot be negative.'
  return ''
})

const handleSubmit = () => {
  touched.value = true
  if (quantityError.value || amountError.value || props.loading) return
  emit('submit', {
    containerName: form.containerName,
    quantity: form.quantity,
    amount: form.amount,
  })
}

// Called by the parent once the purchase is actually saved.
const reset = () => {
  form.containerName = ''
  form.quantity = 30
  form.amount = 200
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

.stock-fields {
  display: grid;
  grid-template-columns: minmax(0, 1.5fr) minmax(90px, 0.6fr) minmax(110px, 0.7fr);
  gap: var(--spacing-sm);
}

.action-panel .form-group {
  margin-bottom: var(--spacing-md);
}

.action-panel .form-group input {
  padding: var(--input-padding);
}

.action-panel > .btn {
  align-self: flex-end;
}

@media (max-width: 520px) {
  .stock-fields {
    grid-template-columns: 1fr;
  }

  .action-panel > .btn {
    width: 100%;
  }
}
</style>
