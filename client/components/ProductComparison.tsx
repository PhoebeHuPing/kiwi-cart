import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useDebounce } from 'use-debounce'
import { useAuth0 } from '@auth0/auth0-react'
import { getComparePrices, getFavorites, toggleFavorite } from '../apis/products'
import StoreMap from './StoreMap'
import { PriceComparisonData } from '../../models/products'
import { useBasket } from '../contexts/BasketContext'

interface GroupedProduct {
  product_name: string
  image_url: string
  options: PriceComparisonData[]
}

function ProductComparison() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['favorites'] })
    },
  })

  const isFavorite = (name: string) => favorites.includes(name)

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
                className="flex-1 bg-transparent border-none focus:ring-0 text-xl font-medium outline-none placeholder:text-gray-400"
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

            {/* Trending Categories Quick Tags */}
            <div className="flex flex-wrap gap-4">
              {trendingCategories.map((cat) => (
                <button
                  key={cat.name}
                  onClick={() => setSearchTerm(cat.name)}
                  className="px-6 py-3 bg-white rounded-2xl text-base font-bold text-gray-600 border border-gray-100 hover:border-kiwi hover:text-kiwi transition-all shadow-sm flex items-center gap-2 hover:scale-105"
                >
                  <span className="text-xl">{cat.icon}</span>
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

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-12 items-start">
          {/* Main Comparison List */}
          <div className="lg:col-span-2 space-y-6">
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

            <div className="flex flex-col gap-4">
              {groupedProducts?.map(
                (group: GroupedProduct, groupIdx: number) => {
                  const isExpanded = expandedIndex === groupIdx
                  const bestOption = group.options[0]

                  return (
                    <div
                      key={groupIdx}
                      className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden transition-all"
                    >
                      {/* Product Summary Header (Click to expand details) */}
                      <div
                        role="button"
                        tabIndex={0}
                        onClick={() =>
                          setExpandedIndex(isExpanded ? null : groupIdx)
                        }
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            setExpandedIndex(isExpanded ? null : groupIdx)
                          }
                        }}
                        className="w-full flex flex-col sm:flex-row items-center gap-10 p-8 text-left hover:bg-gray-50/50 transition-colors cursor-pointer"
                      >
                        <div className="w-48 h-48 flex-shrink-0 bg-gray-50 rounded-3xl p-6 shadow-inner">
                          <img
                            src={group.image_url}
                            alt=""
                            className="w-full h-full object-contain mix-blend-multiply"
                          />
                        </div>

                        <div className="flex-1">
                          <h3 className="text-2xl font-black text-kiwi-dark tracking-tight">
                            {group.product_name}
                          </h3>
                          <div className="flex items-center gap-4 mt-2">
                            <p className="text-gray-500 font-medium">
                              Available at {group.options.length} supermarkets
                            </p>
                            
                            <div className="flex items-center gap-2">
                              {isAuthenticated && (
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation()
                                    favoriteMutation.mutate(group.product_name)
                                  }}
                                  className={`w-8 h-8 flex items-center justify-center rounded-full transition-all border-none cursor-pointer text-xl ${
                                    isFavorite(group.product_name)
                                      ? 'text-red-500 bg-red-50'
                                      : 'text-gray-300 bg-gray-50 hover:text-red-300'
                                  }`}
                                  title={isFavorite(group.product_name) ? "Remove from Kitchen" : "Add to Kitchen"}
                                >
                                  {isFavorite(group.product_name) ? '❤️' : '🤍'}
                                </button>
                              )}

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
                                className={`text-xs font-black uppercase tracking-wider px-3 py-1 rounded-full border transition-all ${
                                  isInBasket(group.product_name)
                                    ? 'bg-red-50 text-red-600 border-red-100 hover:bg-red-100'
                                    : 'bg-kiwi/10 text-kiwi border-kiwi/20 hover:bg-kiwi/20'
                                }`}
                              >
                                {isInBasket(group.product_name)
                                  ? '✕ Remove'
                                  : '+ Add to Basket'}
                              </button>
                            </div>
                          </div>
                        </div>

                        <div className="flex flex-col items-center sm:items-end gap-0">
                          <span className="text-sm font-black text-gray-400 uppercase tracking-widest">
                            Best Price
                          </span>
                          <span className="text-4xl font-black text-price tracking-tighter">
                            ${bestOption.price.toFixed(2)}
                          </span>
                          {bestOption.unit_price && (
                            <span className="text-xs font-black text-kiwi uppercase tracking-widest -mt-1 mb-2">
                              {bestOption.unit_price}
                            </span>
                          )}
                          <div
                            className={`text-kiwi transition-transform duration-300 ${
                              isExpanded ? 'rotate-180' : ''
                            }`}
                            aria-hidden="true"
                          >
                            ▼
                          </div>
                        </div>
                      </div>

                      {/* Expanded Comparison Details */}
                      {isExpanded && (
                        <div className="bg-gray-50/50 border-t border-gray-100 p-10 space-y-6 animate-in fade-in slide-in-from-top-2 duration-300">
                          <p className="text-sm font-black text-gray-400 uppercase tracking-[0.2em] mb-4 ml-1">
                            Live Price Comparison
                          </p>

                          {group.options.map(
                            (option: PriceComparisonData, optIdx: number) => (
                              <div
                                key={optIdx}
                                className="flex items-center justify-between bg-white p-8 rounded-3xl border border-gray-100 shadow-sm hover:border-kiwi/30 transition-all"
                              >
                                <div className="flex items-center gap-10">
                                  {/* Supermarket Logo */}
                                  <div className="w-24 h-24 flex-shrink-0 flex items-center justify-center p-2">
                                    <img
                                      src={option.logo_url}
                                      alt=""
                                      className="w-full h-full object-contain filter drop-shadow-sm"
                                    />
                                  </div>

                                  <div>
                                    <p className="font-black text-2xl text-kiwi-dark tracking-tight">
                                      {option.supermarket_name}
                                    </p>
                                    <p className="text-base text-gray-500 mt-1 flex items-center gap-2">
                                      <span className="text-xl" aria-hidden="true">
                                        📍
                                      </span>{' '}
                                      {option.address}
                                    </p>
                                  </div>
                                </div>

                                <div className="flex items-center gap-10">
                                  <div className="text-right">
                                    <span className="text-4xl font-black text-kiwi-dark tracking-tighter">
                                      ${option.price.toFixed(2)}
                                    </span>
                                    {option.unit_price && (
                                      <p className="text-sm text-gray-400 font-black uppercase tracking-widest -mt-1">
                                        {option.unit_price}
                                      </p>
                                    )}
                                    {optIdx === 0 && (
                                      <p className="text-sm text-kiwi font-black uppercase tracking-widest mt-1">
                                        Best Choice
                                      </p>
                                    )}
                                  </div>
                                </div>
                              </div>
                            ),
                          )}
                        </div>
                      )}
                    </div>
                  )
                },
              )}

              {/* Empty state when no results match the search */}
              {!isLoading && products?.length === 0 && debouncedSearchTerm && (
                <div className="bg-white rounded-2xl p-12 text-center shadow-sm border border-gray-100">
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

          {/* Sticky Map and Sidebar Widgets */}
          <div className="lg:sticky lg:top-8 space-y-6">
            <div className="bg-white rounded-3xl p-8 shadow-sm border border-gray-100">
              <h3 className="text-xl font-black text-kiwi-dark mb-6 flex items-center gap-2">
                <span className="text-2xl" aria-hidden="true">🗺️</span> Nearby Stores
              </h3>
              <div className="aspect-square bg-gray-100 rounded-2xl overflow-hidden relative border border-gray-100">
                <StoreMap />
              </div>
            </div>

            {/* AI Insights Widget (Placeholder) */}
            <div className="bg-kiwi/5 rounded-3xl p-8 border border-kiwi/10">
              <h4 className="text-kiwi font-black text-sm uppercase tracking-widest mb-4">
                Kiwi Insight
              </h4>
              <p className="text-lg text-kiwi-dark/80 italic leading-relaxed">
                Milk prices in Auckland CBD have dropped by 5% this week. Keep
                an eye on New World specials!
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Floating Basket Widget */}
      {basket.length > 0 && (
        <div className="fixed bottom-8 right-8 z-50 animate-in fade-in slide-in-from-bottom-4">
          <button
            onClick={() => setIsDrawerOpen(true)}
            className="bg-kiwi-dark text-white p-4 rounded-2xl shadow-2xl flex items-center gap-3 hover:scale-105 transition-all group border-none cursor-pointer"
          >
            <div className="relative">
              <div className="bg-kiwi p-2 rounded-lg group-hover:rotate-12 transition-transform">
                🛒
              </div>
              <span className="absolute -top-2 -right-2 bg-price text-white text-xs font-black w-5 h-5 flex items-center justify-center rounded-full border-2 border-kiwi-dark">
                {basket.reduce((sum, item) => sum + item.quantity, 0)}
              </span>
            </div>
            <div className="text-left">
              <p className="font-black text-sm">Compare Basket</p>
              <p className="text-xs font-bold text-white/80 uppercase tracking-widest">
                Calculate Best Total
              </p>
            </div>
          </button>
        </div>
      )}
    </div>
  )
}

export default ProductComparison
