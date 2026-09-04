import { defineStore } from 'pinia'

const STORAGE_KEY = 'theme'

function systemPrefersDark() {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
}

function applyTheme(theme) {
  if (theme) {
    document.documentElement.setAttribute('data-theme', theme)
  } else {
    document.documentElement.removeAttribute('data-theme')
  }
}

/**
 * Tracks light/dark theme preference. `preference` is null ("follow system") or an explicit
 * 'light'/'dark' choice persisted to localStorage. The actual color values live in
 * variables.css under `[data-theme="dark"]` and a `prefers-color-scheme` media query - this
 * store only decides which one applies, via the `data-theme` attribute on <html>.
 */
export const useThemeStore = defineStore('theme', {
  state: () => ({
    preference: localStorage.getItem(STORAGE_KEY) || null,
  }),
  getters: {
    isDark: (state) => (state.preference ? state.preference === 'dark' : systemPrefersDark()),
  },
  actions: {
    // Call once on app startup, before mount, to avoid a flash of the wrong theme.
    init() {
      applyTheme(this.preference)
    },

    setTheme(theme) {
      this.preference = theme
      if (theme) {
        localStorage.setItem(STORAGE_KEY, theme)
      } else {
        localStorage.removeItem(STORAGE_KEY)
      }
      applyTheme(theme)
    },

    toggleTheme() {
      this.setTheme(this.isDark ? 'light' : 'dark')
    },
  },
})
