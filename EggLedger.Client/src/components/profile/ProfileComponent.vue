<template>
  <div class="profile-workspace">
    <header class="profile-header">
      <div class="profile-identity">
        <span class="profile-avatar" aria-hidden="true">{{ initials }}</span>
        <div>
          <p class="eyebrow">Your account</p>
          <h1>{{ user?.name || 'Profile' }}</h1>
          <p class="profile-email">{{ user?.email }}</p>
        </div>
      </div>
      <div class="profile-header-actions">
        <span v-if="user" class="role-badge">{{ getRoleName(user.role) }}</span>
        <button
          type="button"
          class="profile-settings-button"
          aria-label="Profile settings"
          title="Profile settings"
          @click="showSettingsModal = true"
        >
          <Settings :size="18" aria-hidden="true" />
        </button>
      </div>
    </header>

    <LoadingSkeleton v-if="loading" :count="1" height="140px" aria-label="Loading profile" />
    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <template v-if="user">
      <!-- Statistics -->
      <section class="summary-grid" aria-label="Your stats">
        <div class="summary-card summary-card-primary">
          <span>Rooms joined</span>
          <strong>{{ roomStore.userRooms.length }}</strong>
        </div>
        <div class="summary-card">
          <span>Active {{ resource.inventoryPlural }}</span>
          <strong>{{ totalContainers }}</strong>
        </div>
        <div class="summary-card">
          <span>{{ resource.displayName }} tracked</span>
          <strong>{{ totalEggs }}</strong>
        </div>
        <div class="summary-card">
          <span>Rooms you admin</span>
          <strong>{{ adminRooms }}</strong>
        </div>
      </section>

      <!-- Activity & Streaks -->
      <section class="profile-section" aria-labelledby="activity-heading">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Activity</p>
            <h2 id="activity-heading">Your streaks & stats</h2>
          </div>
          <div class="range-tabs" role="tablist" aria-label="Time range">
            <button
              v-for="option in rangeOptions"
              :key="option.value"
              type="button"
              role="tab"
              :aria-selected="statsStore.range === option.value"
              :class="{ active: statsStore.range === option.value }"
              @click="statsStore.fetchStats(option.value)"
            >
              {{ option.label }}
            </button>
          </div>
        </div>

        <div class="streak-card">
          <span class="streak-flame" aria-hidden="true"><Flame :size="28" /></span>
          <div>
            <strong>{{ statsStore.currentStreakDays }}-day streak</strong>
            <small>Longest streak: {{ statsStore.longestStreakDays }} days</small>
          </div>
        </div>

        <LoadingSkeleton
          v-if="statsStore.loading"
          :count="1"
          height="280px"
          aria-label="Loading stats"
        />
        <template v-else>
          <div class="stats-summary-grid">
            <div class="summary-card summary-card-primary">
              <span>{{ resource.displayName }} eaten</span>
              <strong>{{ statsStore.totalEggsConsumed }}</strong>
            </div>
            <div class="summary-card">
              <span>Protein</span>
              <strong>{{ statsStore.totalProteinGrams }}g</strong>
              <small>Estimated from {{ resource.plural }} eaten</small>
            </div>
            <div class="summary-card">
              <span>Calories</span>
              <strong>{{ statsStore.totalCalories }}<span class="stat-unit">kcal</span></strong>
            </div>
          </div>

          <EmptyState
            v-if="statsStore.buckets.every((bucket) => bucket.eggsConsumed === 0)"
            :icon="Activity"
            title="No activity yet"
            :description="`Record a consume order to start tracking your ${resource.plural} habit.`"
          />
          <StatsChart
            v-else
            :buckets="statsStore.buckets"
            :aria-label="`${resource.displayName} consumed over time`"
          />
        </template>
      </section>

      <!-- Room Memberships -->
      <section class="profile-section" aria-labelledby="rooms-heading">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Memberships</p>
            <h2 id="rooms-heading">Your rooms</h2>
          </div>
        </div>

        <EmptyState
          v-if="roomStore.userRooms.length === 0"
          :icon="House"
          title="No rooms yet"
          description="Join or create a room from the dashboard to start tracking shared inventory."
        >
          <template #actions>
            <router-link to="/" class="btn btn-primary">Go to dashboard</router-link>
          </template>
        </EmptyState>

        <div v-else class="rooms-grid">
          <RoomCard
            v-for="room in roomStore.userRooms"
            :key="room.roomId"
            :room="room"
            :resource="resource"
            :is-admin="room.adminUserId === user.userId"
            :date-label="`Joined ${formatDate(room.joinedAt)}`"
            @select="goToRoom"
          />
        </div>
      </section>

      <!-- My Containers -->
      <section class="profile-section" aria-labelledby="containers-heading">
        <div v-if="!selectedRoom" class="card text-center p-5">
          <p class="text-secondary mb-4">
            Select a room to see your {{ resource.inventoryPlural }} there.
          </p>
          <router-link to="/" class="btn btn-primary">Select a room</router-link>
        </div>
        <template v-else>
          <InventoryGrid
            :containers="activeContainers"
            :loading="loadingContainers"
            :resource="resource"
            :current-user-id="user.userId"
            :heading="`My active ${resource.inventoryPlural} in ${selectedRoom.roomName}`"
            empty-title="Nothing active right now"
            :empty-description="
              historyContainers.length > 0
                ? 'All caught up. Check your history below for past purchases.'
                : `Purchases you make in ${selectedRoom.roomName} will show up here.`
            "
            @select="viewContainerDetails"
          />

          <details
            v-if="!loadingContainers && historyContainers.length > 0"
            class="history-disclosure"
          >
            <summary>
              <span>History</span>
              <span class="history-count">{{ historyContainers.length }}</span>
            </summary>
            <ul class="history-list">
              <li
                v-for="container in historyContainers"
                :key="container.containerId"
                class="history-row"
              >
                <span class="history-icon" aria-hidden="true"
                  ><component :is="resource.icon" :size="16"
                /></span>
                <div class="history-row-info">
                  <strong>{{
                    container.containerName || `Untitled ${resource.inventorySingular}`
                  }}</strong>
                  <small>
                    {{ formatDate(container.purchaseDateTime) }} · {{ statusLabel(container) }}
                    <template v-if="container.deletedAt">
                      on {{ formatDate(container.deletedAt) }}</template
                    >
                  </small>
                </div>
                <span class="history-quantity"
                  >{{ container.totalQuantity }} {{ resource.plural }}</span
                >
                <button type="button" class="link-button" @click="viewContainerDetails(container)">
                  View
                </button>
              </li>
            </ul>
          </details>
        </template>
      </section>

      <!-- Account Actions -->
    </template>

    <ProfileSettingsModal
      v-if="showSettingsModal"
      :refreshing="refreshing"
      @close="showSettingsModal = false"
      @refresh="refreshProfile"
      @change-password="openSettingsChangePassword"
    />

    <!-- Change Password Modal -->
    <Modal v-if="showChangePassword" title="Change password" @close="closeChangePassword">
      <form @submit.prevent="handleChangePassword" novalidate>
        <div class="form-group">
          <label for="current-password" class="form-label">Current password</label>
          <input
            id="current-password"
            v-model="passwordForm.current"
            type="password"
            class="form-input"
            :class="{ 'is-invalid': passwordTouched.current && currentPasswordError }"
            :aria-invalid="passwordTouched.current && !!currentPasswordError"
            aria-describedby="current-password-feedback"
            required
            :disabled="changingPassword"
            @blur="passwordTouched.current = true"
          />
          <small
            v-if="passwordTouched.current && currentPasswordError"
            id="current-password-feedback"
            class="form-feedback is-invalid"
          >
            {{ currentPasswordError }}
          </small>
        </div>
        <div class="form-group">
          <label for="new-password" class="form-label">New password</label>
          <input
            id="new-password"
            v-model="passwordForm.new"
            type="password"
            class="form-input"
            :class="{ 'is-invalid': passwordTouched.new && newPasswordError }"
            :aria-invalid="passwordTouched.new && !!newPasswordError"
            aria-describedby="new-password-feedback"
            required
            :disabled="changingPassword"
            @blur="passwordTouched.new = true"
          />
          <small
            v-if="passwordTouched.new && newPasswordError"
            id="new-password-feedback"
            class="form-feedback is-invalid"
          >
            {{ newPasswordError }}
          </small>
        </div>
        <div class="form-group">
          <label for="confirm-password" class="form-label">Confirm new password</label>
          <input
            id="confirm-password"
            v-model="passwordForm.confirm"
            type="password"
            class="form-input"
            :class="{ 'is-invalid': passwordTouched.confirm && confirmPasswordError }"
            :aria-invalid="passwordTouched.confirm && !!confirmPasswordError"
            aria-describedby="confirm-password-feedback"
            required
            :disabled="changingPassword"
            @blur="passwordTouched.confirm = true"
          />
          <small
            v-if="passwordTouched.confirm && confirmPasswordError"
            id="confirm-password-feedback"
            class="form-feedback is-invalid"
          >
            {{ confirmPasswordError }}
          </small>
        </div>

        <div v-if="passwordFormError" class="alert alert-error">{{ passwordFormError }}</div>

        <button type="submit" :disabled="changingPassword" class="btn btn-primary submit-button">
          {{ changingPassword ? 'Changing…' : 'Change password' }}
        </button>
      </form>
    </Modal>

    <!-- Notification -->
    <Toast :notification="notification" />
  </div>
</template>

<script setup>
import { onMounted, computed, ref, reactive, watch } from 'vue'
import { useRouter } from 'vue-router'
import { Flame, House, Activity, Settings } from '@lucide/vue'
import { useAuthStore } from '@/stores/auth.store'
import { useRoomStore } from '@/stores/room.store'
import { useInventoryStore } from '@/stores/inventory.store'
import { useNotification } from '@/composables/useNotification'
import { errorMessage, isCanceled } from '@/utils/httpError'
import { resourceConfig as resource } from '@/config/resource.config'
import userService from '@/services/user.service'
import Toast from '@/components/common/ToastNotification.vue'
import Modal from '@/components/common/BaseModal.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'
import RoomCard from '@/components/common/RoomCard.vue'
import InventoryGrid from '@/components/room/InventoryGrid.vue'
import StatsChart from '@/components/profile/StatsChart.vue'
import ProfileSettingsModal from '@/components/profile/ProfileSettingsModal.vue'
import { useStatsStore } from '@/stores/stats.store'

const router = useRouter()
const authStore = useAuthStore()
const roomStore = useRoomStore()
const inventoryStore = useInventoryStore()
const statsStore = useStatsStore()
const { notification, showNotification } = useNotification()
const loading = ref(false)
const error = ref(null)
const refreshing = ref(false)
const showSettingsModal = ref(false)
const showChangePassword = ref(false)
const changingPassword = ref(false)
const passwordFormError = ref(null)
const loadingContainers = ref(false)
const userContainers = ref([])

const rangeOptions = [
  { value: 'Week', label: '1W' },
  { value: 'Month', label: '1M' },
  { value: 'Year', label: '1Y' },
  { value: 'Max', label: 'Max' },
]

const passwordForm = reactive({
  current: '',
  new: '',
  confirm: '',
})
const passwordTouched = reactive({
  current: false,
  new: false,
  confirm: false,
})

const user = computed(() => authStore.getUser)
const selectedRoom = computed(() => roomStore.selectedRoom)

const initials = computed(() =>
  (user.value?.name || 'You')
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase(),
)

const totalContainers = computed(() => {
  return roomStore.userRooms.reduce((total, room) => total + (room.containerCount || 0), 0)
})

const totalEggs = computed(() => {
  return roomStore.userRooms.reduce((total, room) => total + (room.totalEggs || 0), 0)
})

const adminRooms = computed(() => {
  return roomStore.userRooms.filter((room) => room.adminUserId === user.value?.userId).length
})

// Mirrors EggLedger.Models.Enums.ContainerStatus - enums serialize as their numeric value.
const CONTAINER_STATUS = { AVAILABLE: 1, DEPLETED: 2, ARCHIVED: 3, SUSPENDED: 4 }

const activeContainers = computed(() =>
  userContainers.value.filter(
    (c) => c.status === CONTAINER_STATUS.AVAILABLE && c.remainingQuantity > 0,
  ),
)

// Depleted-through-use and archived/suspended containers, newest first - kept out of the main
// grid so a room's full purchase history doesn't drown out what's actually available today.
const historyContainers = computed(() =>
  userContainers.value
    .filter((c) => !activeContainers.value.includes(c))
    .slice()
    .sort((a, b) => new Date(b.purchaseDateTime) - new Date(a.purchaseDateTime)),
)

// Consumption never actually flips Status to Depleted (only RemainingQuantity drops), so "fully
// consumed" is derived from quantity rather than the Status field for anything not explicitly
// Archived/Suspended by an admin action - matches ContainerDetailView's lifecycle logic.
const statusLabel = (container) => {
  if (container.status === CONTAINER_STATUS.ARCHIVED) return 'Archived'
  if (container.status === CONTAINER_STATUS.SUSPENDED) return 'Suspended'
  return 'Fully consumed'
}

const currentPasswordError = computed(() => {
  if (!passwordForm.current) return 'Current password is required'
  return ''
})

const newPasswordError = computed(() => {
  if (!passwordForm.new) return 'New password is required'
  if (passwordForm.new.length < 6) return 'New password must be at least 6 characters'
  return ''
})

const confirmPasswordError = computed(() => {
  if (!passwordForm.confirm) return 'Please confirm your new password'
  if (passwordForm.confirm !== passwordForm.new) return 'Passwords do not match'
  return ''
})

const passwordFormValid = computed(
  () => !currentPasswordError.value && !newPasswordError.value && !confirmPasswordError.value,
)

const getRoleName = (role) => {
  switch (role) {
    case 0:
      return 'User'
    case 1:
      return 'Admin'
    case 2:
      return 'Super Admin'
    default:
      return 'Unknown'
  }
}

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString()
  } catch {
    return 'Unknown'
  }
}

const refreshProfile = async () => {
  refreshing.value = true
  try {
    await Promise.all([authStore.fetchProfile(), roomStore.fetchUserRooms(), fetchUserContainers()])
    showNotification('Refreshed successfully!')
  } catch (err) {
    showNotification('Failed to refresh', 'error')
    console.error(err)
  } finally {
    refreshing.value = false
  }
}

const goToRoom = (roomCode) => {
  roomStore.selectRoom(roomCode)
  router.push('/room')
}

const openChangePassword = () => {
  passwordForm.current = ''
  passwordForm.new = ''
  passwordForm.confirm = ''
  passwordTouched.current = false
  passwordTouched.new = false
  passwordTouched.confirm = false
  passwordFormError.value = null
  showChangePassword.value = true
}

const openSettingsChangePassword = () => {
  showSettingsModal.value = false
  openChangePassword()
}

const closeChangePassword = () => {
  if (!changingPassword.value) showChangePassword.value = false
}

const handleChangePassword = async () => {
  passwordTouched.current = true
  passwordTouched.new = true
  passwordTouched.confirm = true
  passwordFormError.value = null

  if (!passwordFormValid.value) return

  changingPassword.value = true
  try {
    await userService.changePassword(user.value.userId, passwordForm.current, passwordForm.new)
    showNotification('Password changed successfully!')
    showChangePassword.value = false
  } catch (err) {
    if (isCanceled(err)) return
    passwordFormError.value = errorMessage(err, 'Failed to change password')
    console.error(err)
  } finally {
    changingPassword.value = false
  }
}

// Navigate to container details with container information
const viewContainerDetails = (container) => {
  try {
    // Store container info in sessionStorage temporarily
    sessionStorage.setItem('currentContainerInfo', JSON.stringify(container))

    router.push({
      name: 'container-detail',
      params: { containerId: container.containerId },
    })
  } catch (err) {
    console.error('Error navigating to container details:', err)
  }
}

// Fetch user's containers from the currently selected room
const fetchUserContainers = async () => {
  if (!user.value || !selectedRoom.value) {
    userContainers.value = []
    return
  }

  loadingContainers.value = true
  try {
    userContainers.value =
      (await inventoryStore.searchMyContainers(selectedRoom.value.roomCode, user.value.name)) || []
  } catch (err) {
    if (isCanceled(err)) return
    console.error('Error fetching containers:', err)
    showNotification('Failed to load containers', 'error')
  } finally {
    loadingContainers.value = false
  }
}

onMounted(async () => {
  if (!user.value) {
    loading.value = true
    error.value = null
    try {
      await authStore.fetchProfile()
    } catch (err) {
      error.value = 'Failed to load profile. You may be logged out.'
      console.error(err)
    } finally {
      loading.value = false
    }
  }

  // Fetch user containers after profile is loaded
  await fetchUserContainers()
  await statsStore.fetchStats('Week')
})

// Watch for changes in selected room to refresh containers
watch(selectedRoom, async (newRoom, oldRoom) => {
  if (newRoom?.roomCode !== oldRoom?.roomCode) {
    await fetchUserContainers()
  }
})
</script>

<style scoped>
.profile-workspace {
  display: grid;
  gap: var(--spacing-2xl);
}

.profile-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, var(--bg-primary), var(--bg-tertiary));
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-md);
}

.profile-identity {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
}

.profile-avatar {
  display: grid;
  width: 64px;
  height: 64px;
  flex-shrink: 0;
  place-items: center;
  border-radius: var(--radius-xl);
  background: var(--color-primary-light);
  color: var(--color-primary);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.profile-identity h1 {
  margin: 0;
  font-size: clamp(1.5rem, 4vw, 2.25rem);
  letter-spacing: -0.03em;
}

.profile-email {
  margin: var(--spacing-xs) 0 0;
  color: var(--text-secondary);
}

.role-badge {
  flex-shrink: 0;
  padding: var(--spacing-xs) var(--spacing-md);
  border-radius: 999px;
  background: var(--color-primary);
  color: var(--text-inverse);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.profile-header-actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: var(--spacing-sm);
}

.profile-settings-button {
  display: grid;
  flex-shrink: 0;
  width: 42px;
  height: 42px;
  place-items: center;
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-md);
  background: var(--bg-primary);
  color: var(--text-secondary);
  cursor: pointer;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.profile-settings-button:hover {
  background: var(--bg-tertiary);
  color: var(--color-primary);
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-sm);
}

.summary-card {
  min-width: 0;
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--bg-primary) 72%, transparent);
}

.summary-card > span,
.summary-card small {
  display: block;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.summary-card strong {
  display: block;
  margin-block: var(--spacing-xs);
  font-size: var(--font-size-2xl);
}

.summary-card strong .stat-unit {
  margin-left: var(--spacing-xs);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-muted);
}

.summary-card-primary {
  background: var(--color-primary);
}

.summary-card-primary span,
.summary-card-primary small {
  color: rgba(255, 255, 255, 0.72);
}

.summary-card-primary strong {
  color: var(--text-inverse);
}

.profile-section {
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  background: var(--bg-primary);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.section-heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-lg);
}

.section-heading h2 {
  margin: 0;
}

.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 290px), 1fr));
  gap: var(--spacing-lg);
}

.range-tabs {
  display: flex;
  flex-shrink: 0;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
}

.range-tabs button {
  padding: var(--spacing-xs) var(--spacing-md);
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.range-tabs button.active {
  background: var(--bg-primary);
  color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.streak-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
  padding: var(--spacing-md) var(--spacing-lg);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: var(--bg-tertiary);
}

.streak-flame {
  display: inline-flex;
  color: var(--color-warning);
}

.streak-card strong {
  display: block;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.streak-card small {
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.stats-summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-lg);
}

.history-disclosure {
  margin-top: var(--spacing-lg);
  border-top: 1px solid var(--border-light);
  padding-top: var(--spacing-lg);
}

.history-disclosure summary {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--text-secondary);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  list-style: none;
}

.history-disclosure summary::-webkit-details-marker {
  display: none;
}

.history-disclosure summary::before {
  content: '▸';
  color: var(--text-muted);
  transition: transform var(--transition-normal);
}

.history-disclosure[open] summary::before {
  transform: rotate(90deg);
}

.history-count {
  padding: 2px var(--spacing-sm);
  border-radius: 999px;
  background: var(--bg-tertiary);
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  margin: var(--spacing-md) 0 0;
  padding: 0;
  list-style: none;
}

.history-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
}

.history-icon {
  display: grid;
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  place-items: center;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  color: var(--text-secondary);
}

.history-row-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}

.history-row-info strong {
  overflow: hidden;
  font-size: var(--font-size-sm);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.history-row-info small {
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.history-quantity {
  flex-shrink: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.link-button {
  flex-shrink: 0;
  padding: 0;
  border: none;
  background: none;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

.link-button:hover {
  text-decoration: underline;
}

.submit-button {
  width: 100%;
  margin-top: var(--spacing-sm);
}

@media (max-width: 768px) {
  .profile-header {
    align-items: stretch;
    flex-direction: column;
  }

  .profile-header-actions {
    align-self: flex-start;
  }

  .summary-grid {
    grid-template-columns: 1fr 1fr;
  }

  .section-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .range-tabs {
    align-self: flex-start;
  }
}

@media (max-width: 520px) {
  .profile-workspace {
    gap: var(--spacing-xl);
  }

  .profile-header,
  .profile-section {
    padding: var(--spacing-lg);
  }

  .summary-grid {
    grid-template-columns: 1fr;
  }

  .summary-card {
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
  }

  .summary-card strong {
    grid-row: span 2;
    margin: 0;
  }
}
</style>
