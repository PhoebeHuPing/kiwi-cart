import request from 'superagent'
import { PriceComparisonData } from '../../models/products'

/**
 * Woolworths (formerly Countdown) Service
 * 
 * Note: Woolworths NZ uses a more traditional REST API than Foodstuffs.
 * It typically requires a session established via /api/v1/session.
 */

interface WoolworthsProduct {
  name: string
  price: {
    salePrice: number
  }
  images: {
    small: string
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
export async function fetchWoolworthsPrices(searchTerm: string): Promise<PriceComparisonData[]> {
  try {
    // 1. Establish session (if needed, though simple GET might work for public search)
    // For simplicity in this first iteration, we'll try a direct search.
    // Woolworths often requires a User-Agent and certain headers.
    
    const response = await request
      .get('https://www.woolworths.co.nz/api/v1/products')
      .query({ 
        target: 'search', 
        search: searchTerm,
        inStockProductsOnly: true 
      })
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('Accept', 'application/json')

    const body = response.body as WoolworthsSearchResponse
    const items = body.products?.items || []

    return items
      .filter(item => item.type === 'Product') // Filter out non-product items (like banners)
      .map((p) => ({
        product_name: p.name,
        image_url: p.images?.small,
        supermarket_name: 'Woolworths',
        logo_url: '/images/woolworths.webp',
        address: 'Grey Lynn, Auckland', // Default for now
        lat: -36.8645,
        lng: 174.7431,
        price: p.price?.salePrice || 0,
      }))
  } catch (error: any) {
    console.error('Woolworths Real-time API failed:', error.message)
    return []
  }
}
