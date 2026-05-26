import { useState } from 'react'
import { useBasket } from '../contexts/BasketContext'
import { compareBasket } from '../apis/products'
import { BasketComparisonResult } from '../../models/products'
import BasketComparisonDisplay from './BasketComparisonDisplay'

export default function BasketDrawer() {
  const {
    basket,
    removeFromBasket,
    updateQuantity,
    isDrawerOpen,
    setIsDrawerOpen,
    clearBasket,
  } = useBasket()

  const [isComparing, setIsComparing] = useState(false)
  const [comparisonResults, setComparisonResults] = useState<
    BasketComparisonResult[] | null
  >(null)

  if (!isDrawerOpen) return null

  const totalItems = basket.reduce((sum, item) => sum + item.quantity, 0)

  const handleCompare = async () => {
    try {
      setIsComparing(true)
      const results = await compareBasket(
        basket.map((i) => ({ name: i.name, quantity: i.quantity })),
      )
      setComparisonResults(results)
    } catch (err) {
      console.error('Comparison failed:', err)
      alert('Failed to compare prices. Please try again later.')
    } finally {
      setIsComparing(false)
    }
  }

  return (
    <>
      <div className="fixed inset-0 z-[100] overflow-hidden">
        {/* Overlay */}
        <div
          role="button"
          tabIndex={-1}
          className="absolute inset-0 bg-kiwi-dark/40 backdrop-blur-sm transition-opacity"
          onClick={() => setIsDrawerOpen(false)}
          onKeyDown={(e) => e.key === 'Escape' && setIsDrawerOpen(false)}
        />

        <div className="absolute inset-y-0 right-0 max-w-full flex">
          <div className="w-screen max-w-md">
            <div className="h-full flex flex-col bg-white shadow-2xl animate-in slide-in-from-right duration-300">
              {/* Header */}
              <div className="px-6 py-8 border-b border-gray-100 flex items-center justify-between bg-kiwi/5">
                <div>
                  <h2 className="text-2xl font-black text-kiwi-dark flex items-center gap-2">
                    <span aria-hidden="true">🛒</span> My Basket
                  </h2>
                  <p className="text-xs font-bold text-kiwi-dark/80 uppercase tracking-widest mt-1">
                    {totalItems} Items to compare
                  </p>
                </div>
                <button
                  onClick={() => setIsDrawerOpen(false)}
                  className="w-10 h-10 flex items-center justify-center rounded-xl hover:bg-white hover:shadow-sm transition-all text-2xl border-none bg-transparent cursor-pointer"
                  aria-label="Close basket"
                >
                  ✕
                </button>
              </div>

              {/* List */}
              <div className="flex-1 overflow-y-auto px-6 py-6">
                {basket.length === 0 ? (
                  <div className="h-full flex flex-col items-center justify-center text-center opacity-60">
                    <div className="text-6xl mb-4" aria-hidden="true">🧺</div>
                    <h3 className="text-xl font-bold text-kiwi-dark">
                      Basket is empty
                    </h3>
                    <p className="mt-2 text-sm max-w-[200px] text-gray-700">
                      Add products from the list to start comparing prices.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {basket.map((item) => (
                      <div
                        key={item.name}
                        className="flex items-center gap-4 bg-gray-50/50 p-4 rounded-2xl border border-gray-100 group transition-all hover:bg-white hover:shadow-md hover:border-kiwi/20"
                      >
                        <div className="w-16 h-16 bg-white rounded-xl p-2 shadow-inner flex-shrink-0">
                          <img
                            src={item.image_url}
                            alt={item.name}
                            className="w-full h-full object-contain"
                          />
                        </div>
                        <div className="flex-1 min-w-0">
                          <h4 className="font-bold text-sm text-kiwi-dark truncate">
                            {item.name}
                          </h4>
                          <div className="flex items-center gap-3 mt-2">
                            <div className="flex items-center bg-white border border-gray-200 rounded-lg overflow-hidden h-8">
                              <button
                                onClick={() =>
                                  updateQuantity(item.name, item.quantity - 1)
                                }
                                className="px-2 hover:bg-gray-50 text-kiwi font-bold border-none bg-transparent cursor-pointer"
                              >
                                −
                              </button>
                              <span className="w-8 text-center text-xs font-black">
                                {item.quantity}
                              </span>
                              <button
                                onClick={() =>
                                  updateQuantity(item.name, item.quantity + 1)
                                }
                                className="px-2 hover:bg-gray-50 text-kiwi font-bold border-none bg-transparent cursor-pointer"
                              >
                                +
                              </button>
                            </div>
                            <button
                              onClick={() => removeFromBasket(item.name)}
                              className="text-xs font-bold text-red-600 hover:text-red-700 uppercase tracking-tighter border-none bg-transparent cursor-pointer"
                            >
                              Remove
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Footer */}
              {basket.length > 0 && (
                <div className="p-6 border-t border-gray-100 bg-gray-50/80">
                  <button
                    onClick={handleCompare}
                    disabled={isComparing}
                    className="w-full py-4 bg-kiwi-dark text-white rounded-2xl font-black text-lg shadow-xl shadow-kiwi-dark/20 hover:scale-[1.02] active:scale-[0.98] transition-all flex items-center justify-center gap-3 mb-3 border-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isComparing ? (
                      <>
                        <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                        <span>Calculating...</span>
                      </>
                    ) : (
                      <>
                        <span>⚡</span> Compare Live Prices
                      </>
                    )}
                  </button>
                  <button
                    onClick={clearBasket}
                    className="w-full py-2 text-xs font-bold text-gray-600 hover:text-red-600 transition-colors uppercase tracking-[0.2em] border-none bg-transparent cursor-pointer"
                  >
                    Clear All Items
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Comparison Results Modal */}
      {comparisonResults && (
        <BasketComparisonDisplay
          results={comparisonResults}
          onClose={() => setComparisonResults(null)}
        />
      )}
    </>
  )
}
