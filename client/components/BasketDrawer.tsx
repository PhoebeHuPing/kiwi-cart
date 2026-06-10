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
          <div className="w-screen max-w-xl">
            <div className="h-full flex flex-col bg-white shadow-2xl animate-in slide-in-from-right duration-300">
              {/* Header */}
              <div className="px-8 py-10 border-b border-gray-100 flex items-center justify-between bg-kiwi/5">
                <div>
                  <h2 className="text-3xl font-black text-kiwi-dark flex items-center gap-3">
                    <span aria-hidden="true">🛒</span> My Basket
                  </h2>
                  <p className="text-sm font-black text-kiwi-dark/80 uppercase tracking-[0.2em] mt-2">
                    {totalItems} Items to compare
                  </p>
                </div>
                <button
                  onClick={() => setIsDrawerOpen(false)}
                  className="w-12 h-12 flex items-center justify-center rounded-2xl hover:bg-white hover:shadow-sm transition-all text-3xl border-none bg-transparent cursor-pointer"
                  aria-label="Close basket"
                >
                  ✕
                </button>
              </div>

              {/* List */}
              <div className="flex-1 overflow-y-auto px-8 py-8">
                {basket.length === 0 ? (
                  <div className="h-full flex flex-col items-center justify-center text-center opacity-60">
                    <div className="text-8xl mb-6" aria-hidden="true">🧺</div>
                    <h3 className="text-2xl font-black text-kiwi-dark">
                      Basket is empty
                    </h3>
                    <p className="mt-4 text-lg max-w-[280px] text-gray-700 font-medium">
                      Add products from the list to start comparing prices.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-8">
                    {basket.map((item) => (
                      <div
                        key={item.name}
                        className="flex items-center gap-6 bg-gray-50/50 p-6 rounded-3xl border border-gray-100 group transition-all hover:bg-white hover:shadow-xl hover:border-kiwi/20"
                      >
                        <div className="w-24 h-24 bg-white rounded-2xl p-4 shadow-inner flex-shrink-0">
                          <img
                            src={item.image_url}
                            alt={item.name}
                            className="w-full h-full object-contain"
                          />
                        </div>
                        <div className="flex-1 min-w-0">
                          <h4 className="font-black text-lg text-kiwi-dark truncate">
                            {item.name}
                          </h4>
                          <div className="flex items-center gap-6 mt-4">
                            <div className="flex items-center bg-white border border-gray-200 rounded-xl overflow-hidden h-10 shadow-sm">
                              <button
                                onClick={() =>
                                  updateQuantity(item.name, item.quantity - 1)
                                }
                                className="px-4 hover:bg-gray-50 text-kiwi font-black border-none bg-transparent cursor-pointer text-xl"
                              >
                                −
                              </button>
                              <span className="w-10 text-center text-base font-black">
                                {item.quantity}
                              </span>
                              <button
                                onClick={() =>
                                  updateQuantity(item.name, item.quantity + 1)
                                }
                                className="px-4 hover:bg-gray-50 text-kiwi font-black border-none bg-transparent cursor-pointer text-xl"
                              >
                                +
                              </button>
                            </div>
                            <button
                              onClick={() => removeFromBasket(item.name)}
                              className="text-sm font-black text-red-600 hover:text-red-700 uppercase tracking-widest border-none bg-transparent cursor-pointer"
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
                <div className="p-8 border-t border-gray-100 bg-gray-50/80">
                  <button
                    onClick={handleCompare}
                    disabled={isComparing}
                    className="w-full py-5 bg-kiwi-dark text-white rounded-3xl font-black text-xl shadow-2xl shadow-kiwi-dark/30 hover:scale-[1.02] active:scale-[0.98] transition-all flex items-center justify-center gap-4 mb-4 border-none cursor-pointer disabled:bg-gray-700 disabled:opacity-90 disabled:cursor-not-allowed"
                  >
                    {isComparing ? (
                      <>
                        <div className="w-6 h-6 border-4 border-white border-t-transparent rounded-full animate-spin"></div>
                        <span>Calculating...</span>
                      </>
                    ) : (
                      <>
                        <span className="text-2xl" aria-hidden="true">⚡</span> Compare Live Prices
                      </>
                    )}
                  </button>

                  <button
                    onClick={clearBasket}
                    className="w-full py-3 text-sm font-black text-gray-400 hover:text-red-600 transition-colors uppercase tracking-[0.3em] border-none bg-transparent cursor-pointer"
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
