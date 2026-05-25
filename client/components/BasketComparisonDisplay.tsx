import { BasketComparisonResult } from '../../models/products'

interface Props {
  results: BasketComparisonResult[]
  onClose: () => void
}

export default function BasketComparisonDisplay({ results, onClose }: Props) {
  // Sort results by total price, putting supermarkets with more items found first
  const sortedResults = [...results].sort((a, b) => {
    if (a.items_found !== b.items_found) {
      return b.items_found - a.items_found
    }
    return a.total_price - b.total_price
  })

  const bestChoice = sortedResults[0]

  return (
    <div className="fixed inset-0 z-[110] flex items-center justify-center p-4 sm:p-6">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-kiwi-dark/60 backdrop-blur-md animate-in fade-in duration-300" 
        onClick={onClose}
      />
      
      {/* Modal Content */}
      <div className="relative bg-white w-full max-w-2xl max-h-[90vh] rounded-3xl shadow-2xl overflow-hidden flex flex-col animate-in zoom-in slide-in-from-bottom-8 duration-300">
        {/* Header */}
        <div className="px-8 py-6 border-b border-gray-100 flex items-center justify-between bg-kiwi/5">
          <div>
            <h2 className="text-2xl font-black text-kiwi-dark">Price Comparison</h2>
            <p className="text-xs font-bold text-kiwi/60 uppercase tracking-widest mt-1">
              Found the best deals for your basket
            </p>
          </div>
          <button 
            onClick={onClose}
            className="w-10 h-10 flex items-center justify-center rounded-xl hover:bg-white hover:shadow-sm transition-all text-2xl border-none bg-transparent cursor-pointer"
          >
            ✕
          </button>
        </div>

        {/* Results List */}
        <div className="flex-1 overflow-y-auto p-8 space-y-6">
          {sortedResults.map((store, index) => (
            <div 
              key={store.supermarket_name}
              className={`relative border-2 rounded-2xl p-6 transition-all ${
                index === 0 
                  ? 'border-kiwi bg-kiwi/5 ring-4 ring-kiwi/10' 
                  : 'border-gray-100 bg-white hover:border-gray-200'
              }`}
            >
              {index === 0 && (
                <div className="absolute -top-3 left-6 bg-kiwi text-white text-[10px] font-black px-3 py-1 rounded-full uppercase tracking-widest">
                  Cheapest Total
                </div>
              )}

              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                  <div className="w-16 h-16 bg-white rounded-xl p-2 shadow-sm border border-gray-100 flex-shrink-0">
                    <img src={store.logo_url} alt={store.supermarket_name} className="w-full h-full object-contain" />
                  </div>
                  <div>
                    <h3 className="text-xl font-black text-kiwi-dark">{store.supermarket_name}</h3>
                    <p className="text-sm font-bold text-gray-400">
                      {store.items_found} items found 
                      {store.missing_items.length > 0 && ` • ${store.missing_items.length} missing`}
                    </p>
                  </div>
                </div>

                <div className="text-left sm:text-right">
                  <div className="text-3xl font-black text-price">
                    ${store.total_price.toFixed(2)}
                  </div>
                  <p className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Total Estimated Cost</p>
                </div>
              </div>

              {/* Missing Items Alert */}
              {store.missing_items.length > 0 && (
                <div className="mt-4 bg-red-50 rounded-xl p-3 border border-red-100">
                  <p className="text-[10px] font-black text-red-500 uppercase tracking-widest flex items-center gap-2">
                    <span>⚠️</span> Missing from this store:
                  </p>
                  <p className="text-xs text-red-400 mt-1 font-medium">
                    {store.missing_items.join(', ')}
                  </p>
                </div>
              )}

              {/* Details Toggler (Optional placeholder for future expansion) */}
              <div className="mt-4 pt-4 border-t border-gray-100/50 flex justify-between items-center">
                <span className="text-[10px] font-bold text-gray-400 uppercase">View itemized breakdown</span>
                <span className="text-kiwi">→</span>
              </div>
            </div>
          ))}
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
