import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import SuggestionCards from '../../client/components/SuggestionCards'
import * as aiApi from '../../client/apis/ai'
import { BasketProvider, useBasket } from '../../client/contexts/BasketContext'
import { SuggestionsResponse } from '../../models/products'

vi.mock('../../client/apis/ai')
vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}))
vi.mock('@auth0/auth0-react', () => ({
  useAuth0: () => ({
    getAccessTokenSilently: vi.fn().mockResolvedValue('test-token'),
  }),
}))

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
})

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={queryClient}>
    <BasketProvider>{children}</BasketProvider>
  </QueryClientProvider>
)

const sampleResponse: SuggestionsResponse = {
  total_potential_saving: 0.5,
  items: [
    {
      product: 'milk',
      reason: 'A staple you buy often',
      potential_saving: 0.5,
      cheapest: {
        product_name: 'Milk 2L',
        image_url: 'milk.jpg',
        supermarket_name: 'PakNSave',
        logo_url: 'logo.png',
        address: '1 Road',
        lat: -36.8,
        lng: 174.7,
        price: 3.0,
      },
    },
    {
      product: 'saffron',
      reason: 'For your paella',
      potential_saving: null,
      cheapest: null,
    },
  ],
}

describe('SuggestionCards Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    queryClient.clear()
  })

  it('renders suggestions with reason, price and saving', async () => {
    vi.mocked(aiApi.getSuggestions).mockResolvedValue(sampleResponse)

    render(<SuggestionCards />, { wrapper })

    await waitFor(() => {
      expect(screen.getByText('milk')).toBeDefined()
    })

    expect(aiApi.getSuggestions).toHaveBeenCalledWith('test-token')
    expect(screen.getByText('A staple you buy often')).toBeDefined()
    expect(screen.getByText(/Save up to \$0\.50/)).toBeDefined()
    // Unmatched suggestion is shown but marked as no match.
    expect(screen.getByText('saffron')).toBeDefined()
    expect(screen.getByText(/No match found/i)).toBeDefined()
  })

  it('shows the empty state when there are no suggestions', async () => {
    vi.mocked(aiApi.getSuggestions).mockResolvedValue({
      total_potential_saving: 0,
      items: [],
    })

    render(<SuggestionCards />, { wrapper })

    await waitFor(() => {
      expect(screen.getByText(/Favorite a few products/i)).toBeDefined()
    })
  })

  it('adds a matched suggestion to the basket', async () => {
    vi.mocked(aiApi.getSuggestions).mockResolvedValue(sampleResponse)

    let basketNames: string[] = []
    function BasketProbe() {
      basketNames = useBasket().basket.map((i) => i.name)
      return null
    }

    render(
      <>
        <SuggestionCards />
        <BasketProbe />
      </>,
      { wrapper },
    )

    const addButton = await screen.findByRole('button', {
      name: /Add Milk 2L to basket/i,
    })
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(basketNames).toContain('Milk 2L')
    })
  })
})
