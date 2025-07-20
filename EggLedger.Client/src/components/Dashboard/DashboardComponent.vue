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
          @click="selectRoom(room.roomCode)"
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
    <div v-if="showLobby" class="modal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>Create or Join Room</h3>
          <button @click="showLobby = false" class="close-btn">×</button>
        </div>

        <div class="modal-body">
          <div class="lobby-options">
            <!-- Create Room -->
            <div class="lobby-section">
              <h4>🚀 Create New Room</h4>
              <form @submit.prevent="handleCreateRoom">
                <div class="form-group">
                  <label class="form-label">Room Name</label>
                  <input
                    v-model="createForm.roomName"
                    type="text"
                    placeholder="e.g., The Eggvengers"
                    class="form-input"
                    required
                  />
                </div>
                <div class="form-group">
                  <label class="form-label">Visibility</label>
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
                  <label class="form-label">6-Digit Room Code</label>
                  <input
                    v-model="joinForm.roomCode"
                    type="text"
                    placeholder="e.g., 123456"
                    maxlength="6"
                    pattern="\d{6}"
                    required
                    class="form-input room-code-input"
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

const emit = defineEmits(['room-selected'])

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
      selectRoom(response.value)
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
      selectRoom(joinForm.roomCode)

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
  max-width: var(--container-max-width);
  margin: var(--spacing-xl) auto;
  padding: var(--spacing-xl);
}

.header {
  text-align: center;
  margin-bottom: var(--spacing-2xl);
}

.header h2 {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--spacing-sm);
}

.header p {
  font-size: var(--font-size-lg);
  color: var(--text-secondary);
}

.rooms-section {
  margin-bottom: var(--spacing-2xl);
}

.rooms-section h3 {
  font-size: var(--font-size-2xl);
  margin-bottom: var(--spacing-lg);
  color: var(--text-primary);
}

.no-rooms {
  text-align: center;
  padding: var(--spacing-xl);
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-lg);
}

.room-card {
  background: var(--bg-primary);
  border-radius: var(--radius-xl);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-md);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all var(--transition-slow);
}

.room-card:hover {
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.room-header h4 {
  margin: 0;
  color: var(--text-primary);
  font-size: var(--font-size-xl);
}

.room-code {
  background: var(--color-primary-light);
  color: var(--color-secondary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm);
}

.room-info {
  margin-bottom: var(--spacing-md);
}

.room-info p {
  margin: var(--spacing-xs) 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.room-stats {
  display: flex;
  gap: var(--spacing-md);
}

.stat {
  background: var(--bg-tertiary);
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  color: var(--text-muted);
}

.actions-section h3 {
  font-size: var(--font-size-2xl);
  margin-bottom: var(--spacing-lg);
  color: var(--text-primary);
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-lg);
}

.action-card {
  background: var(--bg-primary);
  border-radius: var(--radius-xl);
  padding: var(--spacing-xl);
  text-align: center;
  color: inherit;
  box-shadow: var(--shadow-md);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all var(--transition-slow);
}

.action-card:hover {
  border-color: var(--color-warning);
  transform: translateY(-2px);
  text-decoration: none;
  color: inherit;
}

.action-icon {
  font-size: var(--font-size-3xl);
  margin-bottom: var(--spacing-md);
}

.action-card h4 {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--text-primary);
}

.action-card p {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.lobby-options {
  display: grid;
  gap: var(--spacing-xl);
}

.lobby-section {
  background: var(--bg-tertiary);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
}

.lobby-section h4 {
  margin: 0 0 var(--spacing-md) 0;
  color: var(--text-primary);
}

.form-group {
  margin-bottom: var(--spacing-md);
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
  font-size: var(--font-size-lg);
  background: var(--bg-primary);
}

.radio-group {
  display: flex;
  gap: var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  padding: var(--spacing-xs);
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
  display: none;
}

.radio-group input[type='radio']:checked + span {
  color: white;
}

.radio-group label:has(input[value='false']:checked) {
  background-color: var(--color-secondary);
}

.radio-group label:has(input[value='true']:checked) {
  background-color: var(--color-success);
}

.room-code-input {
  text-align: center;
  font-size: var(--font-size-lg);
  letter-spacing: 0.5rem;
}

.notification {
  position: fixed;
  top: var(--spacing-md);
  right: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  border-radius: var(--radius-md);
  color: white;
  font-weight: var(--font-weight-semibold);
  z-index: 1000;
}

.notification.success {
  background: var(--color-success);
}

.notification.error {
  background: var(--color-danger);
}
</style>
