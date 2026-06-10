import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useDebounce } from 'use-debounce'
import { useAuth0 } from '@auth0/auth0-react'
import toast from 'react-hot-toast'
import { getComparePrices, getFavorites, toggleFavorite } from '../apis/products'
import StoreMap from './StoreMap'
import PriceDisplay from './ui/PriceDisplay'
import { PriceComparisonData } from '../../models/products'
import { useBasket } from '../contexts/BasketContext'

interface GroupedProduct {
  product_name: string
  image_url: string
  options: PriceComparisonData[]
}

function ProductComparison() {
  const { getAccessTokenSilently, isAuthenticated, loginWithRedirect } = useAuth0()
  const queryClient = useQueryClient()
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm] = useDebounce(searchTerm, 500)
  const [showDropdown, setShowDropdown] = useState(false)
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null)
  const { basket, addToBasket, isInBasket, removeFromBasket, setIsDrawerOpen } =
    useBasket()

  const {
    data: products,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ['compare', debouncedSearchTerm],
    queryFn: () => getComparePrices(debouncedSearchTerm),
  })

  // Fetch favorites only if authenticated
  const { data: favorites = [] } = useQuery({
    queryKey: ['favorites'],
    queryFn: async () => {
      const token = await getAccessTokenSilently()
      return getFavorites(token)
    },
    enabled: isAuthenticated,
  })

  const favoriteMutation = useMutation({
    mutationFn: async (name: string) => {
      const token = await getAccessTokenSilently()
      return toggleFavorite(name, token)
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['favorites'] })
      toast(
        data.action === 'added' 
          ? `✅ Added "${data.name}" to favorites!` 
          : `🗑️ Removed "${data.name}" from favorites!`,
        {
          duration: 3000,
          style: {
            borderRadius: '24px',
            background: '#333',
            color: '#fff',
            padding: '24px 48px',
            fontSize: '20px',
            fontWeight: 'bold',
            minWidth: '600px',
            textAlign: 'center',
            boxShadow: '0 25px 50px -12px rgb(0 0 0 / 0.5)',
          },
        }
      )
    },
  })

  const isFavorite = (name: string) => favorites.includes(name)

  const handleFavoriteClick = (e: React.MouseEvent, productName: string) => {
    e.stopPropagation()
    if (!isAuthenticated) {
      toast((t) => (
        <span className="flex items-center justify-between w-full font-bold text-kiwi-dark text-xl">
          <span>Please sign in to save favorites!</span>
          <button
            onClick={() => {
              toast.dismiss(t.id)
              loginWithRedirect()
            }}
            className="bg-kiwi text-white px-8 py-3 rounded-2xl text-base font-black uppercase tracking-wider border-none cursor-pointer hover:bg-kiwi-dark hover:scale-105 transition-all shadow-lg"
          >
            Sign In Now
          </button>
        </span>
      ), {
        duration: 6000,
        style: {
          borderRadius: '32px',
          background: '#fff',
          color: '#333',
          border: '4px solid #f1f5f9',
          padding: '32px 60px',
          minWidth: '800px',
          boxShadow: '0 35px 60px -15px rgb(0 0 0 / 0.3)',
        },
      })
      return
    }
    favoriteMutation.mutate(productName)
  }

  // Group the flat array of products by their name and ensure one lowest price per supermarket.
  // This prevents multiple results for the same product at different locations of the same brand.
  const groupedProducts = products?.reduce((acc: GroupedProduct[], current) => {
    const existingProduct = acc.find(
      (p) => p.product_name === current.product_name,
    )

    if (existingProduct) {
      const existingOptionIndex = existingProduct.options.findIndex(
        (opt) => opt.supermarket_name === current.supermarket_name,
      )

      if (existingOptionIndex !== -1) {
        // If this supermarket already has a price for this product, keep the cheapest one
        if (current.price < existingProduct.options[existingOptionIndex].price) {
          existingProduct.options[existingOptionIndex] = current
        }
      } else {
        existingProduct.options.push(current)
      }
      // Ensure options are always sorted by price within the group
      existingProduct.options.sort((a, b) => a.price - b.price)
    } else {
      acc.push({
        product_name: current.product_name,
        image_url: current.image_url,
        options: [current],
      })
    }
    return acc
  }, [])

  const trendingCategories = [
    { name: 'Milk', icon: '🥛' },
    { name: 'Bread', icon: '🍞' },
    { name: 'Eggs', icon: '🥚' },
    { name: 'Butter', icon: '🧈' },
    { name: 'Apples', icon: '🍎' },
  ]

  return (
    <div className="min-h-screen bg-background pb-12">
      <div className="py-8">
        {/* Search and Navigation Header (Sticky) */}
        <div className="sticky top-0 z-40 -mx-4 px-4 py-4 mb-12 bg-background/95 backdrop-blur-md border-b border-transparent transition-all data-[stuck]:border-gray-100">
          <div className="flex flex-col gap-6">
            <div className="flex items-center gap-4 bg-white p-6 rounded-3xl shadow-sm border border-gray-100 focus-within:ring-4 focus-within:ring-kiwi/10 transition-all relative">
              <span className="text-3xl ml-2" aria-hidden="true">
                🔍
              </span>
              <input
                type="text"
                aria-label="Search for products"
                className="flex-1 bg-transparent border-none focus:ring-0 text-xl font-medium outline-none placeholder:text-gray-600"
                placeholder="Search for a product (e.g. Milk, Bread, Steak)..."
                value={searchTerm}
                onFocus={() => setShowDropdown(true)}
                onChange={(e) => {
                  setSearchTerm(e.target.value)
                  setShowDropdown(true)
                }}
              />

              {isLoading && (
                <div className="w-6 h-6 border-2 border-kiwi border-t-transparent rounded-full animate-spin"></div>
              )}

              {/* Quick Search Dropdown */}
              {showDropdown && searchTerm.length > 0 && (
                <div className="absolute top-full left-0 right-0 mt-2 bg-white rounded-2xl shadow-xl border border-gray-100 z-50 overflow-hidden">
                  {isLoading ? (
                    <div className="p-8 text-center">
                      <div className="w-8 h-8 border-4 border-kiwi border-t-transparent rounded-full animate-spin mx-auto mb-2"></div>
                      <p className="text-sm text-gray-700 font-medium">
                        Comparing prices from supermarkets...
                      </p>
                    </div>
                  ) : products?.length === 0 ? (
                    <div className="p-8 text-center text-gray-700 text-sm">
                      No products found for {searchTerm}
                    </div>
                  ) : (
                    <div className="divide-y divide-gray-50">
                      {products?.slice(0, 5).map((item, index) => (
                        <button
                          key={index}
                          type="button"
                          className="w-full text-left p-4 hover:bg-gray-50 flex items-center justify-between cursor-pointer transition-colors border-none bg-transparent"
                          onClick={() => {
                            setSearchTerm(item.product_name)
                            setShowDropdown(false)
                          }}
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-12 h-12 bg-gray-50 rounded-lg p-1 flex-shrink-0">
                              <img
                                src={item.image_url}
                                alt=""
                                className="w-full h-full object-contain"
                              />
                            </div>
                            <div>
                              <h4 className="font-bold text-sm text-kiwi-dark line-clamp-1">
                                {item.product_name}
                              </h4>
                              <div className="flex items-center gap-1.5 mt-0.5">
                                <img
                                  src={item.logo_url}
                                  alt=""
                                  className="w-3 h-3 object-contain"
                                />
                                <span className="text-xs text-gray-600 font-bold uppercase tracking-tight">
                                  {item.supermarket_name}
                                </span>
                              </div>
                            </div>
                          </div>

                          <div className="text-right flex-shrink-0">
                            <p className="font-black text-price text-lg">
                              ${item.price.toFixed(2)}
                            </p>
                            {item.unit_price && (
                              <p className="text-[10px] text-gray-400 font-black uppercase tracking-tighter -mt-1">
                                {item.unit_price}
                              </p>
                            )}
                            {index === 0 && (
                              <span className="text-[10px] bg-kiwi text-white px-1.5 py-0.5 rounded font-bold uppercase">
                                Cheapest
                              </span>
                            )}
                          </div>
                        </button>
                      ))}

                      <div className="p-2 bg-gray-50 text-center">
                        <button
                          className="text-xs font-black text-kiwi tracking-widest uppercase hover:underline"
                          onClick={() => setShowDropdown(false)}
                        >
                          View All Results
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Trending Categories Quick Tags - Optimized for Mobile */}
            <div className="flex flex-nowrap md:flex-wrap gap-2 md:gap-4 overflow-x-auto pb-2 md:pb-0 scrollbar-hide -mx-2 px-2 md:mx-0 md:px-0">
              {trendingCategories.map((cat) => (
                <button
                  key={cat.name}
                  onClick={() => setSearchTerm(cat.name)}
                  className="px-4 py-2 md:px-6 md:py-3 bg-white rounded-xl md:rounded-2xl text-sm md:text-base font-bold text-gray-600 border border-gray-100 hover:border-kiwi hover:text-kiwi transition-all shadow-sm flex items-center gap-2 hover:scale-105 whitespace-nowrap flex-shrink-0"
                >
                  <span className="text-lg md:text-xl">{cat.icon}</span>
                  {cat.name}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Overlay to close the dropdown when clicking outside */}
        {showDropdown && (
          <div
            role="presentation"
            aria-hidden="true"
            className="fixed inset-0 z-40 cursor-default"
            onClick={() => setShowDropdown(false)}
          ></div>
        )}

        <div className="flex flex-col xl:flex-row gap-12 items-start">
          {/* Main Comparison List - Now in a flexible container to maximize width */}
          <div className="flex-1 space-y-8 w-full">
            <h2 className="text-3xl font-black text-kiwi-dark mb-6 flex items-center gap-3">
              {debouncedSearchTerm ? (
                <>
                  <span className="text-2xl" aria-hidden="true">
                    🔎
                  </span>{' '}
                  Results for &quot;
                  {debouncedSearchTerm}&quot;
                </>
              ) : (
                <>
                  <span className="text-2xl" aria-hidden="true">
                    🔥
                  </span>{' '}
                  Daily Essentials
                </>
              )}
            </h2>

            {isError && (
              <div className="bg-red-50 text-red-600 p-4 rounded-xl border border-red-100">
                <span aria-hidden="true">⚠️</span> Error: {error.message}
              </div>
            )}

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {groupedProducts?.map(
                (group: GroupedProduct, groupIdx: number) => {
                  const isExpanded = expandedIndex === groupIdx
                  const bestOption = group.options[0]

                  return (
                    <div
                      key={groupIdx}
                      className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden flex flex-col hover:shadow-xl transition-all duration-300 group"
                    >
                      {/* Flowbite-style Image Header */}
                      <div className="relative aspect-square bg-gray-50/50 p-8 flex items-center justify-center overflow-hidden">
                        <img
                          src={group.image_url}
                          alt={group.product_name}
                          className="w-full h-full object-contain mix-blend-multiply group-hover:scale-110 transition-transform duration-500"
                        />
                        <div className="absolute top-4 left-4 flex flex-col gap-2">
                          <button
                            onClick={(e) => handleFavoriteClick(e, group.product_name)}
                            className={`w-10 h-10 flex items-center justify-center rounded-xl transition-all border-none cursor-pointer text-xl shadow-sm ${
                              isAuthenticated && isFavorite(group.product_name)
                                ? 'text-red-500 bg-white'
                                : 'text-gray-300 bg-white/80 hover:text-red-300'
                            }`}
                          >
                            {isAuthenticated && isFavorite(group.product_name) ? '❤️' : '🤍'}
                          </button>
                        </div>
                        {group.options.length > 1 && (
                          <div className="absolute top-4 right-4 bg-kiwi-dark/95 backdrop-blur-md text-white text-xs font-black px-3 py-1.5 rounded-xl uppercase tracking-widest shadow-lg border border-white/20">
                            {group.options.length} Stores
                          </div>
                        )}
                      </div>

                      {/* Content Area */}
                      <div className="p-6 flex flex-col flex-1">
                        <div className="flex-1">
                          <h3 className="text-xl font-bold text-gray-900 line-clamp-2 tracking-tight mb-2">
                            {group.product_name}
                          </h3>
                          <div className="flex items-center gap-6 mb-4 bg-gray-50/50 p-3 rounded-2xl border border-gray-100/50">
                            <div className="w-20 h-20 bg-white rounded-xl p-2.5 shadow-sm flex-shrink-0 flex items-center justify-center">
                              <img src={bestOption.logo_url} alt={bestOption.supermarket_name} className="w-full h-full object-contain" />
                            </div>
                            <div className="flex flex-col">
                              <span className="text-sm font-black text-kiwi-dark uppercase tracking-widest leading-none mb-2">
                                Best Price At
                              </span>
                              <span className="text-xl font-black text-kiwi-dark leading-tight">
                                {bestOption.supermarket_name}
                              </span>
                            </div>
                          </div>
                        </div>

                        {/* Pricing & Actions */}
                        <div className="mt-auto space-y-4">
                          <div className="flex items-end justify-between">
                            <PriceDisplay 
                              price={bestOption.price}
                              unitPrice={bestOption.unit_price}
                              isCheapest={true}
                              size="lg"
                            />
                            <button
                              onClick={(e) => {
                                e.stopPropagation()
                                if (isInBasket(group.product_name)) {
                                  removeFromBasket(group.product_name)
                                } else {
                                  addToBasket({
                                    name: group.product_name,
                                    image_url: group.image_url,
                                  })
                                }
                              }}
                              className={`w-12 h-12 flex items-center justify-center rounded-2xl transition-all border shadow-sm ${
                                isInBasket(group.product_name)
                                  ? 'bg-red-100 text-red-800 border-red-200 hover:bg-red-200'
                                  : 'bg-kiwi-dark text-white border-kiwi-dark shadow-kiwi/20 hover:scale-105'
                              }`}
                              title={isInBasket(group.product_name) ? "Remove" : "Add to Basket"}
                            >
                              {isInBasket(group.product_name) ? (
                                <span className="text-xl" aria-hidden="true">✕</span>
                              ) : (
                                <span className="text-xl" aria-hidden="true">🛒</span>
                              )}
                              <span className="sr-only">
                                {isInBasket(group.product_name) ? "Remove from basket" : "Add to basket"}
                              </span>
                            </button>
                          </div>

                          <button
                            onClick={() => setExpandedIndex(isExpanded ? null : groupIdx)}
                            className="w-full py-3 px-4 bg-gray-50 hover:bg-gray-100 text-gray-700 rounded-xl text-sm font-black uppercase tracking-widest transition-colors flex items-center justify-center gap-2 border-none cursor-pointer"
                          >
                            {isExpanded ? 'Close Prices' : 'View All Prices'}
                            <span className={`transition-transform duration-300 ${isExpanded ? 'rotate-180' : ''}`}>
                              ▼
                            </span>
                          </button>
                        </div>
                      </div>

                      {/* Expanded Pricing Table - High Legibility & Responsive Fix */}
                      {isExpanded && (
                        <div className="bg-gray-50/80 border-t border-gray-100 p-4 sm:p-6 space-y-4 animate-in fade-in slide-in-from-top-2">
                          <p className="text-xs font-black text-gray-600 uppercase tracking-[0.2em] mb-2 ml-1">
                            Available Store Prices
                          </p>
                          {group.options.map((option, optIdx) => (
                            <div
                              key={optIdx}
                              className={`flex flex-col sm:flex-row sm:items-center justify-between bg-white p-4 sm:p-5 rounded-2xl border transition-all gap-4 ${
                                optIdx === 0 
                                  ? 'border-kiwi/30 shadow-md ring-1 ring-kiwi/5' 
                                  : 'border-gray-100 shadow-sm'
                              }`}
                            >
                              <div className="flex items-center gap-4 min-w-0">
                                <div className="w-12 h-12 sm:w-16 sm:h-16 bg-gray-50 rounded-xl p-2 flex items-center justify-center flex-shrink-0">
                                  <img src={option.logo_url} alt={option.supermarket_name} className="w-full h-full object-contain" />
                                </div>
                                <div className="min-w-0">
                                  <span className="block font-black text-base sm:text-lg text-kiwi-dark leading-tight truncate">
                                    {option.supermarket_name}
                                  </span>
                                  <span className="text-xs sm:text-sm text-gray-600 font-medium flex items-center gap-1 mt-0.5 truncate">
                                    📍 {option.address.split(',')[0]}
                                  </span>
                                </div>
                              </div>
                              
                              <div className="flex sm:flex-col items-center sm:items-end justify-between sm:justify-center border-t sm:border-t-0 pt-3 sm:pt-0">
                                <span className="sm:hidden text-xs font-black text-gray-600 uppercase tracking-widest">Price</span>
                                <div className="text-right">
                                  <div className="flex flex-col items-end">
                                    <span className="font-black text-xl sm:text-2xl text-kiwi-dark tracking-tighter">
                                      ${option.price.toFixed(2)}
                                    </span>
                                    {option.unit_price && (
                                      <span className="text-xs sm:text-sm font-bold text-gray-700 uppercase tracking-tighter -mt-1">
                                        {option.unit_price}
                                      </span>
                                    )}
                                    {optIdx === 0 && (
                                      <span className="mt-1 px-2 py-0.5 bg-kiwi-dark text-white text-xs font-black uppercase tracking-widest rounded-lg">
                                        Cheapest
                                      </span>
                                    )}
                                  </div>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )
                },
              )}

              {/* Empty state when no results match the search */}
              {!isLoading && products?.length === 0 && debouncedSearchTerm && (
                <div className="col-span-full bg-white rounded-2xl p-12 text-center shadow-sm border border-gray-100">
                  <div className="text-5xl mb-4 text-center" aria-hidden="true">🥝</div>
                  <h3 className="text-xl font-bold text-kiwi-dark">
                    No products found
                  </h3>
                  <p className="text-gray-700 mt-2">
                    We couldn&apos;t find {debouncedSearchTerm}. <br />
                    Try searching for something else!
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* Sticky Sidebar - Now with a fixed width on XL screens to give more room to cards */}
          <div className="xl:w-80 space-y-6 flex-shrink-0">
            <div className="bg-white rounded-3xl p-8 shadow-sm border border-gray-100">
              <h3 className="text-xl font-black text-kiwi-dark mb-6 flex items-center gap-2">
                <span className="text-2xl" aria-hidden="true">🗺️</span> Nearby Stores
              </h3>
              <div className="aspect-square bg-gray-100 rounded-2xl overflow-hidden relative border border-gray-100">
                <StoreMap />
              </div>
            </div>

            {/* AI Insights Widget */}
            <div className="bg-kiwi/5 rounded-3xl p-8 border border-kiwi/10">
              <h4 className="text-kiwi font-black text-sm uppercase tracking-widest mb-4 flex items-center gap-2">
                <span>🥝</span> Kiwi Insight
              </h4>
              <p className="text-lg text-kiwi-dark/80 italic leading-relaxed">
                Milk prices in Auckland CBD have dropped by 5% this week. Keep
                an eye on New World specials!
              </p>
            </div>

            {/* Sidebar Basket Widget */}
            {basket.length > 0 && (
              <div className="bg-kiwi-dark rounded-3xl p-8 text-white shadow-xl shadow-kiwi-dark/20 animate-in fade-in slide-in-from-right-4">
                <div className="flex items-center gap-4 mb-6">
                  <div className="bg-kiwi-dark text-white p-3 rounded-2xl text-2xl shadow-lg">
                    🛒
                  </div>
                  <div>
                    <h4 className="font-black text-xl leading-none">Your Basket</h4>
                    <p className="text-white/90 text-sm font-black uppercase tracking-widest mt-2">
                      {basket.reduce((sum, item) => sum + item.quantity, 0)} Items Selected
                    </p>
                  </div>
                  </div>

                  <div className="space-y-4 mb-8">
                  {basket.slice(0, 3).map((item, i) => (
                    <div key={i} className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-white/10 rounded-xl p-1.5">
                        <img src={item.image_url} alt={item.name} className="w-full h-full object-contain mix-blend-screen" />
                      </div>
                      <span className="text-sm font-black truncate flex-1">{item.name}</span>
                      <span className="text-sm font-black text-white">×{item.quantity}</span>
                    </div>
                  ))}
                  {basket.length > 3 && (
                    <p className="text-xs text-white/80 font-black uppercase tracking-[0.2em] text-center bg-white/5 py-2 rounded-lg">
                      + {basket.length - 3} more items
                    </p>
                  )}
                  </div>

                  <button
                  onClick={() => setIsDrawerOpen(true)}
                  className="w-full py-4 bg-white text-kiwi-dark rounded-2xl font-black text-base shadow-lg hover:bg-kiwi-light transition-all border-none cursor-pointer"
                  >
                  Compare Total Prices
                  </button>

              </div>
            )}
          </div>
        </div>
      </div>

      {/* Floating Basket Widget (Removed as per request, now in sidebar) */}
    </div>
  )
}


export default ProductComparison
