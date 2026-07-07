export function buildApiUrl(path: string): string {
  const baseUrl = import.meta.env.VITE_API_BASE_URL?.trim()

  if (baseUrl) {
    return `${baseUrl.replace(/\/$/, '')}${path.startsWith('/') ? path : `/${path}`}`
  }

  return `/api${path.startsWith('/') ? path : `/${path}`}`
}
