import { fetchFoodstuffsPrices, FoodstuffsConfig, FetchOptions } from './foodstuffs-base.ts'
import { PriceComparisonData } from '../../models/products'

const config: FoodstuffsConfig = {
  domain: 'paknsave.co.nz',
  apiDomain: 'api-prod.paknsave.co.nz',
  storeId: '65defcf2-bc15-490e-a84f-1f13b769cd22', // Henderson Pak'nSave
  supermarketName: "Pak'nSave",
  logoUrl: '/images/pak-n-save.webp',
  defaultAddress: 'Henderson, West Auckland',
  defaultLat: -36.8819,
  defaultLng: 174.6336,
}

/**
 * Fetches real-time prices from Pak'nSave using the shared Foodstuffs base.
 */
export async function fetchPaknsavePrices(
  searchTerm: string,
  options?: FetchOptions,
): Promise<PriceComparisonData[]> {
  return fetchFoodstuffsPrices(searchTerm, config, options)
}
