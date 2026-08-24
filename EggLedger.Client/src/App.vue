<template>
  <RouterView />
  <span class="app-version">{{ appVersion }}</span>
</template>

<script setup>
import { RouterView } from 'vue-router'

// Build stamp injected at build time (commit SHA in CI, "dev" locally). Shows a
// short version tag so the running client is identifiable at a glance.
const rawVersion = import.meta.env.VITE_APP_VERSION || 'dev'
const appVersion = rawVersion.slice(0, 7)
</script>

<style scoped>
.app-version {
  position: fixed;
  bottom: 4px;
  right: 6px;
  font-size: 10px;
  color: rgba(0, 0, 0, 0.35);
  pointer-events: none;
  user-select: none;
  z-index: 9999;
}
</style>
