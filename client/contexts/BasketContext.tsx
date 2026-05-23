import React, { createContext, useContext, useState, ReactNode } from 'react'

export interface BasketItem {
  name: string
  image_url: string
}

interface BasketContextType {
  basket: BasketItem[]
  addToBasket: (item: BasketItem) => void
  removeFromBasket: (name: string) => void
  clearBasket: () => void
  isInBasket: (name: string) => boolean
}

const BasketContext = createContext<BasketContextType | undefined>(undefined)

export function BasketProvider({ children }: { children: ReactNode }) {
  const [basket, setBasket] = useState<BasketItem[]>([])

  const addToBasket = (item: BasketItem) => {
    setBasket((prev) => {
      if (prev.find((i) => i.name === item.name)) return prev
      return [...prev, item]
    })
  }

  const removeFromBasket = (name: string) => {
    setBasket((prev) => prev.filter((i) => i.name !== name))
  }

  const clearBasket = () => {
    setBasket([])
  }

  const isInBasket = (name: string) => {
    return basket.some((i) => i.name === name)
  }

  return (
    <BasketContext.Provider
      value={{ basket, addToBasket, removeFromBasket, clearBasket, isInBasket }}
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
