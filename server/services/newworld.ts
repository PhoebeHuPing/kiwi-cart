import request from 'superagent'
import { PriceComparisonData } from '../../models/products'

const DEFAULT_STORE_ID = 'dbdfdd2a-55f7-4870-9b51-979286323647' // browns bay New World
let cachedToken: string | null = null

/**
 * Fetches a fresh access token from the New World session initialization endpoint.
 */
async function getNewWorldToken(): Promise<string> {
  if (cachedToken) return cachedToken

  try {
    const response = await request
      .post('https://www.newworld.co.nz/api/user/get-current-user')
      .set(
        'User-Agent',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
      )
      .set('Content-Type', 'application/json')
      .send({})

    const token = response.body.access_token
    if (!token) throw new Error('No token returned from New World')

    cachedToken = token
    console.log('Successfully refreshed New World token.')
    return token
  } catch (error: any) {
    console.error('Failed to fetch New World token:', error.message)
    throw error
  }
}

/**
 * Represents the raw product structure returned by New World's internal API.
 */
interface NewWorldProduct {
  productId: string
  name: string
  images?: {
    primaryImages?: {
      '400px'?: string
    }
  }
  singlePrice?: {
    price?: number
  }
}

interface NewWorldSearchResponse {
  products: NewWorldProduct[]
}

/**
 * Fetches real-time prices from New World using the automated token system.
 */
export async function fetchNewWorldPrices(
  searchTerm: string,
): Promise<PriceComparisonData[]> {
  try {
    let token = await getNewWorldToken()

    let response = await makeSearchRequest(searchTerm, token)

    // Handle token expiration (retry once)
    if (response.status === 401) {
      console.warn('New World token expired, refreshing...')
      cachedToken = null
      token = await getNewWorldToken()
      response = await makeSearchRequest(searchTerm, token)
    }

    const body = response.body as NewWorldSearchResponse
    const products = body.products || []

    return products.map((p) => {
      // New World uses long UUIDs; the first part is often used for simple image lookups
      const simpleId = p.productId.split('-')[0]
      return {
        product_name: p.name,
        image_url:
          p.images?.primaryImages?.['400px'] ||
          `https://a.fsimg.co.nz/product/retail/fan/image/400x400/${simpleId}.png`,
        supermarket_name: 'New World',
        logo_url: '/images/new-world.webp',
        address: 'Victoria Park, Auckland',
        lat: -36.8485,
        lng: 174.7523,
        price: (p.singlePrice?.price || 0) / 100, // Price is in cents, convert to dollars
      }
    })
  } catch (error: any) {
    console.error('New World Real-time API failed:', error.message)
    return []
  }
}

/**
 * Helper to perform the actual HTTP POST to the search API.
 */
async function makeSearchRequest(searchTerm: string, token: string) {
  return request
    .post('https://api-prod.newworld.co.nz/v1/edge/search/paginated/products')
    .set('Authorization', `Bearer ${token}`)
    .set('Content-Type', 'application/json')
    .set(
      'User-Agent',
      'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
    )
    .ok((res) => res.status < 500)
    .send({
      algoliaQuery: {
        query: searchTerm,
      },
      storeId: DEFAULT_STORE_ID,
      hitsPerPage: 50,
      page: 0,
      sortOrder: 'NI_POPULARITY_ASC',
    })
}
