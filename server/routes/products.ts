import express from 'express'
import * as db from '../db/index.ts'
import { fetchPaknsavePrices } from '../services/paknsave.ts'
import { fetchNewWorldPrices } from '../services/newworld.ts'

const router = express.Router()

router.get('/compare', async (req, res) => {
  const searchTerm = (req.query.q as string) || 'Milk'
  console.log(`Searching for: ${searchTerm}`)

  try {
    // 1. Get existing database results (for historical or other stores)
    const dbProducts = await db.getComparePrices(searchTerm)

    // 2. Fetch real-time prices from Foodstuffs brands in parallel

    const [pnsResults, nwResults] = await Promise.all([
      fetchPaknsavePrices(searchTerm),
      fetchNewWorldPrices(searchTerm),
    ])
    // console.log(pnsResults)
    // console.log(nwResults)

    // 3. Combine all results and sort by price
    const combined = [...dbProducts, ...pnsResults, ...nwResults].sort(
      (a, b) => a.price - b.price,
    )

    res.json(combined)
  } catch (error) {
    console.error('Comparison route failed:', error)
    res.status(500).send('Something went wrong')
  }
})

router.get('/supermarkets', async (req, res) => {
  try {
    const supermarkets = await db.getSupermarkets()
    res.json(supermarkets)
  } catch (error) {
    console.error(error)
    res.status(500).send('Something went wrong')
  }
})

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
