import request from 'superagent'
import db from '../connection.ts'

async function getToken(brand: 'paknsave' | 'newworld') {
  const domain = brand === 'paknsave' ? 'www.paknsave.co.nz' : 'www.newworld.co.nz'
  try {
    const response = await request
      .post(`https://${domain}/api/user/get-current-user`)
      .set('User-Agent', 'Mozilla/5.0')
      .send({})
    return response.body.access_token
  } catch (err: any) {
    console.error(`Failed to get token for ${brand}:`, err.message)
    return null
  }
}

async function fetchFoodstuffsStores(brand: 'paknsave' | 'newworld') {
  console.log(`Fetching ${brand} stores...`)
  const token = await getToken(brand)
  if (!token) return []

  const apiDomain = brand === 'paknsave' ? 'api-prod.paknsave.co.nz' : 'api-prod.newworld.co.nz'
  try {
    const response = await request
      .get(`https://${apiDomain}/v1/edge/stores`)
      .set('Authorization', `Bearer ${token}`)
      .set('User-Agent', 'Mozilla/5.0')
    
    // The response is usually an array of store objects
    const stores = response.body || []
    return stores.map((s: any) => ({
      name: s.name,
      address: s.address,
      lat: s.latitude,
      lng: s.longitude,
      external_store_id: s.storeId,
      brand: brand,
      logo_url: brand === 'paknsave' ? '/images/pak-n-save.webp' : '/images/new-world.webp'
    }))
  } catch (err: any) {
    console.error(`Error fetching ${brand} stores:`, err.message)
    return []
  }
}

async function fetchWoolworthsStores() {
  console.log('Fetching Woolworths stores...')
  try {
    // Woolworths might work with just a user agent now
    const response = await request
      .get('https://www.woolworths.co.nz/api/v1/fulfilment/my/pickup-addresses')
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('X-Requested-With', 'OnlineShopping.WebApp')
    
    const stores = response.body.addresses || []
    return stores.map((s: any) => ({
      name: s.name,
      address: s.address,
      lat: s.latitude,
      lng: s.longitude,
      external_store_id: s.addressId?.toString(),
      brand: 'woolworths',
      logo_url: '/images/woolworths.webp'
    }))
  } catch (err: any) {
    console.error('Error fetching Woolworths stores:', err.message)
    // Fallback: If the API fails, maybe we can find a static list or another endpoint
    return []
  }
}

async function run() {
  const pns = await fetchFoodstuffsStores('paknsave')
  const nw = await fetchFoodstuffsStores('newworld')
  const ww = await fetchWoolworthsStores()

  const allStores = [...pns, ...nw, ...ww]
  console.log(`Total stores found: ${allStores.length}`)

  if (allStores.length === 0) {
    console.log('No stores found. Exiting.')
    process.exit(0)
  }

  // Filter for Auckland stores if possible, or just add all
  // Auckland lat is around -36.8, lng around 174.7
  const aucklandStores = allStores.filter(s => 
    s.lat < -36.5 && s.lat > -37.3 && s.lng > 174.4 && s.lng < 175.2
  )
  
  console.log(`Found ${aucklandStores.length} stores in Auckland area.`)

  const storesToUse = aucklandStores.length > 0 ? aucklandStores : allStores

  for (const store of storesToUse) {
    if (!store.lat || !store.lng || !store.external_store_id) continue

    await db('supermarkets')
      .insert(store)
      .onConflict('external_store_id')
      .merge()
  }

  console.log('Successfully updated supermarkets table.')
  process.exit(0)
}

run()
