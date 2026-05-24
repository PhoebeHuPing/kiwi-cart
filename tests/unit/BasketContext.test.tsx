import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { BasketProvider, useBasket } from '../../client/contexts/BasketContext'
import React from 'react'

describe('BasketContext Quantity Management', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('initializes with an empty basket', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })
    expect(result.current.basket).toEqual([])
  })

  it('adds a new item with quantity 1', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    act(() => {
      result.current.addToBasket({ name: 'Apple', image_url: 'apple.jpg' })
    })

    expect(result.current.basket).toEqual([
      { name: 'Apple', image_url: 'apple.jpg', quantity: 1 }
    ])
  })

  it('increments quantity when adding the same item twice', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    act(() => {
      result.current.addToBasket({ name: 'Apple', image_url: 'apple.jpg' })
      result.current.addToBasket({ name: 'Apple', image_url: 'apple.jpg' })
    })

    expect(result.current.basket).toEqual([
      { name: 'Apple', image_url: 'apple.jpg', quantity: 2 }
    ])
  })

  it('updates quantity directly', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    act(() => {
      result.current.addToBasket({ name: 'Apple', image_url: 'apple.jpg' })
      result.current.updateQuantity('Apple', 5)
    })

    expect(result.current.basket[0].quantity).toBe(5)
  })

  it('removes item when quantity is set to 0', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    act(() => {
      result.current.addToBasket({ name: 'Apple', image_url: 'apple.jpg' })
      result.current.updateQuantity('Apple', 0)
    })

    expect(result.current.basket).toEqual([])
  })

  it('persists quantity to localStorage', () => {
    const item = { name: 'Banana', image_url: 'banana.jpg', quantity: 3 }
    localStorage.setItem('kiwicart_basket', JSON.stringify([item]))

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    expect(result.current.basket).toEqual([item])
  })
})
