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
            <label for="room-name" class="form-label">Room Name</label>
            <input
              id="room-name"
              v-model="createForm.roomName"
              type="text"
              placeholder="e.g., The Eggvengers"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label class="form-label">Room Visibility</label>
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
          <button type="submit" :disabled="isLoading" class="btn btn-primary w-full">
            {{ isLoading ? 'Creating...' : 'Create Room' }}
          </button>
        </form>
      </div>

      <!-- Option 2: Join a Room -->
      <div class="option-card">
        <h3>🚪 Join an Existing Room</h3>
        <form @submit.prevent="handleJoinRoom">
          <div class="form-group">
            <label for="room-code" class="form-label">6-Digit Room Code</label>
            <input
              id="room-code"
              v-model="joinForm.roomCode"
              type="text"
              placeholder="e.g., 123456"
              maxlength="6"
              pattern="\d{6}"
              title="Please enter a 6-digit number."
              class="form-input room-code-input"
              required
            />
          </div>
          <button type="submit" :disabled="isLoading" class="btn btn-primary w-full">
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
  margin: var(--spacing-xl) auto;
  padding: var(--spacing-xl);
}

.lobby-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.lobby-header h2 {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.lobby-header p {
  font-size: var(--font-size-lg);
  color: var(--text-secondary);
}

.options-wrapper {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--spacing-xl);
}

@media (min-width: 768px) {
  .options-wrapper {
    grid-template-columns: 1fr 1fr;
  }
}

.option-card {
  background: var(--bg-primary);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
  border-top: 4px solid var(--color-primary);
}

.option-card h3 {
  margin-top: 0;
  margin-bottom: var(--spacing-lg);
  text-align: center;
  color: var(--text-primary);
}

.form-group {
  margin-bottom: var(--spacing-lg);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-muted);
}

.form-group input[type='text'] {
  width: 100%;
  padding: var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  box-sizing: border-box;
  font-size: var(--font-size-base);
}

.radio-group {
  display: flex;
  gap: var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  padding: var(--spacing-sm);
}

.radio-group label {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-sm);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.radio-group input[type='radio'] {
  display: none; /* Hide the actual radio button */
}

.radio-group input[type='radio']:checked + span {
  color: var(--text-inverse);
}

.radio-group label:has(input[value='false']:checked) {
  background-color: var(--color-secondary); /* Indigo for private */
}

.radio-group label:has(input[value='true']:checked) {
  background-color: var(--color-success); /* Green for public */
}

.room-code-input {
  text-align: center;
  font-size: var(--font-size-lg);
  letter-spacing: 0.5rem;
}

.w-full {
  width: 100%;
}

.notification {
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  color: var(--text-inverse);
  margin-bottom: var(--spacing-lg);
  text-align: center;
}

.notification.success {
  background-color: var(--color-success);
}

.notification.error {
  background-color: var(--color-danger);
}
</style>
