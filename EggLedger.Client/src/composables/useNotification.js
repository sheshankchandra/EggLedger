import { ref } from 'vue'

/**
 * Toast-style notification state, previously hand-rolled identically in DashboardComponent,
 * RoomComponent, and ProfileComponent. Each component still renders its own `.notification`
 * element (styling stays local/themeable), but the state and auto-dismiss timer are shared.
 */
export function useNotification(defaultDurationMs = 4000) {
  const notification = ref(null)
  let dismissTimer = null

  const showNotification = (message, type = 'success', durationMs = defaultDurationMs) => {
    clearTimeout(dismissTimer)
    notification.value = { message, type }
    dismissTimer = setTimeout(() => {
      notification.value = null
    }, durationMs)
  }

  const clearNotification = () => {
    clearTimeout(dismissTimer)
    notification.value = null
  }

  return { notification, showNotification, clearNotification }
}
