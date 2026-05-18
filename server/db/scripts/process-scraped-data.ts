import * as fs from 'fs'
import db from '../connection.ts'

async function run() {
  const pnsData = JSON.parse(fs.readFileSync('pns_data.json', 'utf8'))
  const nwData = JSON.parse(fs.readFileSync('nw_data.json', 'utf8'))

  const extractStores = (obj: any, stores: any[] = [], contentstack: any[] = []) => {
    if (!obj || typeof obj !== 'object') return
    
    if (obj.title && obj.contactDetails && obj.contactDetails.latitude) {
      const csStore = contentstack.find((cs: any) => cs.url === obj.url)
      if (csStore && csStore.store_id) {
        stores.push({
          title: obj.title,
          lat: obj.contactDetails.latitude,
          lng: obj.contactDetails.longitude,
          store_id: csStore.store_id
        })
      }
    }

    for (const key in obj) {
      extractStores(obj[key], stores, contentstack)
    }
  }

  const processFile = (data: any, brand: string, logo: string) => {
    const stores: any[] = []
    const contentstack = data.props.pageProps.contentstackStores || []
    extractStores(data, stores, contentstack)
    
    return stores.map(s => ({
      name: `${brand === 'paknsave' ? "PAK'nSAVE" : 'New World'} ${s.title}`,
      address: 'New Zealand',
      lat: s.lat,
      lng: s.lng,
      external_store_id: s.store_id,
      brand: brand,
      logo_url: logo
    }))
  }

  const pnsStores = processFile(pnsData, 'paknsave', '/images/pak-n-save.webp')
  const nwStores = processFile(nwData, 'newworld', '/images/new-world.webp')

  const allStores = [...pnsStores, ...nwStores]
  console.log(`Processed ${allStores.length} Foodstuffs stores.`)

  for (const store of allStores) {
    await db('supermarkets')
      .insert(store)
      .onConflict('external_store_id')
      .merge()
  }

  console.log('Successfully updated Foodstuffs stores in database.')
  process.exit(0)
}

run()
