import { useQuery } from '@tanstack/react-query'
import { useAuth0 } from '@auth0/auth0-react'
import toast from 'react-hot-toast'
import { getSuggestions } from '../apis/ai'
import { SuggestionsResponse } from '../../models/products'
import { useBasket } from '../contexts/BasketContext'

/**
 * Personalized AI suggestions for the signed-in user, shown on My Kitchen.
 * Suggestions are derived from the user's favorites by the backend (Gemini),
 * then priced across supermarkets with any cross-store saving highlighted.
 * Matched suggestions can be added to the basket.
 */
export default function SuggestionCards() {
  const { getAccessTokenSilently } = useAuth0()
  const { addToBasket } = useBasket()

  const { data, isLoading, isError } = useQuery<SuggestionsResponse>({
    queryKey: ['suggestions'],
    queryFn: async () => {
      const token = await getAccessTokenSilently()
      return getSuggestions(token)
    },
    // Suggestions are personalized and involve a billed AI call; keep them
    // fresh for a few minutes rather than refetching on every focus.
    staleTime: 5 * 60 * 1000,
    retry: false,
  })

  // Loading state.
  if (isLoading) {
    return (
      <section
        className="bg-gradient-to-br from-kiwi/10 to-kiwi/5 rounded-3xl p-5 sm:p-8 border border-kiwi/20"
        aria-label="Personalized suggestions"
      >
        <SuggestionsHeader />
        <p
          className="mt-4 text-sm font-bold text-kiwi-dark/70 animate-pulse"
          role="status"
          aria-live="polite"
        >
          Finding personalized picks for you…
        </p>
      </section>
    )
  }

  // Error state: surface quietly, don't block the rest of My Kitchen.
  if (isError) {
    return (
      <section
        className="bg-gradient-to-br from-kiwi/10 to-kiwi/5 rounded-3xl p-5 sm:p-8 border border-kiwi/20"
        aria-label="Personalized suggestions"
      >
        <SuggestionsHeader />
        <p className="mt-4 text-sm font-medium text-kiwi-dark/70" role="alert">
          Couldn&apos;t load suggestions right now. Please try again later.
        </p>
      </section>
    )
  }

  const items = data?.items ?? []

  // Empty state: no favorites yet or the AI returned nothing usable.
  if (items.length === 0) {
    return (
      <section
        className="bg-gradient-to-br from-kiwi/10 to-kiwi/5 rounded-3xl p-5 sm:p-8 border border-kiwi/20"
        aria-label="Personalized suggestions"
      >
        <SuggestionsHeader />
        <p className="mt-4 text-sm font-medium text-kiwi-dark/70">
          Favorite a few products and we&apos;ll suggest personalized picks and
          savings here.
        </p>
      </section>
    )
  }

  const totalSaving = data?.total_potential_saving ?? 0

  return (
    <section
      className="bg-gradient-to-br from-kiwi/10 to-kiwi/5 rounded-3xl p-5 sm:p-8 border border-kiwi/20"
      aria-label="Personalized suggestions"
    >
      <div className="flex items-center justify-between mb-4">
        <SuggestionsHeader />
        {totalSaving > 0 && (
          <span className="text-sm font-black text-price">
            Save up to ${totalSaving.toFixed(2)}
          </span>
        )}
      </div>

      <ul className="grid grid-cols-1 sm:grid-cols-2 gap-3" aria-live="polite">
        {items.map((item) => {
          const match = item.cheapest
          return (
            <li
              key={item.product}
              className="flex items-center justify-between gap-3 bg-white/70 rounded-2xl px-4 py-3 border border-kiwi/10"
            >
              <div className="min-w-0">
                <p className="text-sm font-black text-kiwi-dark capitalize truncate">
                  {item.product}
                </p>
                <p className="text-xs text-kiwi-dark/70 truncate">{item.reason}</p>
                {match ? (
                  <p className="text-xs text-kiwi-dark/70 truncate mt-1">
                    {match.supermarket_name} · ${match.price.toFixed(2)}
                    {item.potential_saving && item.potential_saving > 0 ? (
                      <span className="text-price font-bold">
                        {' '}
                        · save ${item.potential_saving.toFixed(2)}
                      </span>
                    ) : null}
                  </p>
                ) : (
                  <p className="text-xs text-kiwi-dark/50 mt-1">No match found</p>
                )}
              </div>

              {match && (
                <button
                  type="button"
                  onClick={() => {
                    addToBasket({
                      name: match.product_name,
                      image_url: match.image_url,
                    })
                    toast.success(`Added ${match.product_name} to basket`)
                  }}
                  aria-label={`Add ${match.product_name} to basket`}
                  className="shrink-0 text-[11px] font-black uppercase tracking-widest text-white bg-kiwi px-3 py-2 rounded-xl border-none cursor-pointer hover:bg-kiwi-dark transition-colors"
                >
                  Add
                </button>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

function SuggestionsHeader() {
  return (
    <h4 className="text-kiwi font-black text-sm uppercase tracking-widest flex items-center gap-2">
      <span aria-hidden="true">✨</span> Suggested for you
    </h4>
  )
}
