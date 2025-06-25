<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <h3>Stock Up: {{ container.name }}</h3>
      <form @submit.prevent="submitStock">
        <div class="form-group">
          <label for="quantity">How many eggs are you adding?</label>
          <input type="number" v-model.number="form.quantity" min="1" required />
        </div>
        <div class="form-group">
          <label for="notes">Notes (optional)</label>
          <input type="text" v-model="form.notes" placeholder="e.g., Bought from Costco" />
        </div>
        <p v-if="error" class="error-message">{{ error }}</p>
        <div class="modal-actions">
          <button type="button" @click="$emit('close')" class="btn-cancel">Cancel</button>
          <button type="submit" :disabled="loading">{{ loading ? 'Saving...' : 'Add Stock' }}</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { orderService } from '@/services/order.service';

const props = defineProps({
  container: { type: Object, required: true }
});
const emit = defineEmits(['close', 'stocked']);

// This form matches the structure of your StockOrderDto
const form = reactive({
  containerId: props.container.containerId,
  quantity: 12,
  notes: ''
});

const loading = ref(false);
const error = ref(null);

const submitStock = async () => {
  loading.value = true;
  error.value = null;
  try {
    await orderService.stockOrder(form);
    emit('stocked');
  } catch (err) {
    error.value = 'Failed to add stock.';
    console.error(err);
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
/* You can reuse the styles from ConsumptionModal.vue */
.modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); display: flex; justify-content: center; align-items: center; }
.modal-content { background: white; padding: 20px 30px; border-radius: 8px; width: 90%; max-width: 400px; }
.form-group { margin-bottom: 15px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
.btn-cancel { background-color: #6c757d; }
</style>