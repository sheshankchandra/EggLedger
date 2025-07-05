<template>
  <div class="room-selection-container">
    <div class="header">
      <h2>Welcome back, {{ authStore.getUser?.name || 'User' }}!</h2>
      <p>Choose a room to enter or create/join a new one.</p>
    </div>

    <!-- User's Rooms -->
    <div class="rooms-section">
      <h3>Your Rooms</h3>
      <div v-if="authStore.getUserRooms.length === 0" class="no-rooms">
        <p>You're not in any rooms yet.</p>
        <button @click="showLobby = true" class="btn btn-primary">Create or Join Room</button>
      </div>
      <div v-else class="rooms-grid">
        <div
          v-for="room in authStore.getUserRooms"
          :key="room.roomId"
          class="room-card"
          @click="selectRoom(room)"
        >
          <div class="room-header">
            <h4>{{ room.roomName }}</h4>
            <span class="room-code">{{ room.roomCode }}</span>
          </div>
          <div class="room-info">
            <p><strong>Members:</strong> {{ room.memberCount || 0 }}</p>
            <p><strong>Created:</strong> {{ formatDate(room.createdAt) }}</p>
          </div>
          <div class="room-stats">
            <span class="stat"> 🥚 {{ room.totalEggs || 0 }} eggs </span>
            <span class="stat"> 📦 {{ room.containerCount || 0 }} containers </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Actions -->
    <div class="actions-section">
      <h3>Other Options</h3>
      <div class="actions-grid">
        <div @click="showLobby = true" class="action-card">
          <div class="action-icon">🚀</div>
          <h4>Create New Room</h4>
          <p>Start a fresh room for your group</p>
        </div>
        <div @click="showLobby = true" class="action-card">
          <div class="action-icon">🚪</div>
          <h4>Join Another Room</h4>
          <p>Enter a room with an invitation code</p>
        </div>
      </div>
    </div>

    <!-- Lobby Modal -->
    <div v-if="showLobby" class="lobby-modal">
      <div class="lobby-content">
        <div class="lobby-header">
          <h3>Create or Join Room</h3>
          <button @click="showLobby = false" class="close-btn">×</button>
        </div>

        <div class="lobby-options">
          <!-- Create Room -->
          <div class="lobby-section">
            <h4>🚀 Create New Room</h4>
            <form @submit.prevent="handleCreateRoom">
              <div class="form-group">
                <label>Room Name</label>
                <input
                  v-model="createForm.roomName"
                  type="text"
                  placeholder="e.g., The Eggvengers"
                  required
                />
              </div>
              <div class="form-group">
                <label>Visibility</label>
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
                    <input
                      type="radio"
                      v-model="createForm.isPublic"
                      :value="true"
                      name="visibility"
                    />
                    <span>🌍 Public</span>
                  </label>
                </div>
              </div>
              <button type="submit" :disabled="loading" class="btn btn-primary">
                {{ loading ? 'Creating...' : 'Create Room' }}
              </button>
            </form>
          </div>

          <!-- Join Room -->
          <div class="lobby-section">
            <h4>🚪 Join Existing Room</h4>
            <form @submit.prevent="handleJoinRoom">
              <div class="form-group">
                <label>6-Digit Room Code</label>
                <input
                  v-model="joinForm.roomCode"
                  type="text"
                  placeholder="e.g., 123456"
                  maxlength="6"
                  pattern="\d{6}"
                  required
                  class="room-code-input"
                />
              </div>
              <button type="submit" :disabled="loading" class="btn btn-primary">
                {{ loading ? 'Joining...' : 'Join Room' }}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>

    <!-- Notification -->
    <div v-if="notification.message" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import roomService from '@/services/room.service'

const emit = defineEmits(['room-selected', 'room-created'])

const authStore = useAuthStore()
const showLobby = ref(false)
const loading = ref(false)

let abortController = new AbortController()

// Fetch user rooms when component mounts
onMounted(async () => {
  await authStore.fetchUserRooms()
})

const createForm = reactive({
  roomName: '',
  isPublic: false,
})

const joinForm = reactive({
  roomCode: '',
})

const notification = reactive({
  message: '',
  type: 'success',
})

const selectRoom = (room) => {
  emit('room-selected', room)
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}

const showNotification = (message, type = 'success', duration = 5000) => {
  notification.message = message
  notification.type = type
  setTimeout(() => {
    notification.message = ''
  }, duration)
}

const handleCreateRoom = async () => {
  if (loading.value) return

  abortController.abort()
  abortController = new AbortController()
  loading.value = true

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
      await authStore.fetchUserRooms()

      // Create room object for emit
      const newRoom = {
        roomCode: response.value,
        roomName: createForm.roomName,
        isPublic: createForm.isPublic,
      }

      emit('room-created', newRoom)
      showLobby.value = false

      // Reset form
      createForm.roomName = ''
      createForm.isPublic = false
    } else {
      throw new Error(response.value || 'Failed to create room.')
    }
  } catch (error) {
    if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') return

    let errorMessage = 'Could not create the room. Please try again.'
    if (Array.isArray(error.response?.data)) {
      errorMessage = error.response.data.join(', ')
    } else if (typeof error.response?.data === 'string') {
      errorMessage = error.response.data
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    }
    showNotification(errorMessage, 'error')
  } finally {
    loading.value = false
  }
}

const handleJoinRoom = async () => {
  if (!/^\d{6}$/.test(joinForm.roomCode)) {
    showNotification('Please enter a valid 6-digit room code.', 'error')
    return
  }

  abortController.abort()
  abortController = new AbortController()
  loading.value = true

  try {
    const response = await roomService.joinRoom(joinForm.roomCode, abortController.signal)

    if (response.isSuccess) {
      showNotification('Joined room successfully!', 'success')
      await authStore.fetchUserRooms()

      // Find the joined room
      const joinedRoom = authStore.getUserRooms.find(
        (room) => room.roomCode === Number(joinForm.roomCode),
      )
      if (joinedRoom) {
        emit('room-selected', joinedRoom)
      }

      showLobby.value = false
      joinForm.roomCode = ''
    } else {
      throw new Error(response.value || 'Failed to join room.')
    }
  } catch (error) {
    if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') return

    let errorMessage = 'Could not join the room. Please check the code and try again.'
    if (Array.isArray(error.response?.data)) {
      errorMessage = error.response.data.join(', ')
    } else if (typeof error.response?.data === 'string') {
      errorMessage = error.response.data
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    }
    showNotification(errorMessage, 'error')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.room-selection-container {
  max-width: 1200px;
  margin: 2rem auto;
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 3rem;
}

.header h2 {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
  margin-bottom: 0.5rem;
}

.header p {
  font-size: 1.1rem;
  color: #666;
}

.rooms-section {
  margin-bottom: 3rem;
}

.rooms-section h3 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
  color: #333;
}

.no-rooms {
  text-align: center;
  padding: 2rem;
  background: #f8f9fa;
  border-radius: 8px;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.room-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.3s ease;
}

.room-card:hover {
  border-color: #4caf50;
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.15);
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.room-header h4 {
  margin: 0;
  color: #333;
  font-size: 1.2rem;
}

.room-code {
  background: #e3f2fd;
  color: #1976d2;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-family: monospace;
  font-size: 0.9rem;
}

.room-info {
  margin-bottom: 1rem;
}

.room-info p {
  margin: 0.25rem 0;
  color: #666;
  font-size: 0.9rem;
}

.room-stats {
  display: flex;
  gap: 1rem;
}

.stat {
  background: #f5f5f5;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  color: #555;
}

.actions-section h3 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
  color: #333;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.action-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  color: inherit;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.3s ease;
}

.action-card:hover {
  border-color: #ff9800;
  transform: translateY(-2px);
  text-decoration: none;
  color: inherit;
}

.action-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.action-card h4 {
  margin: 0 0 0.5rem 0;
  color: #333;
}

.action-card p {
  margin: 0;
  color: #666;
  font-size: 0.9rem;
}

.btn {
  display: inline-block;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
  border: none;
  cursor: pointer;
}

.btn-primary {
  background: #4caf50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
  text-decoration: none;
  color: white;
}

.btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

/* Lobby Modal Styles */
.lobby-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.lobby-content {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  width: 90%;
  max-width: 600px;
  max-height: 80vh;
  overflow-y: auto;
}

.lobby-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #666;
}

.lobby-options {
  display: grid;
  gap: 2rem;
}

.lobby-section {
  background: #f8f9fa;
  padding: 1.5rem;
  border-radius: 8px;
}

.lobby-section h4 {
  margin: 0 0 1rem 0;
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
  display: none;
}

.radio-group input[type='radio']:checked + span {
  color: white;
}

.radio-group label:has(input[value='false']:checked) {
  background-color: #5c6bc0;
}

.radio-group label:has(input[value='true']:checked) {
  background-color: #66bb6a;
}

.room-code-input {
  text-align: center;
  font-size: 1.2rem;
  letter-spacing: 0.5rem;
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
