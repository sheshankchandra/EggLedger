<template>
  <div class="modal" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h3 class="modal-title">Record Egg Consumption</h3>
        <button @click="$emit('close')" class="close-btn">×</button>
      </div>
      <div class="modal-body">
        <h4 class="mb-3">From: {{ container.name }}</h4>
        <form @submit.prevent="submitConsumption">
          <div class="form-group">
            <label for="quantity" class="form-label">How many did you eat?</label>
            <input
              type="number"
              v-model.number="form.quantityConsumed"
              min="1"
              :max="container.currentQuantity"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="notes" class="form-label">Notes (optional)</label>
            <input
              type="text"
              v-model="form.notes"
              placeholder="e.g., Omelette for breakfast"
              class="form-input"
            />
          </div>
          <div v-if="error" class="alert alert-error">{{ error }}</div>
          <div class="modal-footer">
            <button type="button" @click="$emit('close')" class="btn btn-secondary">Cancel</button>
            <button type="submit" :disabled="loading" class="btn btn-primary">
              {{ loading ? 'Saving...' : 'Record' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onUnmounted } from 'vue';
import { orderService } from '@/services/order.service';

// Simple AbortController for canceling requests
let abortController = new AbortController();

const props = defineProps({
  container: {
    type: Object,
    required: true
  }
});

const emit = defineEmits(['close', 'consumed']);

const form = reactive({
  containerId: props.container.containerId,
  quantityConsumed: 1,
  notes: ''
});

const loading = ref(false);
const error = ref(null);

const submitConsumption = async () => {
  // Cancel previous requests
  abortController.abort();
  abortController = new AbortController();

  loading.value = true;
  error.value = null;
  try {
    await orderService.consumeOrder(form, abortController.signal);
    emit('consumed');
  } catch (err) {
    if (err.name === 'AbortError' || err.code === 'ERR_CANCELED') {
      return;
    }
    error.value = 'Failed to record consumption.';
    console.error(err);
  } finally {
    loading.value = false;
  }
};

// Cancel all requests when component unmounts (saves backend resources)
onUnmounted(() => {
  abortController.abort();
});
</script>


