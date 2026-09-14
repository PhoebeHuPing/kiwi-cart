import express from 'express'
import * as db from '../db/index.ts'
import { fetchPaknsavePrices } from '../services/paknsave.ts'
import { fetchNewWorldPrices } from '../services/newworld.ts'
import { fetchWoolworthsPrices } from '../services/woolworths.ts'
import { checkJwt } from '../auth0.ts'
import { calculateUnitPrice } from '../utils/price-calculator.ts'

const router = express.Router()

/**
 * GET /api/v1/products/compare
 * Core endpoint for price comparison. It fetches cached results from the DB
 * and fallback to real-time results if cache is missing or stale (> 24h).
 * 
 * Query params:
 *   - q: search term (default: 'Milk')
 *   - lat: user latitude (optional)
 *   - lng: user longitude (optional)
 * 
 * When lat/lng provided, resolves the nearest store within 5km for each brand
 * that supports per-store pricing (Pak'nSave, New World). Stores beyond 5km
 * are dropped. Woolworths uses national pricing (no store-specific lookup).
 */
router.get('/compare', async (req, res) => {
  const searchTerm = (req.query.q as string) || 'Milk'
  const lat = req.query.lat ? parseFloat(req.query.lat as string) : undefined
  const lng = req.query.lng ? parseFloat(req.query.lng as string) : undefined

  const hasLocation = lat !== undefined && lng !== undefined && !isNaN(lat!) && !isNaN(lng!)
  console.log(`Searching for: ${searchTerm}, location: ${hasLocation ? `${lat},${lng}` : 'none'}`)

  // Nearby radius in km (must match .NET's StoreService.NearbyRadiusKm)
  const NEARBY_RADIUS_KM = 5

  try {
    // 1. Try to fetch from local cache (Database)
    const cachedResults = await db.getComparePrices(searchTerm)
    const CACHE_EXPIRY_MS = 24 * 60 * 60 * 1000 // 24 hours

    const isCacheFresh = 
      cachedResults.length > 0 && 
      cachedResults.every(r => r.updated_at && (Date.now() - new Date(r.updated_at).getTime() < CACHE_EXPIRY_MS))

    if (isCacheFresh) {
      console.log('Serving from cache...')
      return res.json(cachedResults.map(item => ({
        ...item,
        unit_price: calculateUnitPrice(item.product_name, item.price)
      })))
    }

    console.log('Cache missing or stale. Fetching real-time prices...')

    // 2. Resolve nearest stores for brands that support per-store pricing (PNS, NW)
    // Drop the brand if no store within 5km
    const selectedStores: Record<string, {
      storeId: string;
      name: string;
      address: string;
      lat: number;
      lng: number;
    }> = {}
    const droppedBrands: string[] = []

    if (hasLocation) {
      // Pak'nSave: find nearest within 5km
      const pnsStore = await db.getNearestStoreWithExternalId('PakNSave', lat!, lng!, NEARBY_RADIUS_KM)
      if (pnsStore) {
        selectedStores['PakNSave'] = {
          storeId: pnsStore.external_store_id,
          name: pnsStore.name,
          address: pnsStore.address,
          lat: pnsStore.latitude,
          lng: pnsStore.longitude
        }
        console.log(`PNS: selected ${pnsStore.name} (${pnsStore.external_store_id}) within 5km`)
      } else {
        droppedBrands.push('PakNSave')
        console.log('PNS: no store within 5km, dropping')
      }

      // New World: find nearest within 5km
      const nwStore = await db.getNearestStoreWithExternalId('NewWorld', lat!, lng!, NEARBY_RADIUS_KM)
      if (nwStore) {
        selectedStores['NewWorld'] = {
          storeId: nwStore.external_store_id,
          name: nwStore.name,
          address: nwStore.address,
          lat: nwStore.latitude,
          lng: nwStore.longitude
        }
        console.log(`NW: selected ${nwStore.name} (${nwStore.external_store_id}) within 5km`)
      } else {
        droppedBrands.push('NewWorld')
        console.log('NW: no store within 5km, dropping')
      }

      // Note: Woolworths uses national pricing (no store-specific lookup) - it's always included
    }

    // 3. Fetch real-time prices from all major brands in parallel
    // Pass store-specific info when available
    const pnsOptions = selectedStores['PakNSave'] ? {
      storeId: selectedStores['PakNSave'].storeId,
      address: selectedStores['PakNSave'].address,
      lat: selectedStores['PakNSave'].lat,
      lng: selectedStores['PakNSave'].lng
    } : undefined

    const nwOptions = selectedStores['NewWorld'] ? {
      storeId: selectedStores['NewWorld'].storeId,
      address: selectedStores['NewWorld'].address,
      lat: selectedStores['NewWorld'].lat,
      lng: selectedStores['NewWorld'].lng
    } : undefined

    const [pnsResults, nwResults, wwResults] = await Promise.all([
      pnsOptions ? fetchPaknsavePrices(searchTerm, pnsOptions) : Promise.resolve([]),
      nwOptions ? fetchNewWorldPrices(searchTerm, nwOptions) : Promise.resolve([]),
      fetchWoolworthsPrices(searchTerm),
    ])

    // 4. Combine results (PNS/NW may be empty if dropped)
    const combined = [...pnsResults, ...nwResults, ...wwResults]
      .map((item) => ({
        ...item,
        unit_price: calculateUnitPrice(item.product_name, item.price),
      }))
      .sort((a, b) => a.price - b.price)

    // 5. Update the cache in the background (don't block the response)
    // We only upsert the top results or unique items to keep the DB clean
    // Note: this cache write is NOT location-aware (same bug as .NET)
    combined.forEach(item => {
      db.upsertPrice({
        product_name: item.product_name,
        image_url: item.image_url,
        supermarket_name: item.supermarket_name,
        price: item.price
      }).catch(err => console.error('Failed to background update cache:', err))
    })

    res.json(combined)
  } catch (error) {
    console.error('Comparison route failed:', error)
    res.status(500).send('Something went wrong')
  }
})

/**
 * POST /api/v1/products/compare-bucket
 * Calculates the total cost of a basket across different supermarket brands.
 */
router.post('/compare-bucket', async (req, res) => {
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
