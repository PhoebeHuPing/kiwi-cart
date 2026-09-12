import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import AiAssistant from '../../client/components/AiAssistant'
import * as aiApi from '../../client/apis/ai'
import { BasketProvider, useBasket } from '../../client/contexts/BasketContext'
import { MealPlanResponse } from '../../models/products'

vi.mock('../../client/apis/ai')
vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}))

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
})

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={queryClient}>
    <BasketProvider>{children}</BasketProvider>
  </QueryClientProvider>
)

const sampleResponse: MealPlanResponse = {
  estimated_total: 11.5,
  items: [
    {
      ingredient: 'chicken breast',
      cheapest: {
        product_name: 'Chicken Breast 1kg',
        image_url: 'chicken.jpg',
        supermarket_name: 'PakNSave',
        logo_url: 'logo.png',
        address: '1 Road',
        lat: -36.8,
        lng: 174.7,
        price: 9.5,
      },
    },
    {
      ingredient: 'saffron',
      cheapest: null,
    },
  ],
}

describe('AiAssistant Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    queryClient.clear()
  })

  it('renders the prompt input', () => {
    render(<AiAssistant />, { wrapper })
    expect(
      screen.getByLabelText(/Describe a meal or shopping goal/i),
    ).toBeDefined()
  })

  it('submits a prompt and renders the costed shopping list', async () => {
    vi.mocked(aiApi.getMealPlan).mockResolvedValue(sampleResponse)

    render(<AiAssistant />, { wrapper })

    const input = screen.getByLabelText(/Describe a meal or shopping goal/i)
    fireEvent.change(input, { target: { value: 'chicken curry' } })
    fireEvent.click(screen.getByRole('button', { name: /^Ask$/i }))

    await waitFor(() => {
      expect(screen.getByText('chicken breast')).toBeDefined()
    })

    expect(aiApi.getMealPlan).toHaveBeenCalledWith('chicken curry')
    expect(screen.getByText(/Chicken Breast 1kg/)).toBeDefined()
    expect(screen.getByText(/Est\. \$11\.50/)).toBeDefined()
    // Unmatched ingredient is shown but marked as no match.
    expect(screen.getByText('saffron')).toBeDefined()
    expect(screen.getByText(/No match found/i)).toBeDefined()
  })

  it('adds a matched ingredient to the basket', async () => {
    vi.mocked(aiApi.getMealPlan).mockResolvedValue(sampleResponse)

    // Probe component to read basket state from context.
    let basketNames: string[] = []
    function BasketProbe() {
      basketNames = useBasket().basket.map((i) => i.name)
      return null
    }

    render(
      <>
        <AiAssistant />
        <BasketProbe />
      </>,
      { wrapper },
    )

    fireEvent.change(
      screen.getByLabelText(/Describe a meal or shopping goal/i),
      { target: { value: 'chicken curry' } },
    )
    fireEvent.click(screen.getByRole('button', { name: /^Ask$/i }))

    const addButton = await screen.findByRole('button', {
      name: /Add Chicken Breast 1kg to basket/i,
    })
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(basketNames).toContain('Chicken Breast 1kg')
    })
  })

  it('shows an empty-state message when no ingredients are matched', async () => {
    vi.mocked(aiApi.getMealPlan).mockResolvedValue({
      estimated_total: 0,
      items: [],
    })

    render(<AiAssistant />, { wrapper })

    fireEvent.change(
      screen.getByLabelText(/Describe a meal or shopping goal/i),
      { target: { value: '???' } },
    )
    fireEvent.click(screen.getByRole('button', { name: /^Ask$/i }))

    await waitFor(() => {
      expect(screen.getByText(/Couldn't work out any ingredients/i)).toBeDefined()
    })
  })
})
