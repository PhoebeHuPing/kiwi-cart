import express from 'express'
import * as db from '../db/index.ts'
import { fetchPaknsavePrices } from '../services/paknsave.ts'
import { fetchNewWorldPrices } from '../services/newworld.ts'
import { fetchWoolworthsPrices } from '../services/woolworths.ts'
import { checkJwt } from '../auth0.ts'

const router = express.Router()

/**
 * Extracts quantity and unit from product names and calculates unit price.
 * Handles patterns like "1kg", "500g", "2L", "750ml", "12 x 330ml".
 */
function calculateUnitPrice(name: string, price: number): string | null {
  if (!price || price <= 0) return null

  const normalized = name.toLowerCase()

  // Case 1: Multi-packs (e.g., "12 x 330ml")
  const multipackMatch = normalized.match(/(\d+)\s*x\s*(\d+(?:\.\d+)?)\s*(ml|l|g|kg)/)
  if (multipackMatch) {
    const packCount = parseInt(multipackMatch[1])
    const qty = parseFloat(multipackMatch[2])
    const unit = multipackMatch[3]
    const totalQty = packCount * qty
    return formatPriceByUnit(totalQty, unit, price)
  }

  // Case 2: Standard single units (e.g., "500g", "2L", "1.5kg")
  const singleMatch = normalized.match(/(\d+(?:\.\d+)?)\s*(ml|l|g|kg)/)
  if (singleMatch) {
    const qty = parseFloat(singleMatch[1])
    const unit = singleMatch[2]
    return formatPriceByUnit(qty, unit, price)
  }

  return null
}

/**
 * Formats the price based on quantity and unit (e.g., /100g, /kg, /100ml, /L).
 * Standardizes units for consistent price comparison.
 */
function formatPriceByUnit(
  qty: number,
  unit: string,
  totalPrice: number,
): string | null {
  if (qty <= 0) return null

  switch (unit) {
    case 'g': {
      // Standardize to price per 100g
      const pricePer100g = (totalPrice / qty) * 100
      return `$${pricePer100g.toFixed(2)}/100g`
    }
    case 'kg': {
      // Standardize to price per kg
      const pricePerKg = totalPrice / qty
      return `$${pricePerKg.toFixed(2)}/kg`
    }
    case 'ml': {
      // Standardize to price per 100ml
      const pricePer100ml = (totalPrice / qty) * 100
      return `$${pricePer100ml.toFixed(2)}/100ml`
    }
    case 'l': {
      // Standardize to price per L
      const pricePerL = totalPrice / qty
      return `$${pricePerL.toFixed(2)}/L`
    }
    default:
      return null
  }
}

/**
 * GET /api/v1/products/compare
 * Core endpoint for price comparison. It fetches cached results from the DB
 * and real-time results from supermarket scrapers/APIs in parallel.
 */
router.get('/compare', async (req, res) => {
  const searchTerm = (req.query.q as string) || 'Milk'
  console.log(`Searching for: ${searchTerm}`)

  try {
    // 2. Fetch real-time prices from all major brands in parallel
    const [pnsResults, nwResults, wwResults] = await Promise.all([
      fetchPaknsavePrices(searchTerm),
      fetchNewWorldPrices(searchTerm),
      fetchWoolworthsPrices(searchTerm),
    ])

    // 3. Combine real-time results, calculate unit prices, and sort
    const combined = [...pnsResults, ...nwResults, ...wwResults]
      .map((item) => ({
        ...item,
        unit_price: calculateUnitPrice(item.product_name, item.price),
      }))
      .sort((a, b) => a.price - b.price)

    res.json(combined)
  } catch (error) {
    console.error('Comparison route failed:', error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * POST /api/v1/products/compare-basket
 * Calculates the total cost of a basket across different supermarket brands.
 */
router.post('/compare-basket', async (req, res) => {
  const { items } = req.body as { items: { name: string; quantity: number }[] }

  if (!items || !Array.isArray(items)) {
    return res.status(400).send('Invalid basket items')
  }

  try {
    // 1. For each item in the basket, fetch prices across all supermarkets in real-time
    const pricePromises = items.map(async (item) => {
      const [pnsItems, nwItems, wwItems] = await Promise.all([
        fetchPaknsavePrices(item.name),
        fetchNewWorldPrices(item.name),
        fetchWoolworthsPrices(item.name),
      ])

      const results = [...pnsItems, ...nwItems, ...wwItems]

      // Filter results to only keep the best match per supermarket for this specific item
      const bestPerSupermarket = results.reduce(
        (acc: Record<string, any>, curr) => {
          if (
            !acc[curr.supermarket_name] ||
            curr.price < acc[curr.supermarket_name].price
          ) {
            acc[curr.supermarket_name] = curr
          }
          return acc
        },
        {},
      )
      return {
        itemName: item.name,
        quantity: item.quantity,
        prices: bestPerSupermarket,
      }
    })

    const basketPriceResults = await Promise.all(pricePromises)

    // 2. Group by supermarket and calculate totals
    const supermarketTotals: Record<string, any> = {}

    basketPriceResults.forEach((result) => {
      Object.keys(result.prices).forEach((sName) => {
        const info = result.prices[sName]
        if (!supermarketTotals[sName]) {
          supermarketTotals[sName] = {
            supermarket_name: sName,
            logo_url: info.logo_url,
            total_price: 0,
            items_found: 0,
            missing_items: [],
            details: [],
          }
        }
        supermarketTotals[sName].total_price += info.price * result.quantity
        supermarketTotals[sName].items_found += 1
        supermarketTotals[sName].details.push({
          name: result.itemName,
          price: info.price,
          quantity: result.quantity,
          subtotal: info.price * result.quantity,
        })
      })
    })

    // 3. Identify missing items for each supermarket
    const allSupermarketNames = Object.keys(supermarketTotals)
    allSupermarketNames.forEach((sName) => {
      const foundItemNames = supermarketTotals[sName].details.map((d: any) => d.name)
      supermarketTotals[sName].missing_items = items
        .filter((item) => !foundItemNames.includes(item.name))
        .map((item) => item.name)
    })

    res.json(Object.values(supermarketTotals))
  } catch (error) {
    console.error('Basket comparison failed:', error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * GET /api/v1/products/supermarkets
 * Returns a list of all supermarkets in the system.
 */
router.get('/supermarkets', async (req, res) => {
  try {
    const supermarkets = await db.getSupermarkets()
    res.json(supermarkets)
  } catch (error) {
    console.error(error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * GET /api/v1/products/nearby
 * Returns supermarkets within a specific radius of given coordinates.
 */
router.get('/nearby', async (req, res) => {
  try {
    const lat = parseFloat(req.query.lat as string)
    const lng = parseFloat(req.query.lng as string)
    const radius = parseFloat((req.query.radius as string) || '5')

    if (isNaN(lat) || isNaN(lng)) {
      return res.status(400).send('Invalid coordinates')
    }

    const supermarkets = await db.getNearbySupermarkets(lat, lng, radius)
    res.json(supermarkets)
  } catch (error) {
    console.error(error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * GET /api/v1/products
 * Returns all base products.
 */
router.get('/', async (req, res) => {
  try {
    const products = await db.getProducts()
    res.json(products)
  } catch (error) {
    console.error(error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * DELETE /api/v1/products/:id
 * Removes a product from the database by its ID.
 * Protected: Requires a valid Auth0 Access Token.
 */
router.delete('/:id', checkJwt, async (req, res) => {
  try {
    const id = Number(req.params.id)
    await db.deleteProductById(id)
    res.sendStatus(200)
  } catch (error) {
    res.status(500).send('Something went wrong')
  }
})

/**
 * GET /api/v1/products/favorites
 * Returns a list of product names favorited by the current authenticated user.
 */
router.get('/favorites', checkJwt, async (req, res) => {
  try {
    const userId = req.auth?.payload.sub
    if (!userId) return res.status(401).send('Unauthorized')

    const favorites = await db.getFavorites(userId)
    res.json(favorites.map((f) => f.product_name))
  } catch (error) {
    console.error('Failed to fetch favorites:', error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * POST /api/v1/products/favorites
 * Toggles a product in the user's favorites list.
 * Expects { name: string } in request body.
 */
router.post('/favorites', checkJwt, async (req, res) => {
  try {
    const userId = req.auth?.payload.sub
    const { name } = req.body
    
    if (!userId) return res.status(401).send('Unauthorized')
    if (!name) return res.status(400).send('Product name is required')

    const existing = await db.getFavorites(userId)
    const isAlreadyFavorite = existing.some((f) => f.product_name === name)

    if (isAlreadyFavorite) {
      await db.removeFavorite(userId, name)
      res.json({ action: 'removed', name })
    } else {
      await db.addFavorite(userId, name)
      res.json({ action: 'added', name })
    }
  } catch (error) {
    console.error('Failed to toggle favorite:', error)
    res.status(500).send('Something went wrong')
  }
})

export default router
