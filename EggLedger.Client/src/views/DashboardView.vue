<template>
  <div class="dashboard-container">
    <h2>My Egg Containers</h2>

    <div class="add-container-form">
        <h3>Add New Container</h3>
        <!-- The form now matches the assumed fields of ContainerCreateDto -->
        <form @submit.prevent="addNewContainer">
        <input v-model="newContainer.name" placeholder="Container Name (e.g., 'Kitchen Fridge')" required />
        <input type="number" v-model.number="newContainer.capacity" placeholder="Total Capacity (e.g., 30)" required min="1" />
        <input type="number" v-model.number="newContainer.initialStock" placeholder="Starting Egg Count" required min="0" />
        <button type="submit" :disabled="loading">
            {{ loading ? 'Adding...' : '+ Add Container' }}
        </button>
        <p v-if="error" class="error-message">{{ error }}</p>
        </form>
    </div>

    <div class="container-list">
      <h3>Existing Containers</h3>
      <div v-if="containersLoading">Loading containers...</div>
      <div v-else-if="containers.length === 0">No containers found. Add one above!</div>
      <ul v-else>
        <li v-for="container in containers" :key="container.containerId" class="container-item">
          <div class="container-info">
            <strong>{{ container.name }}</strong>
            <span> - Eggs Left: {{ container.RemainingQuantity }} / {{ container.TotalQuantity }}</span>
          </div>
          <div class="container-actions">
            <router-link :to="`/container/${container.id}`" class="btn-details">View Details</router-link>
            <button @click="openConsumptionModal(container)" class="btn-consume">Eat Eggs</button>
          </div>
        </li>
      </ul>
    </div>

    <!-- Consumption Modal -->
    <ConsumptionModal 
      v-if="showModal" 
      :container="selectedContainer" 
      @close="closeConsumptionModal" 
      @consumed="handleConsumption"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue';
import { containerService } from '@/services/container.service';
import ConsumptionModal from '@/components/ConsumptionModal.vue';

const containers = ref([]);
const containersLoading = ref(true);
const loading = ref(false);
const error = ref(null);

const newContainer = reactive({
  containerName: '',
  totalQuantity: 30,
  amount : 200,
});

const showModal = ref(false);
const selectedContainer = ref(null);

const fetchContainers = async () => {
  containersLoading.value = true;
  try {
    const response = await containerService.getContainers();
    containers.value = response.data;
  } catch (err) {
    console.error("Failed to fetch containers:", err);
    error.value = "Could not load containers.";
  } finally {
    containersLoading.value = false;
  }
};

const addNewContainer = async () => {
  loading.value = true;
  error.value = null;
  try {
    await containerService.createContainer(newContainer);
    newContainer.containerName = ''; // Reset form
    newContainer.totalQuantity = 30;
    newContainer.amount = 200;
    await fetchContainers(); // Refresh the list
  } catch (err) {
    error.value = "Failed to add container.";
    console.error(err);
  } finally {
    loading.value = false;
  }
};

const openConsumptionModal = (container) => {
  selectedContainer.value = container;
  showModal.value = true;
};

const closeConsumptionModal = () => {
  showModal.value = false;
  selectedContainer.value = null;
};

const handleConsumption = () => {
  closeConsumptionModal();
  fetchContainers(); // Refresh the list to show updated quantity
};

onMounted(fetchContainers);
</script>

<style scoped>
.dashboard-container { max-width: 800px; margin: auto; }
.add-container-form { background: #f9f9f9; padding: 20px; border-radius: 8px; margin-bottom: 30px; }
.add-container-form form { display: flex; gap: 10px; align-items: center; }
.container-list ul { list-style: none; padding: 0; }
.container-item { display: flex; justify-content: space-between; align-items: center; padding: 15px; border: 1px solid #ddd; border-radius: 8px; margin-bottom: 10px; }
.container-actions { display: flex; gap: 10px; }
.btn-details, .btn-consume { padding: 8px 12px; text-decoration: none; border-radius: 5px; color: white; border: none; cursor: pointer; }
.btn-details { background-color: #007bff; }
.btn-consume { background-color: #28a745; }
.error-message { color: red; }
</style>