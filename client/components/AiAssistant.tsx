import { useState, FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { getMealPlan } from '../apis/ai'
import { MealPlanResponse } from '../../models/products'
import { useBasket } from '../contexts/BasketContext'

const EXAMPLE_PROMPTS = [
  'ingredients for a chicken curry for 4',
  'a simple breakfast for two',
  'taco night for the family',
]

/**
 * AI shopping assistant: the user describes a meal in plain language, the
 * backend (Gemini) extracts ingredients and prices each across supermarkets,
 * and results are shown with the cheapest match per ingredient plus an
 * estimated total. Matched ingredients can be added to the basket.
 */
function AiAssistant() {
  const [prompt, setPrompt] = useState('')
  const { addToBasket } = useBasket()

  const {
    mutate,
    data,
    isPending,
    isError,
    reset,
  } = useMutation<MealPlanResponse, unknown, string>({
    mutationFn: (p: string) => getMealPlan(p),
    onError: (err: unknown) => {
      // superagent errors expose the HTTP status; give a tailored hint for
      // rate limiting and AI-provider outages, both surfaced by the backend.
      const status =
        typeof err === 'object' && err !== null && 'status' in err
          ? (err as { status?: number }).status
          : undefined
      if (status === 429) {
        toast.error('Too many AI requests. Please wait a moment and try again.')
      } else if (status === 502) {
        toast.error('The AI assistant is unavailable right now. Try again shortly.')
      } else {
        toast.error('Could not get a meal plan. Please try again.')
      }
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = prompt.trim()
    if (!trimmed) return
    mutate(trimmed)
  }

  const hasResults = data && data.items.length > 0
  const noMatches = data && data.items.length === 0

  return (
    <div className="bg-gradient-to-br from-kiwi/10 to-kiwi/5 rounded-3xl p-5 sm:p-8 border border-kiwi/20">
      <div className="flex items-center justify-between mb-4">
        <h4 className="text-kiwi font-black text-sm uppercase tracking-widest flex items-center gap-2">
          <span aria-hidden="true">🤖</span> AI Assistant
        </h4>
      </div>

      <p className="text-base text-kiwi-dark/80 leading-relaxed mb-4">
        Ask in plain language — e.g. &quot;cheapest breakfast basket near
        me&quot; — and let the assistant build a costed shopping list.
      </p>

      <form onSubmit={handleSubmit}>
        <div className="flex items-center gap-2 bg-white/70 rounded-2xl px-4 py-3 border border-kiwi/10 focus-within:border-kiwi/40 transition-colors">
          <span className="text-lg" aria-hidden="true">
            ✨
          </span>
          <input
            type="text"
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            maxLength={500}
            disabled={isPending}
            placeholder="Ask the AI assistant…"
            aria-label="Describe a meal or shopping goal"
            className="flex-1 bg-transparent border-none outline-none text-sm font-medium text-kiwi-dark placeholder:text-kiwi-dark/40 disabled:opacity-60"
          />
          <button
            type="submit"
            disabled={isPending || prompt.trim().length === 0}
            className="text-xs font-black uppercase tracking-widest text-white bg-kiwi px-3 py-2 rounded-xl border-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed hover:bg-kiwi-dark transition-colors"
          >
            {isPending ? 'Thinking…' : 'Ask'}
          </button>
        </div>
      </form>

      {/* Example prompts to help the user get started (hidden once results
          are showing to reduce clutter). */}
      {!data && !isPending && (
        <div className="mt-3 flex flex-wrap gap-2">
          {EXAMPLE_PROMPTS.map((example) => (
            <button
              key={example}
              type="button"
              onClick={() => {
                setPrompt(example)
                mutate(example)
              }}
              className="text-[11px] font-bold text-kiwi-dark/70 bg-white/60 border border-kiwi/10 rounded-full px-3 py-1 cursor-pointer hover:bg-white transition-colors"
            >
              {example}
            </button>
          ))}
        </div>
      )}

      {/* Loading state */}
      {isPending && (
        <p
          className="mt-4 text-sm font-bold text-kiwi-dark/70 animate-pulse"
          role="status"
          aria-live="polite"
        >
          Building your shopping list…
        </p>
      )}

      {/* Error state (toast also fires); offer a retry/reset. */}
      {isError && !isPending && (
        <div className="mt-4" role="alert">
          <p className="text-sm font-bold text-red-600">
            Something went wrong getting your meal plan.
          </p>
          <button
            type="button"
            onClick={() => reset()}
            className="mt-2 text-xs font-black uppercase tracking-widest text-kiwi underline bg-transparent border-none cursor-pointer p-0"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Empty result: prompt understood but no ingredients matched. */}
      {noMatches && !isPending && (
        <p className="mt-4 text-sm font-medium text-kiwi-dark/70" aria-live="polite">
          Couldn&apos;t work out any ingredients from that. Try describing a
          specific meal, e.g. &quot;chicken stir fry for two&quot;.
        </p>
      )}

      {/* Results */}
      {hasResults && !isPending && (
        <div className="mt-5" aria-live="polite">
          <div className="flex items-center justify-between mb-3">
            <h5 className="text-xs font-black uppercase tracking-widest text-kiwi-dark/70">
              Shopping list
            </h5>
            <span className="text-sm font-black text-kiwi-dark">
              Est. ${data.estimated_total.toFixed(2)}
            </span>
          </div>

          <ul className="flex flex-col gap-2">
            {data.items.map((item) => {
              const match = item.cheapest
              return (
                <li
                  key={item.ingredient}
                  className="flex items-center justify-between gap-3 bg-white/70 rounded-2xl px-4 py-3 border border-kiwi/10"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-black text-kiwi-dark capitalize truncate">
                      {item.ingredient}
                    </p>
                    {match ? (
                      <p className="text-xs text-kiwi-dark/70 truncate">
                        {match.product_name} · {match.supermarket_name} · $
                        {match.price.toFixed(2)}
                      </p>
                    ) : (
                      <p className="text-xs text-kiwi-dark/50">No match found</p>
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

          <button
            type="button"
            onClick={() => {
              data.items.forEach((item) => {
                if (item.cheapest) {
                  addToBasket({
                    name: item.cheapest.product_name,
                    image_url: item.cheapest.image_url,
                  })
                }
              })
              toast.success('Added all matched items to basket')
            }}
            className="mt-3 w-full py-3 bg-kiwi text-white rounded-2xl font-black text-sm border-none cursor-pointer hover:bg-kiwi-dark transition-colors"
          >
            Add all to basket
          </button>
        </div>
      )}
    </div>
  )
}

export default AiAssistant
