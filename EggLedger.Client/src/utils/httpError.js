/**
 * Extracts a user-facing message from an Axios error. The API returns RFC 7807 ProblemDetails
 * ({ detail, title, status }), so `detail` is checked first; other shapes are kept for safety.
 */
export function errorMessage(error, fallback) {
  const data = error?.response?.data
  if (Array.isArray(data)) return data.join(', ')
  if (typeof data === 'string') return data
  if (data?.detail) return data.detail
  if (data?.message) return data.message
  return fallback
}

export function isCanceled(error) {
  return error?.name === 'AbortError' || error?.code === 'ERR_CANCELED'
}
