<template>
  <BaseModal title="Profile settings" @close="$emit('close')">
    <section class="settings-section">
      <h3 class="settings-section-title">Data</h3>
      <button
        type="button"
        class="btn btn-secondary"
        :disabled="refreshing"
        @click="$emit('refresh')"
      >
        <RefreshCw :size="16" aria-hidden="true" />
        {{ refreshing ? 'Refreshing…' : 'Refresh profile data' }}
      </button>
    </section>

    <section class="settings-section">
      <h3 class="settings-section-title">Security</h3>
      <button type="button" class="btn btn-secondary" @click="$emit('change-password')">
        <KeyRound :size="16" aria-hidden="true" />
        Change password
      </button>
    </section>
  </BaseModal>
</template>

<script setup>
import { RefreshCw, KeyRound } from '@lucide/vue'
import BaseModal from '@/components/common/BaseModal.vue'

defineProps({
  refreshing: { type: Boolean, default: false },
})

defineEmits(['close', 'refresh', 'change-password'])
</script>

<style scoped>
.settings-section .btn {
  width: 100%;
}
</style>
