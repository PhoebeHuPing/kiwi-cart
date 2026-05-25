import React, { createContext, useContext, useState, ReactNode } from 'react'

export interface BasketItem {
  name: string
  image_url: string
  quantity: number
}

interface BasketContextType {
  basket: BasketItem[]
  addToBasket: (item: Omit<BasketItem, 'quantity'>) => void
  removeFromBasket: (name: string) => void
  updateQuantity: (name: string, quantity: number) => void
  clearBasket: () => void
  isInBasket: (name: string) => boolean
  isDrawerOpen: boolean
  setIsDrawerOpen: (open: boolean) => void
}

const BasketContext = createContext<BasketContextType | undefined>(undefined)

export function BasketProvider({ children }: { children: ReactNode }) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)
  const [basket, setBasket] = useState<BasketItem[]>(() => {
    const saved = localStorage.getItem('kiwicart_basket')
    return saved ? JSON.parse(saved) : []
  })

  React.useEffect(() => {
    localStorage.setItem('kiwicart_basket', JSON.stringify(basket))
  }, [basket])

  const addToBasket = (item: Omit<BasketItem, 'quantity'>) => {
    setBasket((prev) => {
      const existing = prev.find((i) => i.name === item.name)
      if (existing) {
        return prev.map((i) =>
          i.name === item.name ? { ...i, quantity: i.quantity + 1 } : i,
        )
      }
      return [...prev, { ...item, quantity: 1 }]
    })
    setIsDrawerOpen(true) // Auto-open drawer when adding item
  }

  const removeFromBasket = (name: string) => {
    setBasket((prev) => prev.filter((i) => i.name !== name))
  }

  const updateQuantity = (name: string, quantity: number) => {
    setBasket((prev) =>
      prev
        .map((i) => (i.name === name ? { ...i, quantity: Math.max(0, quantity) } : i))
        .filter((i) => i.quantity > 0),
    )
  }

  const clearBasket = () => {
    setBasket([])
  }

  const isInBasket = (name: string) => {
    return basket.some((i) => i.name === name)
  }

  return (
    <BasketContext.Provider
      value={{
        basket,
        addToBasket,
        removeFromBasket,
        updateQuantity,
        clearBasket,
        isInBasket,
        isDrawerOpen,
        setIsDrawerOpen,
      }}
    >
      {children}
    </BasketContext.Provider>
  )
}

export function useBasket() {
  const context = useContext(BasketContext)
  if (context === undefined) {
    throw new Error('useBasket must be used within a BasketProvider')
  }
  return context
}
