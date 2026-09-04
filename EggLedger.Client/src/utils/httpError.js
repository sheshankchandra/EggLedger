/**
 * Extracts a user-facing message from an Axios error, matching the API's varying error
 * shapes (a plain string, an array of validation messages, or a { message } object).
 * Was duplicated ad hoc across Dashboard/Room/Profile components.
 */
export function errorMessage(error, fallback) {
  const data = error?.response?.data
  if (Array.isArray(data)) return data.join(', ')
  if (typeof data === 'string') return data
  if (data?.message) return data.message
  return fallback
}

export function isCanceled(error) {
  return error?.name === 'AbortError' || error?.code === 'ERR_CANCELED'
}
