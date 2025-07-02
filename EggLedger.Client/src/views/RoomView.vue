<template>
  <div class="room-container">
    <div v-if="loading" class="loading">
      <p>Loading room...</p>
    </div>

    <div v-else-if="!roomData" class="error">
      <h2>Room not found</h2>
      <p>The room you're trying to access doesn't exist or you don't have permission.</p>
      <router-link to="/room-selection" class="btn btn-primary">Back to Rooms</router-link>
    </div>

    <div v-else class="room-content">
      <!-- Room Header -->
      <div class="room-header">
        <div class="room-info">
          <h1>{{ roomData.roomName }}</h1>
          <span class="room-code">Code: {{ roomData.roomCode }}</span>
        </div>
        <div class="room-actions">
          <router-link to="/room-selection" class="btn btn-secondary">Back to Rooms</router-link>
        </div>
      </div>

      <!-- Room Stats -->
      <div class="room-stats">
        <div class="stat-card">
          <h3>🥚 Total Eggs</h3>
          <p class="stat-number">{{ roomData.totalEggs || 0 }}</p>
        </div>
        <div class="stat-card">
          <h3>📦 Containers</h3>
          <p class="stat-number">{{ roomData.containerCount || 0 }}</p>
        </div>
        <div class="stat-card">
          <h3>👥 Members</h3>
          <p class="stat-number">{{ roomData.memberCount || 0 }}</p>
        </div>
      </div>

      <!-- Action Cards -->
      <div class="actions-grid">
        <!-- Stock Eggs -->
        <div class="action-card">
          <h3>🛒 Stock Eggs</h3>
          <p>Add new eggs to the room</p>
          <form @submit.prevent="handleStock">
            <div class="form-group">
              <label>Container Name</label>
              <input v-model="stockForm.name" type="text" placeholder="e.g., Fresh Eggs" required />
            </div>
            <div class="form-group">
              <label>Quantity</label>
              <input v-model.number="stockForm.quantity" type="number" min="1" required />
            </div>
            <div class="form-group">
              <label>Price per container</label>
              <input v-model.number="stockForm.price" type="number" step="0.01" min="0" required />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-success">
              {{ loading ? 'Adding...' : 'Add Stock' }}
            </button>
          </form>
        </div>

        <!-- Consume Eggs -->
        <div class="action-card">
          <h3>🍳 Consume Eggs</h3>
          <p>Record eggs you've used</p>
          <form @submit.prevent="handleConsume">
            <div class="form-group">
              <label>Quantity</label>
              <input v-model.number="consumeForm.quantity" type="number" min="1" required />
            </div>
            <button type="submit" :disabled="loading" class="btn btn-primary">
              {{ loading ? 'Recording...' : 'Record Consumption' }}
            </button>
          </form>
        </div>
      </div>

      <!-- Notification -->
      <div v-if="notification" :class="['notification', notification.type]">
        {{ notification.message }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import roomService from '@/services/room.service'
import orderService from '@/services/order.service'
import apiClient from '@/services/api'

const router = useRouter()

// State
const roomData = ref(null)
const loading = ref(true)
const notification = ref(null)

// Forms
const stockForm = reactive({
  name: '',
  quantity: 30,
  price: 200,
})

const consumeForm = reactive({
  quantity: 1,
})

// Methods
const showNotification = (message, type = 'success') => {
  notification.value = { message, type }
  setTimeout(() => {
    notification.value = null
  }, 4000)
}

const loadRoomData = async () => {
  try {
    loading.value = true
    const selectedRoom = JSON.parse(sessionStorage.getItem('selectedRoom') || '{}')

    if (selectedRoom) {
      roomData.value = selectedRoom
    } else {
      // No room data available
      router.push('/room-selection')
      return
    }
  } catch (error) {
    console.error('Failed to load room data:', error)
    showNotification('Failed to load room data', 'error')
  } finally {
    loading.value = false
  }
}

const refreshRoomData = async () => {
  try {
    loading.value = true
    const updatedRoom = await roomService.getRoomByCode(roomData.value.roomCode)
    console.log('Update data:', updatedRoom)
    sessionStorage.setItem('selectedRoom', JSON.stringify(updatedRoom))
    sessionStorage.setItem('selectedRoomCode', updatedRoom.roomCode)
    if (updatedRoom) {
      roomData.value = updatedRoom
      console.log('Successfully updated room data', 'success')
    } else {
      showNotification('Failed to get updated room data', 'error')
      return
    }
  } catch (error) {
    console.error('Failed to get updated room data:', error)
    showNotification('Failed to get updated room data', 'error')
  } finally {
    loading.value = false
  }
}

const handleStock = async () => {
  try {
    loading.value = true
    const orderData = {
      name: stockForm.name,
      quantity: stockForm.quantity,
      price: stockForm.price,
    }

    console.log('Stocking order data:', orderData)
    await orderService.stockOrder(roomData.value.roomCode, orderData)
    console.log('Successfully stocked eggs:', orderData)

    showNotification(`Successfully added ${stockForm.quantity} eggs!`)

    // Reset form
    Object.assign(stockForm, { name: '', quantity: 30, price: 200 })

    // Refresh room data
    await refreshRoomData()
  } catch (error) {
    console.error('Failed to stock eggs:', error)
    showNotification('Failed to add eggs', 'error')
  } finally {
    loading.value = false
  }
}

const handleConsume = async () => {
  try {
    loading.value = true
    const payload = {
      quantity: consumeForm.quantity,
    }

    await apiClient.post(`/egg-ledger-api/${roomData.value.code}/orders/consume`, payload)
    showNotification(`Successfully consumed ${consumeForm.quantity} eggs!`)

    // Reset form
    consumeForm.quantity = 1

    // Refresh room data
    await loadRoomData()
  } catch (error) {
    console.error('Failed to consume eggs:', error)
    showNotification('Failed to record consumption', 'error')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadRoomData()
})

onUnmounted(() => {
  sessionStorage.removeItem('selectedRoom')
  sessionStorage.removeItem('selectedRoomCode')
})
</script>

<style scoped>
.room-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.loading,
.error {
  text-align: center;
  padding: 3rem;
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid #e0e0e0;
}

.room-info h1 {
  padding: 0.25rem 0rem 0.25rem 0rem;
  color: #333;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.9rem;
}

.room-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  text-align: center;
}

.stat-card h3 {
  margin: 0 0 0.5rem 0;
  color: #666;
  font-size: 1rem;
}

.stat-number {
  font-size: 2rem;
  font-weight: bold;
  color: #333;
  margin: 0;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
  margin-bottom: 2rem;
}

.action-card {
  background: white;
  padding: 2rem;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.action-card h3 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.action-card p {
  color: #666;
  margin: 0 0 1.5rem 0;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #555;
}

.form-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  box-sizing: border-box;
}

.btn {
  display: inline-block;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  text-align: center;
}

.btn-primary {
  background: #2196f3;
  color: white;
}

.btn-primary:hover {
  background: #1976d2;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #545b62;
}

.btn-success {
  background: #4caf50;
  color: white;
}

.btn-success:hover {
  background: #45a049;
}

.btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 1rem 1.5rem;
  border-radius: 6px;
  color: white;
  font-weight: 500;
  z-index: 1000;
}

.notification.success {
  background: #4caf50;
}

.notification.error {
  background: #f44336;
}
</style>
