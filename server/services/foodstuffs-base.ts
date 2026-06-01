import request from 'superagent'
import { PriceComparisonData } from '../../models/products'

/**
 * Common configuration for Foodstuffs brands (Pak'nSave, New World, Four Square).
 */
export interface FoodstuffsConfig {
  domain: string // e.g., 'paknsave.co.nz'
  apiDomain: string // e.g., 'api-prod.paknsave.co.nz'
  storeId: string
  supermarketName: string
  logoUrl: string
  defaultAddress: string
  defaultLat: number
  defaultLng: number
}

/**
 * Optional overrides for a specific fetch request.
 */
export interface FetchOptions {
  storeId?: string
  lat?: number
  lng?: number
  address?: string
}

/**
 * Shared state for tokens to avoid redundant refreshes across requests.
 */
const tokenCache: Record<string, string | null> = {}

/**
 * Fetches or returns a cached access token for a specific Foodstuffs domain.
 */
async function getFoodstuffsToken(domain: string): Promise<string> {
  if (tokenCache[domain]) return tokenCache[domain]!

  try {
    const response = await request
      .post(`https://www.${domain}/api/user/get-current-user`)
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('Content-Type', 'application/json')
      .send({})

    const token = response.body.access_token
    if (!token) throw new Error(`No token returned from ${domain}`)
    
    tokenCache[domain] = token
    console.log(`Successfully refreshed token for ${domain}`)
    return token
  } catch (error: any) {
    console.error(`Failed to fetch token for ${domain}:`, error.message)
    throw error
  }
}

/**
 * Helper to perform the actual HTTP POST to the Foodstuffs edge search API.
 */
async function makeSearchRequest(searchTerm: string, token: string, config: FoodstuffsConfig, options?: FetchOptions) {
  const storeId = options?.storeId || config.storeId
  
  return request
    .post(`https://${config.apiDomain}/v1/edge/search/paginated/products`)
    .set('Authorization', `Bearer ${token}`)
    .set('Content-Type', 'application/json')
    .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
    .ok(res => res.status < 500)
    .send({
      algoliaQuery: {
        query: searchTerm,
      },
      storeId,
      hitsPerPage: 50,
      page: 0,
      sortOrder: 'NI_POPULARITY_ASC'
    })
}

/**
 * Generic fetcher for Foodstuffs brand prices.
 */
export async function fetchFoodstuffsPrices(
  searchTerm: string,
  config: FoodstuffsConfig,
  options?: FetchOptions
): Promise<PriceComparisonData[]> {
  try {
    let token = await getFoodstuffsToken(config.domain)
    let response = await makeSearchRequest(searchTerm, token, config, options)

    // Handle token expiration (retry once)
    if (response.status === 401) {
      console.warn(`Token for ${config.domain} expired, refreshing...`)
      tokenCache[config.domain] = null
      token = await getFoodstuffsToken(config.domain)
      response = await makeSearchRequest(searchTerm, token, config, options)
    }

    const products = response.body.products || []
    
    return products.map((p: any) => {
      const simpleId = p.productId.split('-')[0]
      return {
        product_name: p.name,
        image_url: p.images?.primaryImages?.['400px'] || `https://a.fsimg.co.nz/product/retail/fan/image/400x400/${simpleId}.png`,
        supermarket_name: config.supermarketName,
        logo_url: config.logoUrl,
        address: options?.address || config.defaultAddress,
        lat: options?.lat ?? config.defaultLat,
        lng: options?.lng ?? config.defaultLng,
        price: (p.singlePrice?.price || 0) / 100,
      }
    })

  } catch (error: any) {
    console.error(`${config.supermarketName} Real-time API failed:`, error.message)
    return []
  }
}
