import React from 'react'

interface PriceDisplayProps {
  price: number
  unitPrice?: string | null
  isCheapest?: boolean
  className?: string
  size?: 'sm' | 'md' | 'lg'
}

/**
 * A reusable component to display product prices consistently.
 * Handles the main price, unit price (e.g., /100g), and "cheapest" highlighting.
 */
const PriceDisplay: React.FC<PriceDisplayProps> = ({
  price,
  unitPrice,
  isCheapest = false,
  className = '',
  size = 'md',
}) => {
  const sizeClasses = {
    sm: {
      price: 'text-base sm:text-lg',
      unit: 'text-xs',
    },
    md: {
      price: 'text-xl sm:text-2xl',
      unit: 'text-xs sm:text-sm',
    },
    lg: {
      price: 'text-2xl sm:text-3xl lg:text-4xl',
      unit: 'text-sm sm:text-base',
    },
  }

  return (
    <div className={`flex flex-col ${className}`}>
      <div className="flex items-baseline gap-1">
        <span
          className={`font-black tracking-tighter ${sizeClasses[size].price} ${
            isCheapest ? 'text-emerald-600' : 'text-slate-900'
          }`}
        >
          ${price.toFixed(2)}
        </span>
        {isCheapest && (
          <span className="bg-emerald-100 text-emerald-800 text-xs font-black px-2 py-0.5 rounded-full uppercase tracking-wider ml-1 border border-emerald-200/50">
            Best
          </span>
        )}
      </div>
      {unitPrice && (
        <span className={`text-slate-500 font-medium ${sizeClasses[size].unit}`}>
          {unitPrice}
        </span>
      )}
    </div>
  )
}

export default PriceDisplay
