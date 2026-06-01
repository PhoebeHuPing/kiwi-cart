import React, { useState } from 'react'
import { BasketComparisonResult } from '../../models/products'
import PriceDisplay from './ui/PriceDisplay'

interface Props {

  results: BasketComparisonResult[]
  onClose: () => void
}

export default function BasketComparisonDisplay({ results, onClose }: Props) {
  const [expandedStore, setExpandedStore] = useState<string | null>(null)

  // Sort results by the number of items found (primary) and then by total price (secondary).
  // This ensures that stores with the most matching items are shown first.
  const sortedResults = [...results].sort((a, b) => {
    if (a.items_found !== b.items_found) {
      return b.items_found - a.items_found
    }
    return a.total_price - b.total_price
  })

  // Calculate potential savings by comparing the cheapest and most expensive store results.
  const cheapest = sortedResults[0]
  const expensive = sortedResults[sortedResults.length - 1]
  const potentialSavings = expensive.total_price - cheapest.total_price

  return (
    <div className="fixed inset-0 z-[110] flex items-center justify-center p-4 sm:p-6">
      {/* Backdrop */}
      <div
        role="button"
        tabIndex={-1}
        className="absolute inset-0 bg-kiwi-dark/60 backdrop-blur-md animate-in fade-in duration-300"
        onClick={onClose}
        onKeyDown={(e) => e.key === 'Escape' && onClose()}
      />

      {/* Modal Content */}
      <div className="relative bg-white w-full max-w-4xl max-h-[90vh] rounded-[3rem] shadow-2xl overflow-hidden flex flex-col animate-in zoom-in slide-in-from-bottom-8 duration-300">
        {/* Header */}
        <div className="px-10 py-8 border-b border-gray-100 flex items-center justify-between bg-kiwi/5">
          <div>
            <h2 className="text-3xl font-black text-kiwi-dark">
              Price Comparison
            </h2>
            <p className="text-sm font-black text-kiwi-dark/80 uppercase tracking-[0.2em] mt-2">
              Live results for your current basket
            </p>
          </div>
          <button
            onClick={onClose}
            className="w-12 h-12 flex items-center justify-center rounded-2xl hover:bg-white hover:shadow-sm transition-all text-3xl border-none bg-transparent cursor-pointer"
          >
            ✕
          </button>
        </div>

        {/* AI Insight / Savings Banner */}
        {potentialSavings > 0 && (
          <div className="bg-kiwi px-10 py-6 flex items-center justify-between">
            <div className="flex items-center gap-4">
              <span className="text-3xl" aria-hidden="true">💡</span>
              <p className="text-lg font-bold text-white leading-tight">
                You can save up to{' '}
                <span className="text-2xl font-black underline underline-offset-8 decoration-white/50">
                  ${potentialSavings.toFixed(2)}
                </span>{' '}
                 by shopping at {cheapest.supermarket_name}!
              </p>
            </div>
            <span className="text-sm bg-white/20 px-3 py-1.5 rounded-lg font-black text-white uppercase tracking-widest">
              Kiwi Insight
            </span>
          </div>
        )}

        {/* Results List */}
        <div className="flex-1 overflow-y-auto p-10 space-y-10">
          {sortedResults.map((store, index) => {
            const isExpanded = expandedStore === store.supermarket_name

            return (
              <div
                key={store.supermarket_name}
                className={`relative border-2 rounded-[2rem] transition-all ${
                  index === 0
                    ? 'border-kiwi bg-kiwi/5 ring-8 ring-kiwi/5'
                    : 'border-gray-100 bg-white hover:border-gray-200'
                }`}
              >
                {index === 0 && (
                  <div className="absolute -top-4 left-10 bg-kiwi text-white text-xs font-black px-4 py-1.5 rounded-full uppercase tracking-[0.2em] shadow-lg shadow-kiwi/20">
                    Best Value Choice
                  </div>
                )}

                <div className="p-8">
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-8">
                    <div className="flex items-center gap-6">
                      <div className="w-24 h-24 bg-white rounded-2xl p-4 shadow-sm border border-gray-100 flex-shrink-0">
                        <img
                          src={store.logo_url}
                          alt=""
                          className="w-full h-full object-contain"
                        />
                      </div>
                      <div>
                        <h3 className="text-3xl font-black text-kiwi-dark tracking-tight">
                          {store.supermarket_name}
                        </h3>
                        <p className="text-lg font-bold text-gray-500 mt-1">
                          {store.items_found} items found
                          {store.missing_items.length > 0 &&
                            ` • ${store.missing_items.length} missing`}
                        </p>
                      </div>
                    </div>

                    <div className="text-left md:text-right">
                      <PriceDisplay 
                        price={store.total_price}
                        size="lg"
                        isCheapest={isCheapest}
                      />
                      <p className="text-sm font-black text-gray-400 uppercase tracking-widest mt-1">
                        Total Estimated Cost
                      </p>
                    </div>
                  </div>

                  {/* Missing Items Alert */}
                  {store.missing_items.length > 0 && (
                    <div className="mt-4 bg-red-50 rounded-xl p-3 border border-red-100">
                      <p className="text-xs font-black text-red-600 uppercase tracking-widest flex items-center gap-2">
                        <span aria-hidden="true">⚠️</span> Missing items:
                      </p>
                      <p className="text-xs text-red-600 mt-1 font-medium">
                        {store.missing_items.join(', ')}
                      </p>
                    </div>
                  )}

                  {/* Details Toggler */}
                  <button
                    onClick={() =>
                      setExpandedStore(isExpanded ? null : store.supermarket_name)
                    }
                    className="w-full mt-4 pt-4 border-t border-gray-100/50 flex justify-between items-center bg-transparent border-none cursor-pointer group"
                  >
                    <span className="text-xs font-bold text-gray-600 uppercase tracking-[0.1em] group-hover:text-kiwi transition-colors">
                      {isExpanded ? 'Hide' : 'View'} itemized breakdown
                    </span>
                    <span
                      className={`text-kiwi transition-transform duration-300 ${
                        isExpanded ? 'rotate-180' : ''
                      }`}
                      aria-hidden="true"
                    >
                      ▼
                    </span>
                  </button>

                  {/* Expanded Item List */}
                  {isExpanded && (
                    <div className="mt-4 space-y-2 animate-in slide-in-from-top-2 duration-300">
                      <div className="grid grid-cols-4 px-2 text-xs font-black text-gray-600 uppercase tracking-widest pb-1">
                        <div className="col-span-2">Product</div>
                        <div className="text-center">Qty</div>
                        <div className="text-right">Price</div>
                      </div>
                      {store.details.map((item, i) => (
                        <div
                          key={i}
                          className="grid grid-cols-4 px-2 py-2 bg-white/50 rounded-lg text-sm"
                        >
                          <div className="col-span-2 font-bold text-kiwi-dark truncate">
                            {item.name}
                          </div>
                          <div className="text-center text-gray-600 font-medium">
                            ×{item.quantity}
                          </div>
                          <div className="text-right font-black text-kiwi-dark">
                            ${item.subtotal.toFixed(2)}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>

        {/* Footer Action */}
        <div className="p-8 bg-gray-50 border-t border-gray-100">
          <button
            onClick={onClose}
            className="w-full py-4 bg-kiwi-dark text-white rounded-2xl font-black text-lg shadow-xl hover:scale-[1.02] active:scale-[0.98] transition-all border-none cursor-pointer"
          >
            Got it, thanks!
          </button>
        </div>
      </div>
    </div>
  )
}
