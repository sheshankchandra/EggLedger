<template>
  <div class="room-selection-container">
    <div class="header">
      <div class="welcome-card">
        <h1>Welcome to EggLedger</h1>
        <p>Your smart roommate expense tracker</p>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="spinner-large"></div>
      <p>Loading your rooms...</p>
    </div>

    <!-- User's Rooms -->
    <div v-else class="rooms-section">
      <div class="section-header">
        <h2>Your Rooms</h2>
        <button @click="refreshRooms" class="btn-refresh" :disabled="loading">
          <svg class="refresh-icon" viewBox="0 0 24 24">
            <path fill="currentColor" d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
          </svg>
        </button>
      </div>

      <div v-if="userRooms.length === 0" class="no-rooms">
        <div class="empty-state">
          <div class="empty-icon">🏠</div>
          <h3>No rooms yet</h3>
          <p>Create your first room or join an existing one to get started!</p>
        </div>
      </div>

      <div v-else class="rooms-grid">
        <div
          v-for="room in userRooms"
          :key="room.roomId"
          class="room-card"
          @click="enterRoom(room)"
        >
          <div class="room-header">
            <h3>{{ room.roomName }}</h3>
            <div class="room-badges">
              <span class="room-code">{{ room.roomCode }}</span>
              <span v-if="room.adminUserId" class="admin-badge">Admin</span>
            </div>
          </div>
          <div class="room-footer">
            <span class="room-status" :class="{ 'public': room.isOpen, 'private': !room.isOpen }">
              {{ room.isOpen ? 'Public' : 'Private' }}
            </span>
            <button class="btn-enter">Enter Room</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="actions-section">
      <div class="actions-grid">
        <div class="action-card" @click="showCreateRoomModal = true">
          <div class="action-icon create">
            <svg viewBox="0 0 24 24">
              <path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
            </svg>
          </div>
          <h3>Create Room</h3>
          <p>Start a new room for your group</p>
        </div>

        <div class="action-card" @click="showJoinRoomModal = true">
          <div class="action-icon join">
            <svg viewBox="0 0 24 24">
              <path fill="currentColor" d="M10 17l5-5-5-5v10z"/>
            </svg>
          </div>
          <h3>Join Room</h3>
          <p>Enter an existing room with a code</p>
        </div>
      </div>
    </div>

    <!-- Create Room Modal -->
    <div v-if="showCreateRoomModal" class="modal-overlay" @click.self="closeModals">
      <div class="modal">
        <div class="modal-header">
          <h3>Create New Room</h3>
          <button @click="closeModals" class="btn-close">×</button>
        </div>
        <form @submit.prevent="createRoom" class="modal-form">
          <div class="form-group">
            <label for="roomName">Room Name</label>
            <input
              type="text"
              id="roomName"
              v-model="newRoom.roomName"
              class="form-input"
              placeholder="Enter room name..."
              required
            />
          </div>
          <div class="form-group">
            <label class="checkbox-label">
              <input
                type="checkbox"
                v-model="newRoom.isOpen"
                class="form-checkbox"
              />
              <span class="checkmark"></span>
              Make room public (anyone can join)
            </label>
          </div>
          <div class="modal-actions">
            <button type="button" @click="closeModals" class="btn-secondary">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="creatingRoom">
              <span v-if="creatingRoom" class="spinner"></span>
              {{ creatingRoom ? 'Creating...' : 'Create Room' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Join Room Modal -->
    <div v-if="showJoinRoomModal" class="modal-overlay" @click.self="closeModals">
      <div class="modal">
        <div class="modal-header">
          <h3>Join Room</h3>
          <button @click="closeModals" class="btn-close">×</button>
        </div>
        <form @submit.prevent="joinRoom" class="modal-form">
          <div class="form-group">
            <label for="roomCode">Room Code</label>
            <input
              type="number"
              id="roomCode"
              v-model="joinRoomCode"
              class="form-input"
              placeholder="Enter room code..."
              required
            />
          </div>
          <div class="modal-actions">
            <button type="button" @click="closeModals" class="btn-secondary">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="joiningRoom">
              <span v-if="joiningRoom" class="spinner"></span>
              {{ joiningRoom ? 'Joining...' : 'Join Room' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Error Toast -->
    <div v-if="error" class="toast error-toast">
      <span>{{ error }}</span>
      <button @click="error = null" class="toast-close">×</button>
    </div>

    <!-- Success Toast -->
    <div v-if="success" class="toast success-toast">
      <span>{{ success }}</span>
      <button @click="success = null" class="toast-close">×</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import roomService from '@/services/room.service'

const router = useRouter()

// Reactive data
const loading = ref(false)
const userRooms = ref([])
const error = ref(null)
const success = ref(null)

// Modal states
const showCreateRoomModal = ref(false)
const showJoinRoomModal = ref(false)

// Form data
const newRoom = ref({
  roomName: '',
  isOpen: false
})
const joinRoomCode = ref('')

// Loading states
const creatingRoom = ref(false)
const joiningRoom = ref(false)

// Methods
const loadUserRooms = async () => {
  loading.value = true
  try {
    const rooms = await roomService.getUserRooms()
    userRooms.value = rooms
  } catch (err) {
    error.value = 'Failed to load your rooms'
    console.error('Error loading rooms:', err)
  } finally {
    loading.value = false
  }
}

const refreshRooms = () => {
  loadUserRooms()
}

const createRoom = async () => {
  creatingRoom.value = true
  error.value = null
  try {
    await roomService.createRoom(newRoom.value)
    success.value = 'Room created successfully!'
    closeModals()
    resetForms()
    await loadUserRooms()
  } catch (err) {
    error.value = 'Failed to create room'
    console.error('Error creating room:', err)
  } finally {
    creatingRoom.value = false
  }
}

const joinRoom = async () => {
  joiningRoom.value = true
  error.value = null
  try {
    // Get user ID from somewhere (auth store, etc.)
    const userId = 'user-id-here' // TODO: Get from auth store
    await roomService.joinRoom({
      userId: userId,
      roomCode: parseInt(joinRoomCode.value)
    })
    success.value = 'Joined room successfully!'
    closeModals()
    resetForms()
    await loadUserRooms()
  } catch (err) {
    error.value = 'Failed to join room'
    console.error('Error joining room:', err)
  } finally {
    joiningRoom.value = false
  }
}

const enterRoom = (room) => {
  router.push(`/room/${room.roomCode}`)
}

const closeModals = () => {
  showCreateRoomModal.value = false
  showJoinRoomModal.value = false
}

const resetForms = () => {
  newRoom.value = {
    roomName: '',
    isOpen: false
  }
  joinRoomCode.value = ''
}

// Lifecycle
onMounted(() => {
  loadUserRooms()
})
</script>

<style scoped>
.room-selection-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 3rem;
}

.welcome-card {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 10px 30px rgba(0,0,0,0.1);
  display: inline-block;
}

.welcome-card h1 {
  font-size: 2.5rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.5rem 0;
}

.welcome-card p {
  color: #718096;
  font-size: 1.1rem;
  margin: 0;
}

.loading-state {
  text-align: center;
  padding: 3rem;
}

.spinner-large {
  width: 60px;
  height: 60px;
  border: 4px solid #e2e8f0;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem auto;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.rooms-section {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  margin-bottom: 2rem;
  box-shadow: 0 10px 30px rgba(0,0,0,0.1);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.section-header h2 {
  color: #1a202c;
  margin: 0;
}

.btn-refresh {
  background: none;
  border: 2px solid #e2e8f0;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.btn-refresh:hover {
  border-color: #667eea;
  color: #667eea;
}

.refresh-icon {
  width: 20px;
  height: 20px;
}

.no-rooms {
  text-align: center;
  padding: 3rem 2rem;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.empty-icon {
  font-size: 4rem;
  opacity: 0.5;
}

.empty-state h3 {
  color: #1a202c;
  margin: 0;
}

.empty-state p {
  color: #718096;
  margin: 0;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.room-card {
  background: #f7fafc;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.room-card:hover {
  transform: translateY(-5px);
  border-color: #667eea;
  box-shadow: 0 10px 25px rgba(102, 126, 234, 0.15);
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.room-header h3 {
  color: #1a202c;
  margin: 0;
  font-size: 1.25rem;
}

.room-badges {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}

.room-code {
  background: #667eea;
  color: white;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
}

.admin-badge {
  background: #48bb78;
  color: white;
  padding: 0.25rem 0.5rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}

.room-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.room-status {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
}

.room-status.public {
  background: #c6f6d5;
  color: #276749;
}

.room-status.private {
  background: #fed7d7;
  color: #c53030;
}

.btn-enter {
  background: #667eea;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-enter:hover {
  background: #5a67d8;
}

.actions-section {
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 10px 30px rgba(0,0,0,0.1);
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.action-card {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 2rem;
  border-radius: 12px;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s ease;
  text-decoration: none;
}

.action-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 15px 35px rgba(102, 126, 234, 0.3);
}

.action-icon {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: rgba(255,255,255,0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1rem auto;
}

.action-icon svg {
  width: 30px;
  height: 30px;
}

.action-card h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

.action-card p {
  margin: 0;
  opacity: 0.9;
  font-size: 0.875rem;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  padding: 2rem 2rem 0 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  color: #1a202c;
  font-size: 1.5rem;
}

.btn-close {
  background: none;
  border: none;
  font-size: 2rem;
  cursor: pointer;
  color: #718096;
  padding: 0;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-close:hover {
  color: #1a202c;
}

.modal-form {
  padding: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #374151;
}

.form-input {
  width: 100%;
  padding: 0.875rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: all 0.2s ease;
  box-sizing: border-box;
}

.form-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.checkbox-label {
  display: flex;
  align-items: center;
  cursor: pointer;
  font-weight: normal !important;
}

.form-checkbox {
  margin-right: 0.75rem;
  width: auto;
  padding: 0;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.btn-primary, .btn-secondary {
  padding: 0.875rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  border: none;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.btn-secondary {
  background: #e2e8f0;
  color: #4a5568;
}

.btn-secondary:hover {
  background: #cbd5e0;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

/* Toast Styles */
.toast {
  position: fixed;
  top: 2rem;
  right: 2rem;
  padding: 1rem 1.5rem;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 1rem;
  z-index: 1001;
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.error-toast {
  background: #fed7d7;
  color: #c53030;
  border: 1px solid #feb2b2;
}

.success-toast {
  background: #c6f6d5;
  color: #276749;
  border: 1px solid #9ae6b4;
}

.toast-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: currentColor;
  padding: 0;
}

@media (max-width: 768px) {
  .rooms-grid, .actions-grid {
    grid-template-columns: 1fr;
  }

  .room-selection-container {
    padding: 1rem;
  }

  .modal {
    margin: 1rem;
  }

  .toast {
    left: 1rem;
    right: 1rem;
    top: 1rem;
  }
}
</style>
