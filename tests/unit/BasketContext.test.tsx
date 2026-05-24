import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { BasketProvider, useBasket } from '../../client/contexts/BasketContext'
import React from 'react'

describe('BasketContext Persistence', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('initializes with an empty basket if localStorage is empty', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    expect(result.current.basket).toEqual([])
  })

  it('saves to localStorage when an item is added', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    const item = { name: 'Apple', image_url: 'apple.jpg' }
    
    act(() => {
      result.current.addToBasket(item)
    })

    expect(result.current.basket).toEqual([item])
    expect(localStorage.getItem('kiwicart_basket')).toEqual(JSON.stringify([item]))
  })

  it('initializes with data from localStorage', () => {
    const item = { name: 'Banana', image_url: 'banana.jpg' }
    localStorage.setItem('kiwicart_basket', JSON.stringify([item]))

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    expect(result.current.basket).toEqual([item])
  })

  it('removes from localStorage when an item is removed', () => {
    const item = { name: 'Apple', image_url: 'apple.jpg' }
    localStorage.setItem('kiwicart_basket', JSON.stringify([item]))

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <BasketProvider>{children}</BasketProvider>
    )
    const { result } = renderHook(() => useBasket(), { wrapper })

    act(() => {
      result.current.removeFromBasket('Apple')
    })

    expect(result.current.basket).toEqual([])
    expect(localStorage.getItem('kiwicart_basket')).toEqual(JSON.stringify([]))
  })
})
