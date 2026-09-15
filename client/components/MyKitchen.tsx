import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuth0 } from '@auth0/auth0-react'
import toast from 'react-hot-toast'
import {
  getFavoritesWithGtin,
  getComparePricesByGtin,
  getComparePrices,
  toggleFavorite,
} from '../apis/products'
import { Link } from 'react-router'
import SuggestionCards from './SuggestionCards'
import { stripLeadingBrand } from '../utils/productName'

function toTitleCase(input: string): string {
  return input.replace(/\S+/g, (word) =>
    /[A-Z]/.test(word) ? word : word.charAt(0).toUpperCase() + word.slice(1),
  )
}

function getStoreBrand(supermarketName: string): string | null {
  const patterns = [
    { regex: /^Pak'nSave/, brand: 'PakNSave' },
    { regex: /^New World/, brand: 'NewWorld' },
    { regex: /^Woolworths/, brand: 'Woolworths' },
  ]

  for (const { regex, brand } of patterns) {
    if (regex.test(supermarketName)) {
      return brand
    }
  }

  return null
}

export default function MyKitchen() {
  const { getAccessTokenSilently, user } = useAuth0()
  const queryClient = useQueryClient()

  // Remove a product from favorites (toggleFavorite removes if already favorited)
  const removeMutation = useMutation({
    mutationFn: async (name: string) => {
      const token = await getAccessTokenSilently()
      return toggleFavorite(name, token)
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['favoritesWithGtin'] })
      queryClient.invalidateQueries({ queryKey: ['favorites'] })
      toast(`🗑️ Removed "${data.name}" from your kitchen`, {
        duration: 1500,
        style: {
          borderRadius: '24px',
          background: '#333',
          color: '#fff',
          padding: '16px 24px',
          fontSize: '16px',
          fontWeight: 'bold',
          maxWidth: 'min(90vw, 600px)',
          textAlign: 'center',
          boxShadow: '0 25px 50px -12px rgb(0 0 0 / 0.5)',
        },
      })
    },
  })

  // 1. Fetch favorite products with their GTINs
  const { data: favoritesWithGtin = [], isLoading: isLoadingFavs } = useQuery({
    queryKey: ['favoritesWithGtin'],
    queryFn: async () => {
      const token = await getAccessTokenSilently()
      return getFavoritesWithGtin(token)
    },
  })

  // 2. Fetch prices for ALL favorite products in parallel
  // When GTIN is available, use GTIN-based comparison for exact product matching.
  // Fall back to name-based search if GTIN is not available.
  const { data: staplePrices = [], isLoading: isLoadingPrices } = useQuery({
    queryKey: [
      'staplePrices',
      favoritesWithGtin.map((f) => `${f.name}|${f.gtin ?? ''}`).join(','),
    ],
    queryFn: async () => {
      const pricePromises = favoritesWithGtin.map((fav) =>
        fav.gtin ? getComparePricesByGtin(fav.gtin) : getComparePrices(fav.name),
      )
      const results = await Promise.all(pricePromises)
      // Return a map of product_name -> best prices
      return favoritesWithGtin.map((fav, index) => ({
        name: fav.name,
        prices: results[index],
      }))
    },
    enabled: favoritesWithGtin.length > 0,
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
      <header className="mb-8 sm:mb-12">
        <div className="flex items-center gap-4 mb-4">
          <img src={user?.picture} alt={user?.name || 'User Profile'} className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl border-4 border-white shadow-lg" />
          <div>
            <h1 className="text-2xl sm:text-4xl font-black text-kiwi-dark tracking-tighter">My Kitchen</h1>
            <p className="text-sm sm:text-base text-gray-600 font-medium">Daily staples and monitored prices for {user?.nickname || user?.name}</p>
          </div>
        </div>
      </header>

      <div className="mb-8">
        <SuggestionCards />
      </div>

      {favoritesWithGtin.length === 0 ? (
        <div className="bg-white rounded-[2rem] sm:rounded-[2.5rem] p-8 sm:p-16 text-center border-2 border-dashed border-gray-100">
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
              const cheapest = item.prices && item.prices.length > 0 ? item.prices[0] : null
              
              // Skip if no prices found
              if (!cheapest) {
                return (
                  <div key={item.name} className="bg-white rounded-3xl p-5 sm:p-8 border border-gray-100 shadow-sm">
                    <p className="text-center text-gray-500 italic">No prices found for {item.name}</p>
                  </div>
                )
              }
              
              return (
                <div key={item.name} className="bg-white rounded-3xl p-5 sm:p-8 border border-gray-100 shadow-sm flex flex-col md:flex-row items-center gap-6 sm:gap-10">
                  <div className="w-24 h-24 sm:w-32 sm:h-32 bg-gray-50 rounded-2xl p-4 flex-shrink-0">
                    <img src={cheapest.image_url || '/images/supermarket.avif'} alt="" className="w-full h-full object-contain mix-blend-multiply" />
                  </div>
                  
                  <div className="flex-1 w-full">
                    <h3 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight mb-2 min-h-[5.25rem] sm:min-h-[6rem]">
                      <span className="block min-h-[1.75rem] text-sm uppercase tracking-widest text-kiwi">
                        {cheapest.brand
                          ? toTitleCase(cheapest.brand)
                          : '\u00a0'}
                      </span>
                      <span className="block line-clamp-2">
                        {toTitleCase(
                          stripLeadingBrand(
                            item.name,
                            cheapest.brand,
                          ),
                        )}
                      </span>
                    </h3>
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mt-4">
                      {['PakNSave', 'NewWorld', 'Woolworths'].map(brand => {
                        const displayBrand = brand === 'PakNSave' ? "Pak'nSave" : brand === 'NewWorld' ? 'New World' : 'Woolworths'
                        
                        // Simple direct matching by brand
                        const storePrice = item.prices.find(p => {
                          const storeBrand = getStoreBrand(p.supermarket_name)
                          return storeBrand === brand
                        })
                        
                        return (
                          <div key={brand} className={`p-4 rounded-2xl border ${storePrice ? 'bg-white border-gray-100' : 'bg-gray-50 border-transparent'}`}>
                            <p className="text-xs font-black text-gray-700 uppercase tracking-widest mb-2">{displayBrand}</p>
                            {storePrice ? (
                              <div>
                                <span className={`text-xl font-black ${storePrice.price === cheapest.price ? 'text-price' : 'text-kiwi-dark'}`}>
                                  ${storePrice.price.toFixed(2)}
                                </span>
                                {storePrice.price === cheapest.price && (
                                  <span className="ml-2 text-xs bg-kiwi-dark text-white px-2 py-0.5 rounded-lg font-black uppercase">Best</span>
                                )}
                              </div>
                            ) : (
                              <span className="text-sm font-bold text-gray-400 italic">—</span>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  </div>

                  <button
                    onClick={() => removeMutation.mutate(item.name)}
                    disabled={removeMutation.isPending}
                    className="w-full md:w-auto text-center bg-red-50 text-red-600 px-6 py-4 rounded-xl font-black text-sm border-none cursor-pointer hover:bg-red-100 hover:text-red-700 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Remove
                  </button>
                </div>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}
