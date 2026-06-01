/**
 * Extracts quantity and unit from product names and calculates unit price.
 * Handles patterns like "1kg", "500g", "2L", "750ml", "12 x 330ml".
 */
export function calculateUnitPrice(name: string, price: number): string | null {
  if (!price || price <= 0) return null

  const normalized = name.toLowerCase()

  // Case 1: Multi-packs (e.g., "12 x 330ml")
  const multipackMatch = normalized.match(/(\d+)\s*x\s*(\d+(?:\.\d+)?)\s*(ml|l|g|kg)/)
  if (multipackMatch) {
    const packCount = parseInt(multipackMatch[1])
    const qty = parseFloat(multipackMatch[2])
    const unit = multipackMatch[3]
    const totalQty = packCount * qty
    return formatPriceByUnit(totalQty, unit, price)
  }

  // Case 2: Standard single units (e.g., "500g", "2L", "1.5kg")
  const singleMatch = normalized.match(/(\d+(?:\.\d+)?)\s*(ml|l|g|kg)/)
  if (singleMatch) {
    const qty = parseFloat(singleMatch[1])
    const unit = singleMatch[2]
    return formatPriceByUnit(qty, unit, price)
  }

  return null
}

/**
 * Formats the price based on quantity and unit (e.g., /100g, /kg, /100ml, /L).
 * Standardizes units for consistent price comparison.
 */
export function formatPriceByUnit(
  qty: number,
  unit: string,
  totalPrice: number,
): string | null {
  if (qty <= 0) return null

  switch (unit) {
    case 'g': {
      // Standardize to price per 100g
      const pricePer100g = (totalPrice / qty) * 100
      return `$${pricePer100g.toFixed(2)}/100g`
    }
    case 'kg': {
      // Standardize to price per kg
      const pricePerKg = totalPrice / qty
      return `$${pricePerKg.toFixed(2)}/kg`
    }
    case 'ml': {
      // Standardize to price per 100ml
      const pricePer100ml = (totalPrice / qty) * 100
      return `$${pricePer100ml.toFixed(2)}/100ml`
    }
    case 'l': {
      // Standardize to price per L
      const pricePerL = totalPrice / qty
      return `$${pricePerL.toFixed(2)}/L`
    }
    default:
      return null
  }
}
