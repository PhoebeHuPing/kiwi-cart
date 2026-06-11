import db from './connection.ts'
import { Product, PriceComparisonData } from '../../models/products'

/**
 * Fetches all base products from the database.
 */
export async function getProducts(): Promise<Product[]> {
  return db('products').select('*')
}

/**
 * Executes a complex join to get price comparison data.
 * Merges product info, supermarket info, and price info for a specific search term.
 * Results are sorted by price in ascending order to show the cheapest first.
 */
export async function getComparePrices(
  searchTerm: string,
): Promise<(PriceComparisonData & { updated_at: string | null })[]> {
  return db('products')
    .join('prices', 'products.id', 'prices.product_id')
    .join('supermarkets', 'supermarkets.id', 'prices.supermarket_id')
    .select(
      'products.name as product_name',
      'products.image_url',
      'supermarkets.name as supermarket_name',
      'supermarkets.logo_url',
      'supermarkets.address',
      'supermarkets.lat',
      'supermarkets.lng',
      'prices.price',
      'prices.updated_at',
    )
    .where('products.name', 'like', `%${searchTerm}%`)
    .orderBy('prices.price', 'asc')
}

/**
 * Updates or inserts a price record for a product at a specific supermarket.
 * This is the core of our cache/community-driven data system.
 */
export async function upsertPrice(data: {
  product_name: string
  image_url: string
  supermarket_name: string
  price: number
}): Promise<void> {
  // 1. Ensure the supermarket exists (or get its ID)
  const supermarket = await db('supermarkets')
    .where('name', 'like', `%${data.supermarket_name}%`)
    .first()
  
  if (!supermarket) {
    console.warn(`Supermarket ${data.supermarket_name} not found in DB`)
    return
  }

  // 2. Ensure the product exists (or create it)
  let product = await db('products')
    .where('name', data.product_name)
    .first()
  
  if (!product) {
    const [id] = await db('products').insert({
      name: data.product_name,
      image_url: data.image_url,
      category: 'General',
    })
    product = { id }
  }

  // 3. Upsert the price
  const existingPrice = await db('prices')
    .where('product_id', product.id)
    .andWhere('supermarket_id', supermarket.id)
    .first()

  if (existingPrice) {
    await db('prices')
      .where('id', existingPrice.id)
      .update({
        price: data.price,
        updated_at: new Date().toISOString(),
      })
  } else {
    await db('prices').insert({
      product_id: product.id,
      supermarket_id: supermarket.id,
      price: data.price,
      updated_at: new Date().toISOString(),
    })
  }
}

/**
 * Fetches all supermarkets.
 */
export async function getSupermarkets() {
  return db('supermarkets').select('*')
}

/**
 * Finds supermarkets within a specific radius (in km) using the Haversine formula.
 * This is used for location-based store discovery.
 */
export async function getNearbySupermarkets(
  lat: number,
  lng: number,
  radiusKm: number,
) {
  const allStores = await db('supermarkets').select('*')
  return allStores.filter((store) => {
    const distance = calculateDistance(lat, lng, store.lat, store.lng)
    return distance <= radiusKm
  })
}

/**
 * Haversine formula to calculate the great-circle distance between two points on a sphere.
 * Returns distance in kilometers.
 */
function calculateDistance(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number,
) {
  const R = 6371 // Radius of the earth in km
  const dLat = deg2rad(lat2 - lat1)
  const dLon = deg2rad(lon2 - lon1)
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(deg2rad(lat1)) * Math.cos(deg2rad(lat2)) *
    Math.sin(dLon / 2) * Math.sin(dLon / 2)
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
  return R * c // Distance in km
}

function deg2rad(deg: number) {
  return deg * (Math.PI / 180)
}

/**
 * Deletes a product by its ID.
 */
export async function deleteProductById(id: number): Promise<number> {
  return db('products').where('id', id).delete()
}

/**
 * Fetches all favorited product names for a specific user.
 */
export async function getFavorites(userId: string): Promise<{ product_name: string }[]> {
  return db('favorites')
    .where('user_id', userId)
    .select('product_name')
    .orderBy('created_at', 'desc')
}

/**
 * Adds a product to the user's favorites.
 * Uses a raw insert or simple check to handle unique constraint gracefully if needed, 
 * but here we assume the unique constraint will handle duplicates.
 */
export async function addFavorite(userId: string, productName: string): Promise<void> {
  await db('favorites').insert({
    user_id: userId,
    product_name: productName,
  })
}

/**
 * Removes a product from the user's favorites.
 */
export async function removeFavorite(userId: string, productName: string): Promise<void> {
  await db('favorites')
    .where('user_id', userId)
    .andWhere('product_name', productName)
    .delete()
}

/**
 * Fetches all feedback messages, newest first.
 */
export async function getFeedbackMessages() {
  return db('feedback_messages').select('*').orderBy('created_at', 'desc')
}

/**
 * Creates a new feedback message.
 */
export async function createFeedbackMessage(userId: string, userName: string, message: string) {
  return db('feedback_messages').insert({ user_id: userId, user_name: userName, message })
}
