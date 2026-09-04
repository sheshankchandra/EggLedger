<template>
  <BaseModal
    :title="title"
    role="alertdialog"
    content-class="confirm-modal"
    :closable="!busy"
    :close-on-overlay-click="!busy"
    @close="$emit('cancel')"
  >
    <p>
      <slot />
    </p>
    <div v-if="warning" class="alert alert-warning">{{ warning }}</div>

    <template #footer>
      <button @click="$emit('cancel')" :disabled="busy" class="btn btn-secondary" type="button">
        {{ cancelLabel }}
      </button>
      <button
        @click="$emit('confirm')"
        :disabled="busy"
        :class="['btn', danger ? 'btn-danger' : 'btn-primary']"
        type="button"
      >
        {{ busy ? busyLabel : confirmLabel }}
      </button>
    </template>
  </BaseModal>
</template>

<script setup>
import BaseModal from './BaseModal.vue'

defineProps({
  title: { type: String, required: true },
  warning: { type: String, default: '' },
  confirmLabel: { type: String, default: 'Confirm' },
  busyLabel: { type: String, default: 'Working…' },
  cancelLabel: { type: String, default: 'Cancel' },
  busy: { type: Boolean, default: false },
  danger: { type: Boolean, default: true },
})

defineEmits(['confirm', 'cancel'])
</script>
