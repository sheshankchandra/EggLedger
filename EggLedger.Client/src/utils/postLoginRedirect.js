const STORAGE_KEY = 'postLoginRedirect'

/**
 * Remembers where an unauthenticated visit was headed (e.g. a shared invite link) so login -
 * including the full-page round trip through Google OAuth - can send the user back there
 * instead of always landing on the dashboard.
 */
export function rememberRedirect(fullPath) {
  sessionStorage.setItem(STORAGE_KEY, fullPath)
}

export function consumeRedirect() {
  const path = sessionStorage.getItem(STORAGE_KEY)
  sessionStorage.removeItem(STORAGE_KEY)
  return path
}
