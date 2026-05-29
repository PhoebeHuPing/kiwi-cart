import { useQuery } from '@tanstack/react-query'
import { useAuth0 } from '@auth0/auth0-react'
import { getFavorites, getComparePrices } from '../apis/products'
import { Link } from 'react-router'

export default function MyKitchen() {
  const { getAccessTokenSilently, user } = useAuth0()

  // 1. Fetch favorite product names
  const { data: favoriteNames = [], isLoading: isLoadingFavs } = useQuery({
    queryKey: ['favorites'],
    queryFn: async () => {
      const token = await getAccessTokenSilently()
      return getFavorites(token)
    },
  })

  // 2. Fetch prices for ALL favorite products in parallel
  // Note: For a production app with many favorites, we might want to batch this,
  // but for a "staples" list, parallel queries work well for real-time comparison.
  const { data: staplePrices = [], isLoading: isLoadingPrices } = useQuery({
    queryKey: ['staplePrices', favoriteNames],
    queryFn: async () => {
      const pricePromises = favoriteNames.map((name) => getComparePrices(name))
      const results = await Promise.all(pricePromises)
      // Return a map of product_name -> best prices
      return favoriteNames.map((name, index) => ({
        name,
        prices: results[index],
      }))
    },
    enabled: favoriteNames.length > 0,
  })

  if (isLoadingFavs) {
    return (
      <div className="flex flex-col items-center justify-center py-20">
        <div className="w-12 h-12 border-4 border-kiwi border-t-transparent rounded-full animate-spin mb-4" />
        <p className="text-gray-500 font-bold uppercase tracking-widest text-sm">Entering your kitchen...</p>
      </div>
    )
  }

  return (
    <div className="max-w-6xl mx-auto py-8">
      <header className="mb-12">
        <div className="flex items-center gap-4 mb-4">
          <img src={user?.picture} alt="" className="w-16 h-16 rounded-2xl border-4 border-white shadow-lg" />
          <div>
            <h1 className="text-4xl font-black text-kiwi-dark tracking-tighter">My Kitchen</h1>
            <p className="text-gray-500 font-medium">Daily staples and monitored prices for {user?.nickname || user?.name}</p>
          </div>
        </div>
      </header>

      {favoriteNames.length === 0 ? (
        <div className="bg-white rounded-[2.5rem] p-16 text-center border-2 border-dashed border-gray-100">
          <span className="text-6xl mb-6 block" aria-hidden="true">🍳</span>
          <h2 className="text-2xl font-black text-kiwi-dark mb-4">Your kitchen is empty!</h2>
          <p className="text-gray-500 mb-8 max-w-md mx-auto">
            Search for your daily essentials like "Milk" or "Eggs" and click the heart icon to start monitoring prices here.
          </p>
          <Link to="/" className="inline-block bg-kiwi-dark text-white px-8 py-4 rounded-2xl font-black no-underline hover:scale-105 transition-all">
            Go Shopping
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-8">
          {isLoadingPrices ? (
             <div className="text-center py-10">
               <p className="text-sm font-black text-gray-400 uppercase tracking-widest animate-pulse">Checking today's prices...</p>
             </div>
          ) : (
            staplePrices.map((item) => {
              const cheapest = item.prices[0]
              return (
                <div key={item.name} className="bg-white rounded-3xl p-8 border border-gray-100 shadow-sm flex flex-col md:flex-row items-center gap-10">
                  <div className="w-32 h-32 bg-gray-50 rounded-2xl p-4 flex-shrink-0">
                    <img src={cheapest?.image_url || '/images/supermarket.avif'} alt="" className="w-full h-full object-contain mix-blend-multiply" />
                  </div>
                  
                  <div className="flex-1">
                    <h3 className="text-2xl font-black text-kiwi-dark tracking-tight">{item.name}</h3>
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mt-4">
                      {['Pak\'nSave', 'New World', 'Woolworths'].map(brand => {
                        const storePrice = item.prices.find(p => p.supermarket_name.includes(brand))
                        return (
                          <div key={brand} className={`p-4 rounded-2xl border ${storePrice ? 'bg-white border-gray-100' : 'bg-gray-50 border-transparent opacity-50'}`}>
                            <p className="text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1">{brand}</p>
                            {storePrice ? (
                              <div>
                                <span className={`text-xl font-black ${storePrice === cheapest ? 'text-price' : 'text-kiwi-dark'}`}>
                                  ${storePrice.price.toFixed(2)}
                                </span>
                                {storePrice === cheapest && (
                                  <span className="ml-2 text-[10px] bg-kiwi text-white px-1.5 py-0.5 rounded font-black uppercase">Best</span>
                                )}
                              </div>
                            ) : (
                              <span className="text-xs font-bold text-gray-400 italic">Not found</span>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  </div>

                  <Link 
                    to={`/?q=${item.name}`}
                    className="bg-gray-50 text-kiwi-dark px-6 py-4 rounded-xl font-black text-sm no-underline hover:bg-kiwi/10 hover:text-kiwi transition-all"
                  >
                    View Details
                  </Link>
                </div>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}
