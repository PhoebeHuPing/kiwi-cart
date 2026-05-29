import request from 'superagent'
import { PriceComparisonData } from '../../models/products'

const DEFAULT_STORE_ID = '65defcf2-bc15-490e-a84f-1f13b769cd22' // Henderson Pak'nSave
let cachedToken: string | null = null

/**
 * Fetches a fresh access token from the Pak'nSave session initialization endpoint.
 */
async function getPaknsaveToken(): Promise<string> {
  if (cachedToken) return cachedToken

  try {
    const response = await request
      .post('https://www.paknsave.co.nz/api/user/get-current-user')
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('Content-Type', 'application/json')
      .send({})

    const token = response.body.access_token
    if (!token) throw new Error('No token returned from PaknSave')
    
    cachedToken = token
    console.log('Successfully refreshed PaknSave token.')
    return token
  } catch (error: any) {
    console.error('Failed to fetch PaknSave token:', error.message)
    throw error
  }
}

/**
 * Represents the raw product structure returned by Pak'nSave's internal API.
 */
interface PaknsaveProduct {
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

interface PaknsaveSearchResponse {
  products: PaknsaveProduct[]
}

/**
 * Fetches real-time prices from Pak'nSave using the automated token system.
 */
export async function fetchPaknsavePrices(searchTerm: string): Promise<PriceComparisonData[]> {
  try {
    let token = await getPaknsaveToken()
    
    // Attempt the search request
    let response = await makeSearchRequest(searchTerm, token)

    // Handle token expiration (retry once)
    if (response.status === 401) {
      console.warn('PaknSave token expired, refreshing...')
      cachedToken = null
      token = await getPaknsaveToken()
      response = await makeSearchRequest(searchTerm, token)
    }

    const body = response.body as PaknsaveSearchResponse
    const products = body.products || []
    
    return products.map((p) => {
      // Pak'nSave uses the same backend structure as New World (Foodstuffs)
      const simpleId = p.productId.split('-')[0]
      return {
        product_name: p.name,
        image_url: p.images?.primaryImages?.['400px'] || `https://a.fsimg.co.nz/product/retail/fan/image/400x400/${simpleId}.png`,
        supermarket_name: "Pak'nSave",
        logo_url: '/images/pak-n-save.webp',
        address: 'Henderson, West Auckland', 
        lat: -36.8819,
        lng: 174.6336,
        price: (p.singlePrice?.price || 0) / 100, // Price is in cents, convert to dollars
      }
    })

  } catch (error: any) {
    console.error('PaknSave Real-time API failed:', error.message)
    return []
  }
}

/**
 * Helper to perform the actual HTTP POST to the search API.
 */
async function makeSearchRequest(searchTerm: string, token: string) {
  return request
    .post('https://api-prod.paknsave.co.nz/v1/edge/search/paginated/products')
    .set('Authorization', `Bearer ${token}`)
    .set('Content-Type', 'application/json')
    .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
    .ok(res => res.status < 500) // Don't throw on 401 so we can handle retry
    .send({
      algoliaQuery: {
        query: searchTerm,
      },
      storeId: DEFAULT_STORE_ID,
      hitsPerPage: 50,
      page: 0,
      sortOrder: 'NI_POPULARITY_ASC'
    })
}
