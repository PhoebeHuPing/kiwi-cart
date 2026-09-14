import { useState, useEffect } from 'react'
import {
  useQuery,
  useQueries,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query'
import { useDebounce } from 'use-debounce'
import { useAuth0 } from '@auth0/auth0-react'
import toast from 'react-hot-toast'
import {
  getComparePrices,
  getFavorites,
  toggleFavorite,
} from '../apis/products'
import StoreMap from './StoreMap'
import PriceDisplay from './ui/PriceDisplay'
import AiAssistant from './AiAssistant'
import { PriceComparisonData } from '../../models/products'
import { useBasket } from '../contexts/BasketContext'
import { DEFAULT_LOCATION } from '../constants/location'

interface GroupedProduct {
  product_name: string
  image_url: string
  product_id?: string
  gtin?: string
  options: PriceComparisonData[]
}

/**
 * Normalize a product name for display: capitalize the first letter of each
 * word so all-lowercase source names (e.g. "natures fresh toast bread white")
 * render consistently ("Natures Fresh Toast Bread White"). Words already
 * containing uppercase (brand casing like "UHT") are left untouched.
 */
function toTitleCase(input: string): string {
  return input.replace(/\S+/g, (word) =>
    /[A-Z]/.test(word) ? word : word.charAt(0).toUpperCase() + word.slice(1),
  )
}

function extractStoreLocation(supermarketName: string): string {
  if (!supermarketName) return supermarketName

  const patterns = [
    { regex: /^Pak'nSave\s+(.+)$/ },
    { regex: /^New World\s+(.+)$/ },
    { regex: /^Woolworths\s+(.+)$/ },
  ]

  for (const { regex } of patterns) {
    const match = supermarketName.match(regex)
    if (match && match[1]) {
      return match[1]
    }
  }

  return supermarketName
}

// Number of product cards shown per "page"; the Load more button reveals
// another batch of this size.
const PRODUCTS_PER_PAGE = 30

function ProductComparison() {
  const { getAccessTokenSilently, isAuthenticated, loginWithRedirect } =
    useAuth0()
  const queryClient = useQueryClient()
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm] = useDebounce(searchTerm, 500)
  const [showDropdown, setShowDropdown] = useState(false)
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null)
  // How many product cards are currently shown; grows via the "Load more"
  // button so a broad search does not render hundreds of cards at once.
  const [visibleCount, setVisibleCount] = useState(PRODUCTS_PER_PAGE)
  const { basket, addToBasket, isInBasket, removeFromBasket, setIsDrawerOpen } =
    useBasket()
  const featuredProducts = ['Milk', 'Bread', 'Eggs', 'Butter']

  // Resolve the user's location once on mount: use geolocation when allowed,
  // otherwise fall back to Auckland Central. Price queries wait for this so the
  // backend can pick the nearest priceable store per brand. `null` = resolving.
  const [location, setLocation] = useState<{ lat: number; lng: number } | null>(
    null,
  )
  useEffect(() => {
    if (!('geolocation' in navigator)) {
      setLocation(DEFAULT_LOCATION)
      return
    }
    navigator.geolocation.getCurrentPosition(
      (pos) =>
        setLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
      () => setLocation(DEFAULT_LOCATION),
      { enableHighAccuracy: true },
    )
  }, [])

  const {
    data: products,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ['compare', debouncedSearchTerm, location],
    queryFn: () => getComparePrices(debouncedSearchTerm, location ?? undefined),
    enabled: Boolean(debouncedSearchTerm) && location !== null,
  })

  const featuredQueries = useQueries({
    queries: featuredProducts.map((productName) => ({
      queryKey: ['featured', productName, location],
      queryFn: () => getComparePrices(productName, location ?? undefined),
      staleTime: 5 * 60 * 1000,
      enabled: location !== null,
    })),
  })

  const featuredResults = featuredQueries.flatMap((query) => query.data ?? [])
  const displayedProducts = debouncedSearchTerm ? products : featuredResults
  // Until the user's location is resolved we deliberately fire no price queries
  // (see `enabled: location !== null` above), so treat that window as loading
  // rather than showing empty/default results.
  const isFeaturedLoading =
    location === null || featuredQueries.some((query) => query.isLoading)

  // Unique stores appearing in the current results (featured on first load, or
  // search results), for the map. Deduped by supermarket name; requires valid
  // coordinates. The map fits its viewport to these stores.
  //
  // Only derived once the user's location is resolved. Until then the featured
  // and search queries are disabled, so the backend's default (Auckland
  // Central) stores from the no-location code path never reach the map.
  const resultStores = (() => {
    if (!location) return []
    const map = new Map<
      string,
      { name: string; storeName: string; address: string; latitude: number; longitude: number }
    >()
    for (const p of displayedProducts ?? []) {
      if (
        p.supermarket_name &&
        typeof p.lat === 'number' &&
        typeof p.lng === 'number' &&
        !map.has(p.supermarket_name)
      ) {
        map.set(p.supermarket_name, {
          name: p.supermarket_name,
          storeName: p.store_name_override || p.supermarket_name,
          address: p.address ?? '',
          latitude: p.lat,
          longitude: p.lng,
        })
      }
    }
    return Array.from(map.values())
  })()

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
            padding: '16px 24px',
            fontSize: '16px',
            fontWeight: 'bold',
            maxWidth: 'min(90vw, 600px)',
            textAlign: 'center',
            boxShadow: '0 25px 50px -12px rgb(0 0 0 / 0.5)',
          },
        },
      )
    },
  })

  const isFavorite = (name: string) => favorites.includes(name)

  const handleFavoriteClick = (e: React.MouseEvent, productName: string) => {
    e.stopPropagation()
    if (!isAuthenticated) {
      toast(
        (t) => (
          <span className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 w-full font-bold text-kiwi-dark text-base sm:text-xl">
            <span>Please sign in to save favorites!</span>
            <button
              onClick={() => {
                toast.dismiss(t.id)
                loginWithRedirect()
              }}
              className="bg-kiwi text-white px-6 py-2.5 sm:px-8 sm:py-3 rounded-2xl text-sm sm:text-base font-black uppercase tracking-wider border-none cursor-pointer hover:bg-kiwi-dark hover:scale-105 transition-all shadow-lg whitespace-nowrap"
            >
              Sign In Now
            </button>
          </span>
        ),
        {
          duration: 6000,
          style: {
            borderRadius: '32px',
            background: '#fff',
            color: '#333',
            border: '4px solid #f1f5f9',
            padding: '20px 28px',
            maxWidth: 'min(90vw, 800px)',
            boxShadow: '0 35px 60px -15px rgb(0 0 0 / 0.3)',
          },
        },
      )
      return
    }
    favoriteMutation.mutate(productName)
  }

  // Group the flat array of products into one card per real-world product.
  // Priority 1: GTIN — the cross-platform barcode, merges the same product
  //             across Woolworths + Foodstuffs.
  // Priority 2: product_id — Foodstuffs exact match (Pak'nSave + New World).
  // Priority 3: product name — fallback for rows without ids/GTINs.
  const groupedProducts = displayedProducts?.reduce(
    (acc: GroupedProduct[], current) => {
      let existingProduct: GroupedProduct | undefined

      // Priority 1: match by GTIN (if both have it)
      if (current.gtin) {
        existingProduct = acc.find((p) => p.gtin && p.gtin === current.gtin)
      }

      // Priority 2: match by product_id (if both have it)
      if (!existingProduct && current.product_id) {
        existingProduct = acc.find(
          (p) => p.product_id && p.product_id === current.product_id,
        )
      }

      // Priority 3: fall back to name-based matching
      if (!existingProduct) {
        existingProduct = acc.find(
          (p) => p.product_name === current.product_name,
        )
      }

      if (existingProduct) {
        const existingOptionIndex = existingProduct.options.findIndex(
          (opt) => opt.supermarket_name === current.supermarket_name,
        )

        if (existingOptionIndex !== -1) {
          // If this supermarket already has a price for this product, keep the cheapest one
          if (
            current.price < existingProduct.options[existingOptionIndex].price
          ) {
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
          product_id: current.product_id,
          gtin: current.gtin,
          options: [current],
        })
      }
      return acc
    },
    [],
  )

  // Order cards so cross-store comparable products (available at more than one
  // supermarket) come first — that is the core price-comparison value — while
  // preserving the existing order within each group.
  const sortedProducts = groupedProducts
    ? [...groupedProducts].sort((a, b) => {
        const aStores = new Set(a.options.map((o) => o.supermarket_name)).size
        const bStores = new Set(b.options.map((o) => o.supermarket_name)).size
        return bStores - aStores
      })
    : undefined

  // Cards actually rendered, capped by the Load more button.
  const visibleProducts = sortedProducts?.slice(0, visibleCount)
  const hasMore = sortedProducts ? visibleCount < sortedProducts.length : false

  // Reset paging whenever the search term changes so a new search starts at
  // the first page.
  useEffect(() => {
    setVisibleCount(PRODUCTS_PER_PAGE)
  }, [debouncedSearchTerm])

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
        <div className="sticky top-0 z-40 -mx-4 px-4 py-3 mb-12 bg-background/95 backdrop-blur-md border-b border-transparent transition-all data-[stuck]:border-gray-100">
          <div className="flex flex-col gap-4">
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
                                {toTitleCase(item.display_product_name || item.product_name)}
                              </h4>
                              <div className="flex items-center gap-1.5 mt-0.5">
                                <img
                                  src={item.logo_url}
                                  alt=""
                                  className="w-3 h-3 object-contain"
                                />
                                <span className="text-xs text-gray-600 font-bold uppercase tracking-tight">
                                  {extractStoreLocation(item.supermarket_name)}
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
                  className="px-3 py-1.5 md:px-4 md:py-2 bg-white rounded-xl text-sm md:text-base font-bold text-gray-600 border border-gray-100 hover:border-kiwi hover:text-kiwi transition-all shadow-sm flex items-center gap-2 hover:scale-105 whitespace-nowrap flex-shrink-0"
                >
                  <span className="text-base md:text-lg">{cat.icon}</span>
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

        <div className="flex flex-col lg:flex-row gap-12 items-start">
          {/* Main Comparison List - Now in a flexible container to maximize width */}
          <div className="flex-1 space-y-8 w-full">
            <h2 className="text-2xl sm:text-3xl font-black text-kiwi-dark mb-6 flex items-center gap-3">
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
                  Today&apos;s Picks
                </>
              )}
            </h2>

            {isError && (
              <div className="bg-red-50 text-red-600 p-4 rounded-xl border border-red-100">
                <span aria-hidden="true">⚠️</span> Error: {error.message}
              </div>
            )}

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {isFeaturedLoading &&
                !displayedProducts?.length &&
                !debouncedSearchTerm && (
                  <div className="col-span-full py-12 text-center text-gray-600">
                    <div className="w-8 h-8 border-4 border-kiwi border-t-transparent rounded-full animate-spin mx-auto mb-3" />
                    <p className="font-bold">Loading today&apos;s picks...</p>
                  </div>
                )}

              {visibleProducts?.map(
                (group: GroupedProduct, groupIdx: number) => {
                  const isExpanded = expandedIndex === groupIdx
                  const bestOption = group.options[0]

                  return (
                    <div
                      key={groupIdx}
                      className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden flex flex-col hover:shadow-xl transition-all duration-300 group"
                    >
                      {/* Flowbite-style Image Header */}
                      <div className="relative aspect-square bg-gray-50/50 p-4 sm:p-8 flex items-center justify-center overflow-hidden">
                        <img
                          src={group.image_url}
                          alt={group.product_name}
                          className="w-full h-full object-contain mix-blend-multiply group-hover:scale-110 transition-transform duration-500"
                        />
                        <div className="absolute top-4 left-4 flex flex-col gap-2">
                          <button
                            onClick={(e) =>
                              handleFavoriteClick(e, group.product_name)
                            }
                            className={`w-10 h-10 flex items-center justify-center rounded-xl transition-all border-none cursor-pointer text-xl shadow-sm ${
                              isAuthenticated && isFavorite(group.product_name)
                                ? 'text-red-500 bg-white'
                                : 'text-gray-300 bg-white/80 hover:text-red-300'
                            }`}
                          >
                            {isAuthenticated && isFavorite(group.product_name)
                              ? '❤️'
                              : '🤍'}
                          </button>
                        </div>
                        {group.options.length > 1 && (
                          <div className="absolute top-4 right-4 bg-kiwi-dark/95 backdrop-blur-md text-white text-xs font-black px-3 py-1.5 rounded-xl uppercase tracking-widest shadow-lg border border-white/20">
                            {group.options.length} Stores
                          </div>
                        )}
                      </div>

                      {/* Content Area */}
                      <div className="p-4 sm:p-6 flex flex-col flex-1">
                        <div className="flex-1">
                          <h3 className="text-lg sm:text-xl font-bold text-gray-900 line-clamp-2 tracking-tight mb-2 min-h-[3.5rem] sm:min-h-[4rem]">
                            {toTitleCase(group.options[0]?.display_product_name || group.product_name)}
                          </h3>
                          {/* Volume Display */}
                          {bestOption.volume && (
                            <div className="mb-4 text-sm font-semibold text-gray-600">
                              <span className="text-gray-700">{bestOption.volume}</span>
                            </div>
                          )}
                          <div className="flex items-center gap-3 sm:gap-4 mb-4 bg-gray-50/50 p-3 rounded-2xl border border-gray-100/50">
                            <div className="w-14 h-14 sm:w-16 sm:h-16 bg-white rounded-xl p-2 shadow-sm flex-shrink-0 flex items-center justify-center">
                              <img
                                src={bestOption.logo_url}
                                alt=""
                                className="max-w-full max-h-full object-contain"
                              />
                            </div>
                            <div className="flex flex-col min-w-0">
                              <span className="text-xs font-black text-kiwi-dark uppercase tracking-widest leading-none mb-1.5">
                                Best Price At
                              </span>
                              <span
                                className="text-base sm:text-lg font-black text-kiwi-dark leading-tight line-clamp-2 break-words cursor-help"
                                title={bestOption.supermarket_name}
                              >
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
                              title={
                                isInBasket(group.product_name)
                                  ? 'Remove'
                                  : 'Add to Basket'
                              }
                            >
                              {isInBasket(group.product_name) ? (
                                <span className="text-xl" aria-hidden="true">
                                  ✕
                                </span>
                              ) : (
                                <span className="text-xl" aria-hidden="true">
                                  🛒
                                </span>
                              )}
                              <span className="sr-only">
                                {isInBasket(group.product_name)
                                  ? 'Remove from basket'
                                  : 'Add to basket'}
                              </span>
                            </button>
                          </div>

                          <button
                            onClick={() =>
                              setExpandedIndex(isExpanded ? null : groupIdx)
                            }
                            className="w-full py-3 px-4 bg-gray-50 hover:bg-gray-100 text-gray-700 rounded-xl text-sm font-black uppercase tracking-widest transition-colors flex items-center justify-center gap-2 border-none cursor-pointer"
                          >
                            {isExpanded ? 'Close Prices' : 'View All Prices'}
                            <span
                              className={`transition-transform duration-300 ${isExpanded ? 'rotate-180' : ''}`}
                            >
                              ▼
                            </span>
                          </button>
                        </div>
                      </div>

                      {/* Expanded Pricing Table - High Legibility & Responsive Fix */}
                      {isExpanded && (
                        <div className="bg-gray-50/80 border-t border-gray-100 p-4 sm:p-6 space-y-3 animate-in fade-in slide-in-from-top-2 w-full">
                          <p className="text-xs font-black text-gray-600 uppercase tracking-[0.2em] mb-2 ml-1">
                            Available Store Prices
                          </p>
                          {group.options.map((option, optIdx) => (
                            <div
                              key={optIdx}
                              className={`flex items-center gap-4 bg-white p-5 sm:p-6 rounded-2xl border transition-all ${
                                optIdx === 0
                                  ? 'border-kiwi/30 shadow-md ring-1 ring-kiwi/5'
                                  : 'border-gray-100 shadow-sm'
                              }`}
                            >
                              <div className="w-12 h-12 bg-gray-50 rounded-lg p-1.5 flex items-center justify-center flex-shrink-0">
                                <img
                                  src={option.logo_url}
                                  alt=""
                                  className="w-full h-full object-contain"
                                />
                              </div>

                              <div className="flex-1 min-w-0 pr-3">
                                <div className="text-sm text-gray-700 font-semibold line-clamp-2">
                                  {extractStoreLocation(option.supermarket_name)}
                                </div>
                              </div>

                              <div className="text-right flex-shrink-0">
                                <span className="font-black text-lg text-kiwi-dark">
                                  ${option.price.toFixed(2)}
                                </span>
                                {option.unit_price && (
                                  <div className="text-xs font-bold text-gray-600">
                                    {option.unit_price}
                                  </div>
                                )}
                                {optIdx === 0 && (
                                  <div className="mt-1">
                                    <span className="inline-block px-2 py-1 bg-kiwi-dark text-white text-xs font-black rounded-lg">
                                      CHEAPEST
                                    </span>
                                  </div>
                                )}
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
                  <div className="text-5xl mb-4 text-center" aria-hidden="true">
                    🥝
                  </div>
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

            {/* Load more: reveals the next batch of product cards. */}
            {hasMore && (
              <div className="flex justify-center mt-8">
                <button
                  onClick={() =>
                    setVisibleCount((c) => c + PRODUCTS_PER_PAGE)
                  }
                  className="px-8 py-3 bg-white rounded-2xl text-base font-bold text-kiwi-dark border border-gray-200 shadow-sm hover:border-kiwi hover:text-kiwi hover:scale-105 transition-all"
                >
                  Load more (
                  {(sortedProducts?.length ?? 0) - (visibleProducts?.length ?? 0)}{' '}
                  more)
                </button>
              </div>
            )}
          </div>

          {/* Sticky Sidebar - Pinned to the viewport so Nearby Stores and
              Kiwi Insight stay visible for the full length of the product list.
              The column itself is the sticky element; its containing block is
              the tall flex row above, so it stays pinned while that row is in
              view instead of scrolling away with a short inner wrapper. */}
          <div className="lg:w-80 space-y-6 flex-shrink-0 w-full lg:sticky lg:top-40 lg:self-start lg:max-h-[calc(100vh-11rem)] lg:overflow-y-auto lg:pr-1 scrollbar-hide">
            {/* Nearby Stores map */}
            <div className="bg-white rounded-3xl p-4 sm:p-5 shadow-sm border border-gray-100">
              <h3 className="text-lg sm:text-xl font-black text-kiwi-dark mb-4 flex items-center gap-2">
                <span className="text-2xl" aria-hidden="true">
                  🗺️
                </span>{' '}
                Nearby Stores
              </h3>
              <div className="h-56 bg-gray-100 rounded-2xl overflow-hidden relative border border-gray-100">
                <StoreMap resultStores={resultStores} userLocation={location} />
              </div>
            </div>

            {/* Your Basket - always visible, shows an empty state when empty */}
            <div className="bg-kiwi-dark rounded-3xl p-5 sm:p-8 text-white shadow-xl shadow-kiwi-dark/20">
              <div className="flex items-center gap-4 mb-6">
                <div className="bg-white/10 text-white p-3 rounded-2xl text-2xl shadow-lg">
                  🛒
                </div>
                <div>
                  <h4 className="font-black text-xl leading-none">
                    Your Basket
                  </h4>
                  <p className="text-white/90 text-sm font-black uppercase tracking-widest mt-2">
                    {basket.reduce((sum, item) => sum + item.quantity, 0)} Items
                    Selected
                  </p>
                </div>
              </div>

              {basket.length > 0 ? (
                <>
                  <div className="space-y-4 mb-8">
                    {basket.slice(0, 3).map((item, i) => (
                      <div key={i} className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-white/10 rounded-xl p-1.5">
                          <img
                            src={item.image_url}
                            alt={item.name}
                            className="w-full h-full object-contain mix-blend-screen"
                          />
                        </div>
                        <span className="text-sm font-black truncate flex-1">
                          {item.name}
                        </span>
                        <span className="text-sm font-black text-white">
                          ×{item.quantity}
                        </span>
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
                </>
              ) : (
                <p className="text-sm text-white/80 leading-relaxed bg-white/5 rounded-2xl p-4">
                  Your basket is empty. Add products from the list to compare
                  their total price across supermarkets.
                </p>
              )}
            </div>

            {/* AI Assistant - plain-language meal planning powered by the
                backend Gemini meal-plan endpoint. */}
            <AiAssistant />
          </div>
        </div>
      </div>

      {/* Floating Basket Widget (Removed as per request, now in sidebar) */}
    </div>
  )
}

export default ProductComparison
