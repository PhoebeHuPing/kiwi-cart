import { fetchFoodstuffsPrices, FoodstuffsConfig } from './foodstuffs-base.ts'
import { PriceComparisonData } from '../../models/products'

const config: FoodstuffsConfig = {
  domain: 'newworld.co.nz',
  apiDomain: 'api-prod.newworld.co.nz',
  storeId: 'dbdfdd2a-55f7-4870-9b51-979286323647', // Browns Bay New World
  supermarketName: 'New World',
  logoUrl: '/images/new-world.webp',
  defaultAddress: 'Victoria Park, Auckland',
  defaultLat: -36.8485,
  defaultLng: 174.7523,
}

/**
 * Fetches real-time prices from New World using the shared Foodstuffs base.
 */
export async function fetchNewWorldPrices(searchTerm: string): Promise<PriceComparisonData[]> {
  return fetchFoodstuffsPrices(searchTerm, config)
}
