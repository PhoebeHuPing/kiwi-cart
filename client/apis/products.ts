import request from 'superagent'
import {
  Product,
  PriceComparisonData,
  BasketComparisonResult,
} from '../../models/products'

const rootURL = '/api/v1/products'

/**
 * Fetches all products from the local database.
 */
export async function getProducts(): Promise<Product[]> {
  const response = await request.get(rootURL)
  return response.body
}

/**
 * Core price comparison search. Queries the backend to fetch real-time
 * and cached prices across different supermarket brands.
 */
export async function getComparePrices(
  searchTerm: string,
): Promise<PriceComparisonData[]> {
  const response = await request
    .get(`${rootURL}/compare`)
    .query({ q: searchTerm })
  return response.body
}

/**
 * Compares the total price of a basket across different supermarkets.
 */
export async function compareBasket(
  items: { name: string; quantity: number }[],
): Promise<BasketComparisonResult[]> {
  const response = await request.post(`${rootURL}/compare-basket`).send({ items })
  return response.body
}

/**
 * Fetches a list of all supermarkets and their coordinates.
 */
export async function getSupermarkets() {
  const response = await request.get(`${rootURL}/supermarkets`)
  return response.body
}

/**
 * Fetches supermarkets within a specific radius of a given coordinate.
 */
export async function getNearbySupermarkets(
  lat: number,
  lng: number,
  radius = 5,
) {
  const response = await request
    .get(`${rootURL}/nearby`)
    .query({ lat, lng, radius })
  return response.body
}

/**
 * Fetches details for a single product by its ID.
 */
export async function getProductById(id: number): Promise<Product> {
  const response = await request.get(`${rootURL}/${id}`)
  return response.body
}

/**
 * Fetches the list of favorited product names for the current user.
 */
export async function getFavorites(token: string): Promise<string[]> {
  const response = await request
    .get(`${rootURL}/favorites`)
    .set('Authorization', `Bearer ${token}`)
  return response.body
}

/**
 * Toggles a product in the user's favorites list.
 */
export async function toggleFavorite(
  name: string,
  token: string,
): Promise<{ action: 'added' | 'removed'; name: string }> {
  const response = await request
    .post(`${rootURL}/favorites`)
    .set('Authorization', `Bearer ${token}`)
    .send({ name })
  return response.body
}
