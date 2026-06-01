import request from 'superagent'
import { PriceComparisonData } from '../../models/products'

/**
 * Woolworths (formerly Countdown) Service
 *
 * Note: Woolworths NZ uses a more traditional REST API than Foodstuffs.
 * It typically requires a session established via /api/v1/session.
 */

/**
 * Represents the product structure returned by Woolworths NZ (formerly Countdown).
 */
interface WoolworthsProduct {
  name: string
  price: {
    salePrice: number
  }
  images: {
    small: string
    big: string
  }
  type: string
}

interface WoolworthsSearchResponse {
  products: {
    items: WoolworthsProduct[]
  }
}

/**
 * Fetches real-time prices from Woolworths.
 */
export async function fetchWoolworthsPrices(
  searchTerm: string,
): Promise<PriceComparisonData[]> {
  try {
    // 1. Establish session
    // Woolworths NZ requires a session to be established via /api/v1/session
    // and maintaining cookies for subsequent requests.
    const agent = request.agent()

    await agent
      .post('https://www.woolworths.co.nz/api/v1/session')
      .set(
        'User-Agent',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
      )
      .set('X-Requested-With', 'OnlineShopping.WebApp')
      .send({})

    // 2. Search for products
    const response = await agent
      .get('https://www.woolworths.co.nz/api/v1/products')
      .query({
        target: 'search',
        search: searchTerm,
        inStockProductsOnly: true,
      })
      .set(
        'User-Agent',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
      )
      .set('X-Requested-With', 'OnlineShopping.WebApp')
      .set('Accept', 'application/json')

    const body = response.body as WoolworthsSearchResponse
    const items = body.products?.items || []

    return items
      .filter((item) => item.type === 'Product') // Filter out non-product items (like banners or ads)
      .map((p) => {
        // Use the 'big' image and boost resolution to 400x400 to match Foodstuffs UI quality
        const highResImage =
          p.images?.big?.replace('w=200&h=200', 'w=400&h=400') ||
          p.images?.small

        return {
          product_name: p.name,
          image_url: highResImage,
          supermarket_name: 'Woolworths',
          logo_url: '/images/woolworths.webp',
          address: 'Grey Lynn, Auckland', // Default for now
          lat: -36.8645,
          lng: 174.7431,
          price: p.price?.salePrice || 0,
        }
      })
  } catch (error: any) {
    console.error('Woolworths Real-time API failed:', error.message)
    return []
  }
}
