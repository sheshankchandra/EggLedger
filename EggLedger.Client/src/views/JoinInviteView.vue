<template>
  <div class="join-invite-view">
    <NavigationHeader />
    <main class="page-shell">
      <div class="join-card">
        <p class="eyebrow">Room invite</p>
        <h1>Join with a shared link</h1>

        <div v-if="!code" class="alert alert-error">This invite link is missing a room code.</div>
        <div v-else-if="pending" class="alert alert-success">
          Request submitted. The room admin needs to approve you before you can open the room here.
        </div>
        <template v-else>
          <p class="join-hint">
            You've been invited to join room <strong>{{ code }}</strong
            >.
          </p>
          <div v-if="error" class="alert alert-error">{{ error }}</div>
          <button
            type="button"
            class="btn btn-primary submit-button"
            :disabled="joining"
            @click="handleJoin"
          >
            {{ joining ? 'Joining…' : 'Join room' }}
          </button>
        </template>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useRoomStore } from '@/stores/room.store'
import { errorMessage, isCanceled } from '@/utils/httpError'
import NavigationHeader from '@/components/common/NavigationHeader.vue'

const props = defineProps({
  code: { type: String, default: '' },
})

const router = useRouter()
const roomStore = useRoomStore()
const joining = ref(false)
const error = ref(null)
const pending = ref(false)

const handleJoin = async () => {
  const roomCode = Number(props.code)
  if (!roomCode) {
    error.value = 'This invite link has an invalid room code.'
    return
  }

  joining.value = true
  error.value = null
  try {
    const response = await roomStore.joinRoom(roomCode)
    if (response.value?.isPending) {
      pending.value = true
    } else {
      roomStore.selectRoom(roomCode)
      router.push('/room')
    }
  } catch (err) {
    if (isCanceled(err)) return
    error.value = errorMessage(err, 'Failed to join the room.')
  } finally {
    joining.value = false
  }
}
</script>

<style scoped>
.join-invite-view {
  min-height: 100vh;
}

.join-card {
  max-width: 480px;
  margin: var(--spacing-2xl) auto 0;
  padding: var(--spacing-2xl);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-md);
  text-align: center;
}

.join-card h1 {
  margin: var(--spacing-sm) 0 var(--spacing-lg);
}

.join-hint {
  margin-bottom: var(--spacing-lg);
  color: var(--text-secondary);
}

.join-hint strong {
  font-family: var(--font-family-mono);
  color: var(--color-primary);
}

.submit-button {
  width: 100%;
}
</style>
