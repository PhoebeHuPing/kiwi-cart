import express from 'express'
import * as db from '../db/index.ts'
import { fetchPaknsavePrices } from '../services/paknsave.ts'
import { fetchNewWorldPrices } from '../services/newworld.ts'

const router = express.Router()

/**
 * GET /api/v1/products/compare
 * Core endpoint for price comparison. It fetches cached results from the DB
 * and real-time results from supermarket scrapers/APIs in parallel.
 */
router.get('/compare', async (req, res) => {
  const searchTerm = (req.query.q as string) || 'Milk'
  console.log(`Searching for: ${searchTerm}`)

  try {
    // 2. Fetch real-time prices from Foodstuffs brands in parallel
    const [pnsResults, nwResults] = await Promise.all([
      fetchPaknsavePrices(searchTerm),
      fetchNewWorldPrices(searchTerm),
    ])

    // 3. Combine only real-time results and sort by price (ascending)
    const combined = [...pnsResults, ...nwResults].sort(
      (a, b) => a.price - b.price,
    )

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
      const [pnsItems, nwItems] = await Promise.all([
        fetchPaknsavePrices(item.name),
        fetchNewWorldPrices(item.name),
      ])

      const results = [...pnsItems, ...nwItems]

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

router.delete('/:id', async (req, res) => {
  try {
    const id = Number(req.params.id)
    await db.deleteProductById(id)
    res.sendStatus(200)
  } catch (error) {
    res.status(500).send('Something went wrong')
  }
})

export default router
