import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buildApiUrl } from '../../client/apis/apiBaseUrl'

describe('buildApiUrl', () => {
  const originalBaseUrl = import.meta.env.VITE_API_BASE_URL

  beforeEach(() => {
    vi.unstubAllEnvs()
  })

  afterEach(() => {
    vi.unstubAllEnvs()
    if (originalBaseUrl !== undefined) {
      vi.stubEnv('VITE_API_BASE_URL', originalBaseUrl)
    }
  })

  it('uses the configured API base URL when provided', () => {
    vi.stubEnv('VITE_API_BASE_URL', 'https://kiwi-cart.azurewebsites.net/api')

    expect(buildApiUrl('/v1/products')).toBe(
      'https://kiwi-cart.azurewebsites.net/api/v1/products',
    )
  })

  it('falls back to the local /api prefix when no base URL is configured', () => {
    vi.stubEnv('VITE_API_BASE_URL', '')

    expect(buildApiUrl('/v1/products')).toBe('/api/v1/products')
  })
})
