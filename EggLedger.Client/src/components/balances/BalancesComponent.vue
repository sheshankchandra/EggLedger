<template>
  <div class="balances-workspace">
    <header class="balances-header">
      <div>
        <p class="eyebrow">Ledger</p>
        <h1>Balances</h1>
        <p>Who owes who for {{ resource.plural }} in {{ room.roomName }}.</p>
      </div>
      <router-link to="/room" class="btn btn-secondary">
        <ArrowLeft :size="16" aria-hidden="true" /> Back to room
      </router-link>
    </header>

    <LoadingSkeleton
      v-if="ledgerStore.loading"
      :count="1"
      height="120px"
      aria-label="Loading balances"
    />
    <div v-if="ledgerStore.error" class="alert alert-error">{{ ledgerStore.error }}</div>

    <template v-if="!ledgerStore.loading">
      <section class="my-balance-card" :class="myBalanceClass">
        <span>Your balance</span>
        <strong>{{ myBalanceLabel }}</strong>
        <small v-if="Math.abs(myBalance) > 0.01"
          >Settle up below, or mark a payment as received</small
        >
        <small v-else>You're all settled up in this room</small>
      </section>

      <section class="workspace-section" aria-labelledby="members-heading">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Overview</p>
            <h2 id="members-heading">Member balances</h2>
          </div>
        </div>
        <div class="balances-grid">
          <div v-for="balance in ledgerStore.balances" :key="balance.userId" class="balance-card">
            <span class="balance-name">{{ nameOrYou(balance.userId, balance.userName) }}</span>
            <strong :class="balanceClass(balance.netBalance)">{{
              signedAmount(balance.netBalance)
            }}</strong>
            <small>{{ balanceHint(balance.netBalance) }}</small>
          </div>
        </div>
      </section>

      <section class="workspace-section" aria-labelledby="settle-heading">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Settle up</p>
            <h2 id="settle-heading">
              {{ showDetailed ? 'Every pairwise debt' : 'Suggested payments' }}
            </h2>
          </div>
          <button
            type="button"
            class="btn btn-secondary btn-sm"
            @click="showDetailed = !showDetailed"
          >
            {{ showDetailed ? 'Show simplified' : 'Show detailed' }}
          </button>
        </div>

        <EmptyState
          v-if="activeList.length === 0"
          :icon="CircleCheck"
          title="Nothing to settle"
          description="All balances are already even in this room."
        />
        <ul v-else class="settle-list">
          <li v-for="(entry, index) in activeList" :key="index" class="settle-item">
            <span class="settle-text">
              <strong>{{ nameOrYou(entry.fromUserId, entry.fromUserName) }}</strong>
              owes
              <strong>{{ nameOrYou(entry.toUserId, entry.toUserName) }}</strong>
            </span>
            <span class="settle-amount">₹{{ fmt(entry.amount) }}</span>
            <button
              v-if="entry.toUserId === currentUserId"
              type="button"
              class="btn btn-primary btn-sm"
              @click="openSettleModal(entry)"
            >
              Mark as received
            </button>
          </li>
        </ul>
      </section>

      <section class="workspace-section" aria-labelledby="history-heading">
        <div class="section-heading">
          <div>
            <p class="eyebrow">Activity</p>
            <h2 id="history-heading">Settlement history</h2>
          </div>
        </div>
        <LoadingSkeleton
          v-if="ledgerStore.historyLoading"
          :count="2"
          height="80px"
          aria-label="Loading history"
        />
        <EmptyState
          v-else-if="ledgerStore.history.length === 0"
          :icon="Receipt"
          title="No settlements yet"
          description="Recorded payments will show up here."
        />
        <ul v-else class="history-list">
          <li
            v-for="settlement in ledgerStore.history"
            :key="settlement.settlementId"
            class="history-item"
          >
            <div class="history-line">
              <span>
                <strong>{{ nameOrYou(settlement.payerId, settlement.payerName) }}</strong>
                paid
                <strong>{{ nameOrYou(settlement.receiverId, settlement.receiverName) }}</strong>
              </span>
              <span class="history-amount">₹{{ fmt(settlement.amount) }}</span>
            </div>
            <div class="history-meta">
              <span>{{ formatDate(settlement.datestamp) }}</span>
              <button
                v-if="settlement.receiverId === currentUserId"
                type="button"
                class="link-button"
                @click="handleDelete(settlement.settlementId)"
              >
                Undo
              </button>
            </div>
            <p v-if="settlement.note" class="history-note">"{{ settlement.note }}"</p>
          </li>
        </ul>
      </section>
    </template>

    <Modal v-if="showSettleModal" title="Mark as received" @close="closeSettleModal">
      <form @submit.prevent="handleRecordSettlement" novalidate>
        <p class="settle-modal-hint">
          Confirm that <strong>{{ settleForm.payerName }}</strong> paid you.
        </p>
        <div class="form-group">
          <label for="settle-amount" class="form-label">Amount received (₹)</label>
          <input
            id="settle-amount"
            v-model.number="settleForm.amount"
            type="number"
            min="0.01"
            step="0.01"
            class="form-input"
            required
            :disabled="submitting"
          />
        </div>
        <div class="form-group">
          <label for="settle-note" class="form-label">Note (optional)</label>
          <input
            id="settle-note"
            v-model.trim="settleForm.note"
            type="text"
            maxlength="500"
            class="form-input"
            placeholder="e.g. Paid via GPay"
            :disabled="submitting"
          />
        </div>
        <div v-if="settleError" class="alert alert-error">{{ settleError }}</div>
        <button type="submit" class="btn btn-primary submit-button" :disabled="submitting">
          {{ submitting ? 'Recording…' : 'Confirm received' }}
        </button>
      </form>
    </Modal>

    <Toast :notification="notification" />
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { CircleCheck, Receipt, ArrowLeft } from '@lucide/vue'
import { useAuthStore } from '@/stores/auth.store'
import { useLedgerStore } from '@/stores/ledger.store'
import { useNotification } from '@/composables/useNotification'
import { errorMessage, isCanceled } from '@/utils/httpError'
import { resourceConfig as resource } from '@/config/resource.config'
import Modal from '@/components/common/BaseModal.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import LoadingSkeleton from '@/components/common/LoadingSkeleton.vue'
import Toast from '@/components/common/ToastNotification.vue'

const props = defineProps({
  room: { type: Object, required: true },
})

const authStore = useAuthStore()
const ledgerStore = useLedgerStore()
const { notification, showNotification } = useNotification()

const currentUserId = computed(() => authStore.getUser?.userId)
const showDetailed = ref(false)
const showSettleModal = ref(false)
const submitting = ref(false)
const settleError = ref(null)

const settleForm = reactive({ payerId: null, payerName: '', amount: 0, note: '' })

const activeList = computed(() =>
  showDetailed.value ? ledgerStore.pairwiseDebts : ledgerStore.suggestedSettlements,
)

const myBalance = computed(() => {
  const mine = ledgerStore.balances.find((b) => b.userId === currentUserId.value)
  return mine ? mine.netBalance : 0
})

const myBalanceLabel = computed(() => {
  if (myBalance.value > 0.01) return `You are owed ₹${fmt(myBalance.value)}`
  if (myBalance.value < -0.01) return `You owe ₹${fmt(-myBalance.value)}`
  return "You're settled up"
})

const myBalanceClass = computed(() => balanceClass(myBalance.value, 'my-balance'))

const fmt = (value) => Number(value || 0).toFixed(2)

const signedAmount = (value) => {
  if (value > 0.01) return `+₹${fmt(value)}`
  if (value < -0.01) return `-₹${fmt(-value)}`
  return '₹0.00'
}

const balanceClass = (value, prefix = 'balance') => {
  if (value > 0.01) return `${prefix}-positive`
  if (value < -0.01) return `${prefix}-negative`
  return `${prefix}-neutral`
}

const balanceHint = (value) => {
  if (value > 0.01) return 'is owed overall'
  if (value < -0.01) return 'owes overall'
  return 'settled up'
}

const nameOrYou = (userId, name) => (userId === currentUserId.value ? 'You' : name)

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleString()
  } catch {
    return 'Unknown'
  }
}

const openSettleModal = (entry) => {
  settleError.value = null
  settleForm.payerId = entry.fromUserId
  settleForm.payerName = entry.fromUserName
  settleForm.amount = entry.amount
  settleForm.note = ''
  showSettleModal.value = true
}

const closeSettleModal = () => {
  if (!submitting.value) showSettleModal.value = false
}

const handleRecordSettlement = async () => {
  if (!settleForm.amount || settleForm.amount <= 0) {
    settleError.value = 'Enter an amount greater than zero.'
    return
  }

  submitting.value = true
  settleError.value = null
  try {
    await ledgerStore.recordSettlement(props.room.roomCode, {
      payerId: settleForm.payerId,
      amount: settleForm.amount,
      note: settleForm.note || null,
    })
    showNotification('Settlement recorded!')
    showSettleModal.value = false
  } catch (err) {
    if (isCanceled(err)) return
    settleError.value = errorMessage(err, 'Failed to record settlement')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (settlementId) => {
  try {
    await ledgerStore.deleteSettlement(props.room.roomCode, settlementId)
    showNotification('Settlement removed')
  } catch (err) {
    if (isCanceled(err)) return
    showNotification(errorMessage(err, 'Failed to remove settlement'), 'error')
  }
}

const loadAll = async () => {
  await Promise.all([
    ledgerStore.fetchLedger(props.room.roomCode),
    ledgerStore.fetchHistory(props.room.roomCode),
  ])
}

onMounted(loadAll)

watch(
  () => props.room.roomCode,
  (newCode, oldCode) => {
    if (newCode && newCode !== oldCode) loadAll()
  },
)
</script>

<style scoped>
.balances-workspace {
  display: grid;
  gap: var(--spacing-2xl);
}

.balances-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  border-radius: var(--radius-2xl);
  background: linear-gradient(145deg, var(--bg-primary), var(--bg-tertiary));
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-md);
}

.balances-header h1 {
  margin-bottom: var(--spacing-sm);
  font-size: clamp(2rem, 5vw, 3rem);
  letter-spacing: -0.04em;
}

.balances-header p:last-child {
  margin: 0;
}

.my-balance-card {
  display: grid;
  gap: var(--spacing-xs);
  padding: var(--spacing-xl);
  border-radius: var(--radius-xl);
  text-align: center;
}

.my-balance-card > span {
  color: var(--text-muted);
  font-size: var(--font-size-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.my-balance-card > strong {
  font-size: clamp(1.75rem, 5vw, 2.5rem);
}

.my-balance-card > small {
  color: var(--text-secondary);
}

.my-balance-positive {
  background: var(--color-success-light);
}

.my-balance-positive strong {
  color: var(--color-success);
}

.my-balance-negative {
  background: var(--color-danger-light);
}

.my-balance-negative strong {
  color: var(--color-danger);
}

.my-balance-neutral {
  background: var(--bg-tertiary);
}

.my-balance-neutral strong {
  color: var(--text-primary);
}

.workspace-section {
  display: grid;
  gap: var(--spacing-lg);
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
}

.section-heading h2 {
  margin: 0;
}

.balances-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 200px), 1fr));
  gap: var(--spacing-md);
}

.balance-card {
  display: grid;
  gap: var(--spacing-xs);
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: var(--bg-tertiary);
  text-align: center;
}

.balance-name {
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.balance-card strong {
  font-size: var(--font-size-xl);
}

.balance-card small {
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.balance-positive {
  color: var(--color-success);
}

.balance-negative {
  color: var(--color-danger);
}

.balance-neutral {
  color: var(--text-primary);
}

.settle-list,
.history-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  margin: 0;
  padding: 0;
  list-style: none;
}

.settle-item {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
}

.settle-amount {
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-lg);
  color: var(--text-primary);
}

.history-item {
  padding: var(--spacing-md);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
}

.history-line {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
}

.history-amount {
  font-weight: var(--font-weight-semibold);
  color: var(--color-success);
}

.history-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: var(--spacing-xs);
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.history-note {
  margin: var(--spacing-xs) 0 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
  font-style: italic;
}

.link-button {
  padding: 0;
  border: none;
  background: none;
  color: var(--color-danger);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}

.link-button:hover {
  text-decoration: underline;
}

.settle-modal-hint {
  margin: 0 0 var(--spacing-md);
  color: var(--text-secondary);
}

.submit-button {
  width: 100%;
  margin-top: var(--spacing-sm);
}

@media (max-width: 768px) {
  .balances-header {
    align-items: stretch;
    flex-direction: column;
  }

  .section-heading {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
