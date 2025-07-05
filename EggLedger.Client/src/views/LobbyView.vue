<template>
  <div class="lobby-container">
    <div class="lobby-header">
      <h2>Welcome, {{ authStore.getUser?.name || 'User' }}!</h2>
      <p>Create a new room for your friends or join one with a code.</p>
    </div>

    <!-- Notification Area -->
    <div v-if="notification.message" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>

    <div class="options-wrapper">
      <!-- Option 1: Create a Room -->
      <div class="option-card">
        <h3>🚀 Create a New Room</h3>
        <form @submit.prevent="handleCreateRoom">
          <div class="form-group">
            <label for="room-name">Room Name</label>
            <input
              id="room-name"
              v-model="createForm.roomName"
              type="text"
              placeholder="e.g., The Eggvengers"
              required
            />
          </div>
          <div class="form-group">
            <label>Room Visibility</label>
            <div class="radio-group">
              <label>
                <input
                  type="radio"
                  v-model="createForm.isPublic"
                  :value="false"
                  name="visibility"
                />
                <span>🔒 Private</span>
              </label>
              <label>
                <input type="radio" v-model="createForm.isPublic" :value="true" name="visibility" />
                <span>🌍 Public</span>
              </label>
            </div>
          </div>
          <button type="submit" :disabled="isLoading">
            {{ isLoading ? 'Creating...' : 'Create Room' }}
          </button>
        </form>
      </div>

      <!-- Option 2: Join a Room -->
      <div class="option-card">
        <h3>🚪 Join an Existing Room</h3>
        <form @submit.prevent="handleJoinRoom">
          <div class="form-group">
            <label for="room-code">6-Digit Room Code</label>
            <input
              id="room-code"
              v-model="joinForm.roomCode"
              type="text"
              placeholder="e.g., 123456"
              maxlength="6"
              pattern="\d{6}"
              title="Please enter a 6-digit number."
              required
              class="room-code-input"
            />
          </div>
          <button type="submit" :disabled="isLoading">
            {{ isLoading ? 'Joining...' : 'Join Room' }}
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import roomService from '@/services/room.service'

// Simple AbortController for canceling requests
let abortController = new AbortController()

// Initialize stores and router
const authStore = useAuthStore()
const router = useRouter()

// Component State
const isLoading = ref(false)
const createForm = reactive({
  roomName: '',
  isPublic: false, // Default to private
})
const joinForm = reactive({
  roomCode: '',
})
const notification = reactive({
  message: '',
  type: 'success',
})

// --- Methods ---

const showNotification = (message, type = 'success', duration = 5000) => {
  notification.message = message
  notification.type = type
  setTimeout(() => {
    notification.message = ''
  }, duration)
}

const handleCreateRoom = async () => {
  if (isLoading.value) return

  // Cancel previous requests
  abortController.abort()
  abortController = new AbortController()

  isLoading.value = true

  try {
    const response = await roomService.createRoom(
      {
        roomName: createForm.roomName,
        isOpen: createForm.isPublic,
      },
      abortController.signal,
    )

    if (response.isSuccess) {
      showNotification('Room created successfully!', 'success')
      const newRoomCode = response.value
      await authStore.fetchUserRooms()
      sessionStorage.setItem('selectedRoomCode', newRoomCode)
      router.push('/room')
    } else {
      throw new Error(response.value || 'Failed to create room.')
    }
  } catch (error) {
    if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
      return
    }
    let errorMessage = 'Could not create the room. Please try again.'
    if (Array.isArray(error.response?.data)) {
      errorMessage = error.response.data.join(', ')
    } else if (typeof error.response?.data === 'string') {
      errorMessage = error.response.data
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    }
    console.error('Failed to create room:', errorMessage)
    showNotification(errorMessage, 'error')
  } finally {
    isLoading.value = false
  }
}

const handleJoinRoom = async () => {
  // Simple validation before proceeding
  if (!/^\d{6}$/.test(joinForm.roomCode)) {
    showNotification('Please enter a valid 6-digit room code.', 'error')
    return
  }

  // Cancel previous requests
  abortController.abort()
  abortController = new AbortController()

  try {
    const response = await roomService.joinRoom(joinForm.roomCode, abortController.signal)

    if (response.isSuccess) {
      showNotification('Joined created successfully!', 'success')
      const newRoomCode = response.value
      await authStore.fetchUserRooms()
      sessionStorage.setItem('selectedRoomCode', newRoomCode)
      router.push('/room')
    } else {
      throw new Error(response.value || 'Failed to create room.')
    }
  } catch (error) {
    if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
      return
    }
    let errorMessage = 'Could not join the room. Please check the code and try again.'
    if (Array.isArray(error.response?.data)) {
      errorMessage = error.response.data.join(', ')
    } else if (typeof error.response?.data === 'string') {
      errorMessage = error.response.data
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    }
    console.error('Failed to join room:', errorMessage)
    showNotification(errorMessage, 'error')
  } finally {
    isLoading.value = false
  }
}

// Cancel all requests when component unmounts (saves backend resources)
onUnmounted(() => {
  abortController.abort()
})
</script>

<style scoped>
.lobby-container {
  max-width: 900px;
  margin: 2rem auto;
  padding: 2rem;
}

.lobby-header {
  text-align: center;
  margin-bottom: 2rem;
}

.lobby-header h2 {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
}

.lobby-header p {
  font-size: 1.1rem;
  color: #666;
}

.options-wrapper {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

@media (min-width: 768px) {
  .options-wrapper {
    grid-template-columns: 1fr 1fr;
  }
}

.option-card {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-top: 4px solid #4caf50;
}

.option-card h3 {
  margin-top: 0;
  margin-bottom: 1.5rem;
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
  border: 1px solid #ccc;
  border-radius: 4px;
  box-sizing: border-box;
  font-size: 1rem;
}

.radio-group {
  display: flex;
  gap: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  padding: 0.5rem;
}

.radio-group label {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  transition:
    background-color 0.2s,
    color 0.2s;
}

.radio-group input[type='radio'] {
  display: none; /* Hide the actual radio button */
}

.radio-group input[type='radio']:checked + span {
  color: white;
}

.radio-group label:has(input[value='false']:checked) {
  background-color: #5c6bc0; /* Indigo for private */
}

.radio-group label:has(input[value='true']:checked) {
  background-color: #66bb6a; /* Green for public */
}

.room-code-input {
  text-align: center;
  font-size: 1.2rem;
  letter-spacing: 0.5rem;
}

button {
  width: 100%;
  padding: 0.8rem;
  border: none;
  border-radius: 4px;
  background-color: #4caf50;
  color: white;
  font-size: 1rem;
  font-weight: bold;
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
