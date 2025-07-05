<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <h3>Record Egg Consumption</h3>
      <h4>From: {{ container.name }}</h4>
      <form @submit.prevent="submitConsumption">
        <div class="form-group">
          <label for="quantity">How many did you eat?</label>
          <input type="number" v-model.number="form.quantityConsumed" min="1" :max="container.currentQuantity" required />
        </div>
        <div class="form-group">
          <label for="notes">Notes (optional)</label>
          <input type="text" v-model="form.notes" placeholder="e.g., Omelette for breakfast" />
        </div>
        <p v-if="error" class="error-message">{{ error }}</p>
        <div class="modal-actions">
          <button type="button" @click="$emit('close')" class="btn-cancel">Cancel</button>
          <button type="submit" :disabled="loading">{{ loading ? 'Saving...' : 'Record' }}</button>
        </div>
      </form>
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

<style scoped>
.modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); display: flex; justify-content: center; align-items: center; }
.modal-content { background: white; padding: 20px 30px; border-radius: 8px; width: 90%; max-width: 400px; }
.form-group { margin-bottom: 15px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
.btn-cancel { background-color: #6c757d; }
</style>
