<template>
  <div class="container">
    <div class="header">
      <h1>Room Lobby</h1>
      <p>Create a new room or join one with a code.</p>
    </div>

    <div class="forms-grid">
      <!-- Create Room -->
      <div class="form-card">
        <h2>🚀 Create Room</h2>
        <form @submit.prevent="handleCreate">
          <div class="form-group">
            <label>Room Name</label>
            <input
              v-model="createForm.name"
              type="text"
              placeholder="e.g., The Egg Squad"
              required
            />
          </div>
          <div class="form-group">
            <label>Visibility</label>
            <div class="radio-group">
              <label class="radio-option">
                <input type="radio" v-model="createForm.isPublic" :value="false" />
                <span>🔒 Private</span>
              </label>
              <label class="radio-option">
                <input type="radio" v-model="createForm.isPublic" :value="true" />
                <span>🌍 Public</span>
              </label>
            </div>
          </div>
          <button type="submit" :disabled="loading" class="btn btn-success">
            {{ loading ? 'Creating...' : 'Create Room' }}
          </button>
        </form>
      </div>

      <!-- Join Room -->
      <div class="form-card">
        <h2>🚪 Join Room</h2>
        <form @submit.prevent="handleJoin">
          <div class="form-group">
            <label>6-Digit Room Code</label>
            <input
              v-model="joinForm.code"
              type="text"
              placeholder="123456"
              maxlength="6"
              pattern="\d{6}"
              class="code-input"
              required
            />
          </div>
          <button type="submit" :disabled="loading" class="btn btn-primary">
            {{ loading ? 'Joining...' : 'Join Room' }}
          </button>
        </form>
      </div>
    </div>

    <!-- Notification -->
    <div v-if="notification" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import roomService from '@/services/room.service'

const router = useRouter()
const authStore = useAuthStore()

// State
const loading = ref(false)
const notification = ref(null)

// Forms
const createForm = reactive({
  name: '',
  isPublic: false,
})

const joinForm = reactive({
  code: '',
})

// Methods
const showNotification = (message, type = 'success') => {
  notification.value = { message, type }
  setTimeout(() => {
    notification.value = null
  }, 4000)
}

const handleCreate = async () => {
  try {
    loading.value = true
    const roomData = await roomService.createRoom({
      roomName: createForm.name,
      isOpen: createForm.isPublic,
    })

    // Refresh user rooms
    await authStore.fetchUserRooms()

    // Set room data and navigate
    sessionStorage.setItem('eggLedgerRoomCode', roomData.code || roomData)
    router.push('/room')

    showNotification('Room created successfully!')
  } catch (error) {
    console.error('Failed to create room:', error)
    showNotification('Failed to create room', 'error')
  } finally {
    loading.value = false
  }
}

const handleJoin = async () => {
  try {
    loading.value = true
    await roomService.joinRoom({ roomCode: joinForm.code })

    // Refresh user rooms
    await authStore.fetchUserRooms()

    // Navigate to room
    sessionStorage.setItem('eggLedgerRoomCode', joinForm.code)
    router.push('/room')

    showNotification('Joined room successfully!')
  } catch (error) {
    console.error('Failed to join room:', error)
    showNotification('Failed to join room. Check the code and try again.', 'error')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 3rem;
}

.header h1 {
  font-size: 2.5rem;
  color: #333;
  margin-bottom: 0.5rem;
}

.header p {
  font-size: 1.2rem;
  color: #666;
}

.forms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
}

.form-card {
  background: white;
  padding: 2rem;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-top: 4px solid #4caf50;
}

.form-card h2 {
  margin: 0 0 1.5rem 0;
  text-align: center;
  color: #333;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #555;
}

.form-group input[type='text'] {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 1rem;
  box-sizing: border-box;
}

.code-input {
  text-align: center;
  font-size: 1.2rem;
  letter-spacing: 0.2rem;
  font-family: monospace;
}

.radio-group {
  display: flex;
  gap: 1rem;
  border: 1px solid #ddd;
  border-radius: 6px;
  padding: 0.5rem;
}

.radio-option {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.radio-option input {
  display: none;
}

.radio-option input:checked + span {
  color: white;
}

.radio-option:has(input:checked) {
  background: #4caf50;
}

.btn {
  width: 100%;
  padding: 0.75rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-primary {
  background: #2196f3;
  color: white;
}

.btn-primary:hover {
  background: #1976d2;
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
