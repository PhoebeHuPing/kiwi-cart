import { describe, it, expect, vi } from 'vitest'
import request from 'supertest'
import server from '../../server/server.ts'
import * as db from '../../server/db/index.ts'
import { fetchPaknsavePrices } from '../../server/services/paknsave.ts'
import { fetchNewWorldPrices } from '../../server/services/newworld.ts'
import { fetchWoolworthsPrices } from '../../server/services/woolworths.ts'

vi.mock('../../server/db/index.ts')
vi.mock('../../server/services/paknsave.ts')
vi.mock('../../server/services/newworld.ts')
vi.mock('../../server/services/woolworths.ts')

describe('GET /api/v1/products', () => {
  it('should return a list of products from the database', async () => {
    const mockProducts = [
      { id: 1, name: 'Test Product', category: 'Test', image_url: 'test.jpg' }
    ]
    vi.mocked(db.getProducts).mockResolvedValue(mockProducts)

    const response = await request(server).get('/api/v1/products')

    expect(response.status).toBe(200)
    expect(response.body).toEqual(mockProducts)
  })
})

describe('GET /api/v1/products/compare', () => {
  it('should return combined sorted results and calculate unit price', async () => {
    const mockPnsResults = [
      {
        product_name: 'Milk 2L',
        price: 5.0,
        supermarket_name: 'PaknSave',
        image_url: 'apple.jpg',
        logo_url: 'pns.png',
        address: 'PNS Address',
        lat: -36.0,
        lng: 174.0,
      },
    ]
    const mockNwResults = [
      {
        product_name: 'Butter 500g',
        price: 6.0,
        supermarket_name: 'New World',
        image_url: 'apple.jpg',
        logo_url: 'nw.png',
        address: 'NW Address',
        lat: -36.1,
        lng: 174.1,
      },
    ]

    vi.mocked(fetchPaknsavePrices).mockResolvedValue(mockPnsResults)
    vi.mocked(fetchNewWorldPrices).mockResolvedValue(mockNwResults)
    vi.mocked(fetchWoolworthsPrices).mockResolvedValue([])
    vi.mocked(db.getComparePrices).mockResolvedValue([])
    vi.mocked(db.upsertPrice).mockResolvedValue()

    const response = await request(server).get('/api/v1/products/compare?q=Milk')

    expect(response.status).toBe(200)
    expect(response.body).toHaveLength(2)
    
    // Check sorting and unit price calculation
    expect(response.body[0].product_name).toBe('Milk 2L')
    expect(response.body[0].unit_price).toBe('$2.50/L')
    
    expect(response.body[1].product_name).toBe('Butter 500g')
    expect(response.body[1].unit_price).toBe('$1.20/100g')
  })
})

describe('POST /api/v1/products/compare-bucket', () => {
  it('should calculate total cost across supermarkets and identify missing items', async () => {
    const mockBasket = {
      items: [
        { name: 'Milk', quantity: 2 },
        { name: 'Bread', quantity: 1 }
      ]
    }

    vi.mocked(fetchPaknsavePrices).mockImplementation((searchTerm: string) => {
      if (searchTerm === 'Milk') {
        return Promise.resolve([
          { supermarket_name: 'PaknSave', price: 2.5, logo_url: 'pns.png' }
        ] as any)
      }
      if (searchTerm === 'Bread') {
        return Promise.resolve([
          { supermarket_name: 'PaknSave', price: 4.0, logo_url: 'pns.png' }
        ] as any)
      }
      return Promise.resolve([])
    })

    vi.mocked(fetchNewWorldPrices).mockImplementation((searchTerm: string) => {
      if (searchTerm === 'Milk') {
        return Promise.resolve([
          { supermarket_name: 'New World', price: 3.0, logo_url: 'nw.png' }
        ] as any)
      }
      return Promise.resolve([])
    })

    vi.mocked(fetchWoolworthsPrices).mockResolvedValue([])

    const response = await request(server)
      .post('/api/v1/products/compare-bucket')
      .send(mockBasket)

    expect(response.status).toBe(200)
    
    const pns = response.body.find((s: any) => s.supermarket_name === 'PaknSave')
    const nw = response.body.find((s: any) => s.supermarket_name === 'New World')

    expect(pns.total_price).toBe(9.0)
    expect(pns.items_found).toBe(2)
    expect(nw.total_price).toBe(6.0)
    expect(nw.missing_items).toContain('Bread')
  })
})
