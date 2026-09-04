<template>
  <section class="dashboard">
    <header class="dashboard-hero">
      <div>
        <p class="eyebrow">{{ isNewUser ? 'Welcome to EggLedger' : 'Good to see you again' }}</p>
        <h1>{{ greeting }}, {{ firstName }}</h1>
        <p>Choose a room to manage shared stock, or create a new space for your household.</p>
      </div>
      <button @click="openLobby('create')" class="btn btn-primary" type="button">
        <span aria-hidden="true">＋</span>
        New room
      </button>
    </header>

    <section aria-labelledby="rooms-heading">
      <div class="section-heading">
        <div>
          <h2 id="rooms-heading">Your rooms</h2>
          <p>{{ roomSummary }}</p>
        </div>
        <button @click="openLobby('join')" class="text-button" type="button">
          Join with a code
        </button>
      </div>

      <LoadingSkeleton
        v-if="roomStore.isLoading"
        :count="3"
        height="260px"
        aria-label="Loading rooms"
      />

      <EmptyState
        v-else-if="rooms.length === 0"
        icon="⌂"
        title="Create your first shared room"
        description="Invite your household, track purchases, and keep shared stock visible to everyone."
      >
        <template #actions>
          <button @click="openLobby('create')" class="btn btn-primary" type="button">
            Create a room
          </button>
          <button @click="openLobby('join')" class="btn btn-secondary" type="button">
            Join a room
          </button>
        </template>
      </EmptyState>

      <div v-else class="rooms-grid">
        <RoomCard
          v-for="room in rooms"
          :key="room.roomId"
          :room="room"
          :resource="resource"
          :date-label="formatDate(room.createdAt)"
          @select="selectRoom"
        />
      </div>
    </section>

    <Modal
      v-if="showLobby"
      title="Create or join a room"
      content-class="lobby-modal"
      :closable="!loading"
      :close-on-overlay-click="!loading"
      @close="closeLobby"
    >
      <template #eyebrow>Room setup</template>

      <div class="lobby-tabs" role="tablist" aria-label="Room setup method">
        <button
          :class="{ active: lobbyMode === 'create' }"
          type="button"
          role="tab"
          :aria-selected="lobbyMode === 'create'"
          @click="lobbyMode = 'create'"
        >
          Create room
        </button>
        <button
          :class="{ active: lobbyMode === 'join' }"
          type="button"
          role="tab"
          :aria-selected="lobbyMode === 'join'"
          @click="lobbyMode = 'join'"
        >
          Join room
        </button>
      </div>

      <form v-if="lobbyMode === 'create'" @submit.prevent="handleCreateRoom" novalidate>
        <div class="form-group">
          <label for="room-name" class="form-label">Room name</label>
          <input
            id="room-name"
            v-model.trim="createForm.roomName"
            type="text"
            maxlength="80"
            placeholder="For example, Green Street Flat"
            class="form-input"
            :class="{ 'is-invalid': roomNameError }"
            :aria-invalid="!!roomNameError"
            aria-describedby="room-name-feedback"
            required
            @blur="roomNameTouched = true"
          />
          <small v-if="roomNameError" id="room-name-feedback" class="form-feedback is-invalid">
            {{ roomNameError }}
          </small>
          <small v-else class="field-hint">Use a name your household will recognize.</small>
        </div>
        <fieldset class="form-group">
          <legend class="form-label">Who can join?</legend>
          <div class="visibility-options">
            <label :class="{ selected: !createForm.isPublic }">
              <input v-model="createForm.isPublic" type="radio" :value="false" />
              <span><strong>Private</strong><small>Only people with the code</small></span>
            </label>
            <label :class="{ selected: createForm.isPublic }">
              <input v-model="createForm.isPublic" type="radio" :value="true" />
              <span><strong>Open</strong><small>Discoverable to others</small></span>
            </label>
          </div>
        </fieldset>
        <button type="submit" :disabled="loading" class="btn btn-primary submit-button">
          {{ loading ? 'Creating room…' : 'Create room' }}
        </button>
      </form>

      <form v-else @submit.prevent="handleJoinRoom" novalidate>
        <div class="form-group">
          <label for="room-code" class="form-label">Six-digit room code</label>
          <input
            id="room-code"
            v-model.trim="joinForm.roomCode"
            type="text"
            inputmode="numeric"
            autocomplete="one-time-code"
            placeholder="000000"
            maxlength="6"
            pattern="\d{6}"
            required
            class="form-input room-code-input"
            :class="{ 'is-invalid': roomCodeError }"
            :aria-invalid="!!roomCodeError"
            aria-describedby="room-code-feedback"
            @blur="roomCodeTouched = true"
          />
          <small v-if="roomCodeError" id="room-code-feedback" class="form-feedback is-invalid">
            {{ roomCodeError }}
          </small>
          <small v-else class="field-hint">Ask a room member to share the code with you.</small>
        </div>
        <button type="submit" :disabled="loading" class="btn btn-primary submit-button">
          {{ loading ? 'Joining room…' : 'Join room' }}
        </button>
      </form>
    </Modal>

    <Toast :notification="notification" />
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { resourceConfig as resource } from '@/config/resource.config'
import { useAuthStore } from '@/stores/auth.store'
import { useRoomStore } from '@/stores/room.store'
import { useNotification } from '@/composables/useNotification'
import { errorMessage, isCanceled } from '@/utils/httpError'
import Modal from '@/components/common/BaseModal.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import Toast from '@/components/common/ToastNotification.vue'
import RoomCard from '@/components/common/RoomCard.vue'

const emit = defineEmits(['room-selected'])
const authStore = useAuthStore()
const roomStore = useRoomStore()
const { notification, showNotification } = useNotification(5000)
const showLobby = ref(false)
const lobbyMode = ref('create')
const loading = ref(false)
const isNewUser = ref(false)
let abortController = new AbortController()

const rooms = computed(() => roomStore.userRooms)
const firstName = computed(() => authStore.getUser?.name?.trim().split(/\s+/)[0] || 'there')
const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 18) return 'Good afternoon'
  return 'Good evening'
})
const roomSummary = computed(() => {
  if (rooms.value.length === 0) return 'Your shared spaces will appear here.'
  return `${rooms.value.length} ${rooms.value.length === 1 ? 'room' : 'rooms'} ready to open`
})

const createForm = reactive({ roomName: '', isPublic: false })
const joinForm = reactive({ roomCode: '' })

// Real-time field validation state
const roomNameTouched = ref(false)
const roomCodeTouched = ref(false)
const roomNameError = computed(() => {
  if (!roomNameTouched.value) return ''
  if (!createForm.roomName.trim()) return 'Give your room a name.'
  return ''
})
const roomCodeError = computed(() => {
  if (!roomCodeTouched.value) return ''
  if (!/^\d{6}$/.test(joinForm.roomCode)) return 'Enter a valid six-digit room code.'
  return ''
})

onMounted(async () => {
  await roomStore.fetchUserRooms()
  isNewUser.value = authStore.getIsNewUser
})

const openLobby = (mode) => {
  lobbyMode.value = mode
  showLobby.value = true
}

const closeLobby = () => {
  if (!loading.value) showLobby.value = false
}

const selectRoom = (roomCode) => emit('room-selected', roomCode)

const formatDate = (dateString) => {
  if (!dateString) return 'Recently created'
  const date = new Date(dateString)
  if (Number.isNaN(date.getTime())) return 'Recently created'
  return new Intl.DateTimeFormat(undefined, { month: 'short', year: 'numeric' }).format(date)
}

const startRequest = () => {
  abortController.abort()
  abortController = new AbortController()
  loading.value = true
  return abortController.signal
}

const handleCreateRoom = async () => {
  roomNameTouched.value = true
  if (loading.value || roomNameError.value) return
  const signal = startRequest()
  try {
    const response = await roomStore.createRoom(
      { roomName: createForm.roomName, isOpen: createForm.isPublic },
      signal,
    )
    if (!response.isSuccess) throw new Error(response.value || 'Failed to create room.')
    showNotification('Room created successfully.')
    selectRoom(response.value.roomCode ?? response.value)
    showLobby.value = false
    createForm.roomName = ''
    createForm.isPublic = false
    roomNameTouched.value = false
  } catch (error) {
    if (isCanceled(error)) return
    showNotification(errorMessage(error, 'Could not create the room. Please try again.'), 'error')
  } finally {
    loading.value = false
  }
}

const handleJoinRoom = async () => {
  roomCodeTouched.value = true
  if (roomCodeError.value) return
  const signal = startRequest()
  try {
    const response = await roomStore.joinRoom(joinForm.roomCode, signal)
    if (!response.isSuccess) throw new Error(response.value || 'Failed to join room.')
    showNotification('You joined the room.')
    selectRoom(joinForm.roomCode)
    showLobby.value = false
    joinForm.roomCode = ''
    roomCodeTouched.value = false
  } catch (error) {
    if (isCanceled(error)) return
    showNotification(
      errorMessage(error, 'Could not join the room. Check the code and try again.'),
      'error',
    )
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.dashboard {
  display: grid;
  gap: var(--spacing-2xl);
}

.dashboard-hero,
.section-heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
}

.dashboard-hero {
  padding: var(--spacing-xl);
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--color-primary) 55%, black),
    var(--color-primary)
  );
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-lg);
  color: var(--text-inverse);
}

.dashboard-hero h1 {
  margin-bottom: var(--spacing-sm);
  color: inherit;
  font-size: clamp(2rem, 4vw, 3rem);
  letter-spacing: -0.04em;
}

.dashboard-hero p:not(.eyebrow) {
  max-width: 650px;
  margin: 0;
  color: rgba(255, 255, 255, 0.78);
}

.dashboard-hero .eyebrow {
  color: #b8ead8;
}

.dashboard-hero .btn {
  flex-shrink: 0;
  background: var(--color-white);
  color: var(--color-primary);
}

.section-heading {
  margin-bottom: var(--spacing-lg);
}

.section-heading h2 {
  margin-bottom: var(--spacing-xs);
}

.section-heading p {
  margin: 0;
}

.text-button {
  border: 0;
  background: transparent;
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 290px), 1fr));
  gap: var(--spacing-lg);
}

.lobby-modal {
  max-width: 560px;
}

.lobby-tabs {
  display: grid;
  grid-template-columns: 1fr 1fr;
  margin: var(--spacing-md) var(--spacing-lg) 0;
  padding: var(--spacing-xs);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
}

.lobby-tabs button {
  padding: var(--spacing-sm);
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

.lobby-tabs button.active {
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
  color: var(--color-primary);
}

.field-hint {
  display: block;
  margin-top: var(--spacing-xs);
  color: var(--text-muted);
}

.visibility-options {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-sm);
}

.visibility-options label {
  display: flex;
  padding: var(--spacing-md);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  cursor: pointer;
}

.visibility-options label.selected {
  border-color: var(--color-primary);
  background: var(--color-primary-light);
}

.visibility-options input {
  margin-right: var(--spacing-sm);
}

.visibility-options span,
.visibility-options small {
  display: block;
}

.visibility-options small {
  margin-top: var(--spacing-xs);
  color: var(--text-muted);
}

.room-code-input {
  text-align: center;
  font-family: var(--font-family-mono);
  font-size: var(--font-size-2xl);
  letter-spacing: 0.35em;
}

.submit-button {
  width: 100%;
}

@media (max-width: 640px) {
  .dashboard {
    gap: var(--spacing-xl);
  }

  .dashboard-hero,
  .section-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .dashboard-hero {
    padding: var(--spacing-lg);
  }

  .dashboard-hero .btn {
    width: 100%;
  }

  .section-heading {
    gap: var(--spacing-sm);
  }

  .text-button {
    align-self: flex-start;
    padding: 0;
  }

  .visibility-options {
    grid-template-columns: 1fr;
    flex-direction: column;
  }
}
</style>
