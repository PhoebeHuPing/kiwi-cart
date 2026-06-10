import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ProductComparison from '../../client/components/ProductComparison'
import * as api from '../../client/apis/products'
import React from 'react'
import { BasketProvider } from '../../client/contexts/BasketContext'

vi.mock('../../client/apis/products')

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={queryClient}>
    <BasketProvider>{children}</BasketProvider>
  </QueryClientProvider>
)

describe('ProductComparison Component', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    queryClient.clear()
    vi.mocked(api.getSupermarkets).mockResolvedValue([])
  })

  it('renders search input and daily essentials header', () => {
    vi.mocked(api.getComparePrices).mockResolvedValue([])
    render(<ProductComparison />, { wrapper })
    
    expect(screen.getByPlaceholderText(/Search for a product/i)).toBeDefined()
    expect(screen.getByText(/Daily Essentials/i)).toBeDefined()
  })

  it('displays products when they are fetched', async () => {
    const mockProducts = [
      {
        product_name: 'Apple',
        price: 3.5,
        supermarket_name: 'Woolworths',
        image_url: 'apple.jpg',
        logo_url: 'logo.png',
        address: '123 Street',
        lat: -36.8485,
        lng: 174.7633
      }
    ]
    vi.mocked(api.getComparePrices).mockResolvedValue(mockProducts)

    render(<ProductComparison />, { wrapper })

    // Wait for the query to resolve
    await waitFor(() => {
      expect(screen.getByText('Apple')).toBeDefined()
    })

    expect(screen.getByText('Best Price At')).toBeDefined()
    expect(screen.getByText('$3.50')).toBeDefined()
  })

  it('expands product details when clicked', async () => {
    const mockProducts = [
      {
        product_name: 'Apple',
        price: 3.5,
        supermarket_name: 'Woolworths',
        image_url: 'apple.jpg',
        logo_url: 'logo.png',
        address: '123 Street',
        lat: -36.8485,
        lng: 174.7633
      }
    ]
    vi.mocked(api.getComparePrices).mockResolvedValue(mockProducts)

    render(<ProductComparison />, { wrapper })

    // Wait for the query to resolve and find the 'View All Prices' button
    const viewAllPricesButton = await screen.findByText(/View All Prices/i)
    fireEvent.click(viewAllPricesButton)

    expect(screen.getByText('Available Store Prices')).toBeDefined()
    expect(screen.getAllByText('Woolworths')).toBeDefined()
    expect(screen.getByText(/123 Street/)).toBeDefined()
  })
})
