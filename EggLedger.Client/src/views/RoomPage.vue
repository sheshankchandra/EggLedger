<template>
  <div class="room-container">
    <!-- View 1: Join Room -->
    <div v-if="!roomCode" class="join-room-card">
      <h2>Join an Egg-Ledger Room</h2>
      <p>Enter the 6-digit code for your room to start tracking.</p>
      <form @submit.prevent="joinRoom">
        <input
          v-model="enteredRoomCode"
          type="text"
          placeholder="e.g., 123456"
          maxlength="6"
          class="room-code-input"
          pattern="\d{6}"
          title="Please enter a 6-digit number."
          required
        />
        <button type="submit" :disabled="!isRoomCodeValid">Join Room</button>
      </form>
    </div>

    <!-- View 2: In the Room -->
    <div v-else class="in-room-view">
      <div class="room-header">
        <h2>Room: {{ roomCode }}</h2>
        <button @click="leaveRoom" class="leave-button">Leave Room</button>
      </div>

      <!-- Notification Area -->
      <div v-if="notification.message" :class="['notification', notification.type]">
        {{ notification.message }}
      </div>

      <div class="actions-wrapper">
        <!-- Stock Eggs Card -->
        <div class="action-card">
          <h3>🛒 Stock Eggs</h3>
          <p>Bought new eggs? Log them here.</p>
          <form @submit.prevent="handleStockOrder">
            <div class="form-group">
              <label for="stock-quantity">Quantity (eggs)</label>
              <input
                id="stock-quantity"
                v-model.number="stockForm.quantity"
                type="number"
                min="1"
                required
              />
            </div>
            <div class="form-group">
              <label for="stock-price">Total Price ($)</label>
              <input
                id="stock-price"
                v-model.number="stockForm.price"
                type="number"
                min="0"
                step="0.01"
                required
              />
            </div>
            <button type="submit" :disabled="isLoading">
              {{ isLoading ? 'Stocking...' : 'Add to Ledger' }}
            </button>
          </form>
        </div>

        <!-- Consume Eggs Card -->
        <div class="action-card">
          <h3>🍳 Consume Eggs</h3>
          <p>Used some eggs? Log them here.</p>
          <form @submit.prevent="handleConsumeOrder">
            <div class="form-group">
              <label for="consume-quantity">Quantity (eggs)</label>
              <input
                id="consume-quantity"
                v-model.number="consumeForm.quantity"
                type="number"
                min="1"
                required
              />
            </div>
            <button type="submit" :disabled="isLoading">
              {{ isLoading ? 'Consuming...' : 'Update Ledger' }}
            </button>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import apiClient from '@/services/api'

// --- State Management ---
const roomCode = ref(sessionStorage.getItem('eggLedgerRoomCode') || null)
const enteredRoomCode = ref('')
const isLoading = ref(false)

const stockForm = reactive({
  quantity: 12, // Default to a dozen
  price: 5.0,
})

const consumeForm = reactive({
  quantity: 2,
})

const notification = reactive({
  message: '',
  type: 'success', // 'success' or 'error'
})

// --- Computed Properties ---
const isRoomCodeValid = computed(() => /^\d{6}$/.test(enteredRoomCode.value))

// --- Methods ---
const joinRoom = () => {
  if (isRoomCodeValid.value) {
    roomCode.value = enteredRoomCode.value
    // Persist in sessionStorage to survive page reloads
    sessionStorage.setItem('eggLedgerRoomCode', roomCode.value)
    enteredRoomCode.value = ''
  }
}

const leaveRoom = () => {
  roomCode.value = null
  sessionStorage.removeItem('eggLedgerRoomCode')
}

const showNotification = (message, type = 'success', duration = 4000) => {
  notification.message = message
  notification.type = type
  setTimeout(() => {
    notification.message = ''
  }, duration)
}

const handleStockOrder = async () => {
  if (isLoading.value) return
  isLoading.value = true

  // This structure matches your 'StockOrderDto' based on common practice.
  // Adjust if your DTO has different property names.
  const payload = {
    quantity: stockForm.quantity,
    price: stockForm.price,
  }

  try {
    const response = await apiClient.post(`/egg-ledger-api/${roomCode.value}/orders/stock`, payload)
    showNotification(`Successfully stocked ${response.data.quantity} eggs!`, 'success')
    // Reset form
    stockForm.quantity = 12
    stockForm.price = 5.0
  } catch (error) {
    console.error('Failed to create stock order:', error)
    const errorMessage =
      error.response?.data?.[0] ||
      'Failed to stock eggs. The room might not exist or an error occurred.'
    showNotification(errorMessage, 'error')
  } finally {
    isLoading.value = false
  }
}

const handleConsumeOrder = async () => {
  if (isLoading.value) return
  isLoading.value = true

  // This structure matches your 'ConsumeOrderDto'.
  const payload = {
    quantity: consumeForm.quantity,
  }

  try {
    const response = await apiClient.post(
      `/egg-ledger-api/${roomCode.value}/orders/consume`,
      payload,
    )
    showNotification(`Successfully consumed ${response.data.quantity} eggs!`, 'success')
    // Reset form
    consumeForm.quantity = 2
  } catch (error) {
    console.error('Failed to create consume order:', error)
    const errorMessage =
      error.response?.data?.[0] || 'Failed to consume eggs. Not enough eggs in stock?'
    showNotification(errorMessage, 'error')
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.room-container {
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding: 2rem;
  height: 100%;
  background-color: #f4f7f6;
}

.join-room-card {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  text-align: center;
  max-width: 400px;
}

.room-code-input {
  font-size: 1.5rem;
  padding: 0.5rem;
  width: 100%;
  text-align: center;
  letter-spacing: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  margin: 1rem 0;
}

.in-room-view {
  width: 100%;
  max-width: 900px;
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.leave-button {
  background-color: #f44336;
  color: white;
  border: none;
}
.leave-button:hover {
  background-color: #d32f2f;
}

.actions-wrapper {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
}

.action-card {
  background: white;
  padding: 1.5rem 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.action-card h3 {
  margin-top: 0;
  color: #333;
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
  border: 1px solid #ccc;
  border-radius: 4px;
  box-sizing: border-box; /* Important for padding */
  font-size: 1rem;
}

button {
  width: 100%;
  padding: 0.75rem;
  border: none;
  border-radius: 4px;
  background-color: #4caf50; /* A nice green */
  color: white;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.2s;
}

button:disabled {
  background-color: #cccccc;
  cursor: not-allowed;
}

button:not(:disabled):hover {
  background-color: #45a049;
}

/* Notification Styling */
.notification {
  padding: 1rem;
  border-radius: 4px;
  color: white;
  margin-bottom: 1.5rem;
  text-align: center;
}
.notification.success {
  background-color: #28a745;
}
.notification.error {
  background-color: #dc3545;
}
</style>
