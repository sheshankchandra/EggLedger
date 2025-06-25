<template>
  <div class="container-detail" v-if="container">
    <h2>Details for: {{ container.name }}</h2>
    <p><strong>Owner:</strong> {{ container.ownerName }}</p>
    <p><strong>Eggs Left:</strong> {{ container.currentQuantity }} / {{ container.initialQuantity }}</p>

    <div v-if="isOwner" class="top-actions">
        <button @click="showStockModal = true" class="btn-stock-up">Stock Up Eggs</button>
    </div>

    <h3>Consumption Log</h3>
    <div v-if="ordersLoading">Loading log...</div>
    <table v-else-if="orders.length > 0" class="log-table">
      <thead>
        <tr>
          <th>Who</th>
          <th>Type</th>
          <th>When</th>
          <th>Quantity</th>
          <th>Notes</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="order in orders" :key="order.orderId">
          <td>{{ order.userName }}</td>
          <td>
              <span :class="order.type === 'Consume' ? 'type-consume' : 'type-stock'">
                  {{ order.type }}
              </span>
          </td>
          <td>{{ new Date(order.orderDate).toLocaleString() }}</td>
          <td>{{ order.quantity }}</td>
          <td>{{ order.notes }}</td>
        </tr>
      </tbody>
    </table>
    <p v-else>No eggs have been consumed from this container yet.</p>
  </div>
  <div v-else-if="loading">Loading container details...</div>
  <div v-else class="error-message">Could not load container details.</div>
  <StockUpModal 
    v-if="showStockModal" 
    :container="container" 
    @close="showStockModal = false" 
    @stocked="handleStocked"
  />
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';
import { containerService } from '@/services/container.service';
import { orderService } from '@/services/order.service';
import StockUpModal from '@/components/StockUpModal.vue';

const route = useRoute();
const authStore = useAuthStore();

const container = ref(null);
const orders = ref([]);
const loading = ref(true);
const ordersLoading = ref(true);

const containerId = route.params.id;
const currentUserId = computed(() => authStore.user?.id);
const showStockModal = ref(false);
const isOwner = computed(() => container.value && authStore.user?.id === container.value.ownerId);

const handleStocked = () => {
    showStockModal.value = false;
    fetchDetails(); // Refresh all details after stocking up
}

const fetchDetails = async () => {
  try {
    const containerRes = await containerService.getContainerById(containerId);
    container.value = containerRes.data;

    const ordersRes = await orderService.getOrdersForContainer(containerId);
    // Add a temporary property for the settlement form
    orders.value = ordersRes.data.map(o => ({ ...o, amountToSettle: 0 }));
  } catch (err) {
    console.error("Failed to load details:", err);
  } finally {
    loading.value = false;
    ordersLoading.value = false;
  }
};

const settle = async (order) => {
  try {
    await orderService.settleOrder(order.id, { amountOwed: order.amountToSettle });
    // Refresh the data to show the "Settled" status
    await fetchDetails();
  } catch (err) {
    alert("Failed to settle order.");
    console.error(err);
  }
};

onMounted(fetchDetails);
</script>

<style scoped>
.container-detail { max-width: 900px; margin: auto; }
.log-table { width: 100%; border-collapse: collapse; margin-top: 20px; }
.log-table th, .log-table td { border: 1px solid #ddd; padding: 12px; text-align: left; }
.log-table th { background-color: #f2f2f2; }
.status-settled { color: green; font-weight: bold; }
.status-pending { color: orange; }
.top-actions { margin-bottom: 20px; }
.btn-stock-up { background-color: #17a2b8; color: white; padding: 10px 15px; border: none; border-radius: 5px; cursor: pointer; }
.type-consume { color: #dc3545; font-weight: bold; }
.type-stock { color: #28a745; font-weight: bold; }
</style>