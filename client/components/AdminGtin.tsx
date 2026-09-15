import { useState } from 'react'
import { useAuth0 } from '@auth0/auth0-react'
import {
  backfillWoolworthsByName,
  backfillWoolworthsBySku,
  backfillMissingFoodstuffs,
  GtinBackfillResult,
} from '../apis/admin'

// Auth0 namespaced roles claim (matches the backend RoleClaimType).
const ROLES_CLAIM = 'https://kiwicart.co.nz/roles'
const ADMIN_EMAIL = 'phoebe.ping.hu@gmail.com'

function useIsAdmin(): boolean {
  const { user } = useAuth0()
  const roles = (user?.[ROLES_CLAIM] as string[] | undefined) ?? []
  return (
    roles.includes('admin') ||
    user?.email?.trim().toLowerCase() === ADMIN_EMAIL
  )
}

/** Renders a backfill result summary. */
function ResultView({
  result,
  error,
  running,
}: {
  result: GtinBackfillResult | null
  error: string | null
  running: boolean
}) {
  if (running)
    return <p className="text-sm text-gray-500 mt-2">Running…</p>
  if (error) return <p className="text-sm text-red-600 mt-2">{error}</p>
  if (!result) return null
  return (
    <p className="text-sm text-gray-700 mt-2 font-mono">
      fetched {result.fetched} · inserted {result.inserted} · updated{' '}
      {result.updated} · skipped {result.skipped}
    </p>
  )
}

export default function AdminGtin() {
  const { getAccessTokenSilently } = useAuth0()
  const isAdmin = useIsAdmin()

  // Per-panel state
  const [nameTerm, setNameTerm] = useState('milk')
  const [sku, setSku] = useState('')
  const [count, setCount] = useState(50)
  const [delayMs, setDelayMs] = useState(2000)

  const [nameState, setNameState] = useState<PanelState>(emptyState)
  const [skuState, setSkuState] = useState<PanelState>(emptyState)
  const [missingState, setMissingState] = useState<PanelState>(emptyState)

  if (!isAdmin) {
    return (
      <div className="min-h-screen bg-background py-16">
        <div className="max-w-lg mx-auto bg-white rounded-2xl p-8 shadow-sm border border-gray-100 text-center">
          <div className="text-4xl mb-3">🔒</div>
          <h1 className="text-xl font-bold text-kiwi-dark">Admin only</h1>
          <p className="text-gray-600 mt-2">
            You need the admin role to access GTIN management.
          </p>
        </div>
      </div>
    )
  }

  async function run(
    action: (token: string) => Promise<GtinBackfillResult>,
    setState: (s: PanelState) => void,
  ) {
    setState({ running: true, result: null, error: null })
    try {
      const token = await getAccessTokenSilently()
      const result = await action(token)
      setState({ running: false, result, error: null })
    } catch (e) {
      setState({
        running: false,
        result: null,
        error: e instanceof Error ? e.message : 'Request failed',
      })
    }
  }

  return (
    <div className="min-h-screen bg-background py-10">
      <div className="max-w-2xl mx-auto px-4 space-y-6">
        <header>
          <h1 className="text-2xl font-black text-kiwi-dark">
            GTIN Management
          </h1>
          <p className="text-gray-600 mt-1">
            Backfill the product GTIN mapping used for cross-store matching.
          </p>
        </header>

        {/* API 1: Woolworths by name */}
        <section className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
          <h2 className="font-bold text-kiwi-dark">
            Woolworths — backfill by product name
          </h2>
          <p className="text-sm text-gray-500 mb-3">
            Deep-fetch all Woolworths results for a search term and record GTINs.
          </p>
          <div className="flex gap-3">
            <input
              value={nameTerm}
              onChange={(e) => setNameTerm(e.target.value)}
              placeholder="e.g. milk"
              className="flex-1 px-4 py-2 rounded-xl border border-gray-200 outline-none focus:border-kiwi"
            />
            <button
              onClick={() =>
                run(
                  (t) => backfillWoolworthsByName(nameTerm.trim(), t),
                  setNameState,
                )
              }
              disabled={nameState.running || !nameTerm.trim()}
              className="px-6 py-2 bg-kiwi text-white rounded-xl font-bold hover:bg-kiwi-dark disabled:opacity-50 transition-all"
            >
              Run
            </button>
          </div>
          <ResultView {...nameState} />
        </section>

        {/* API 2: Woolworths by sku */}
        <section className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
          <h2 className="font-bold text-kiwi-dark">
            Woolworths — backfill by sku
          </h2>
          <p className="text-sm text-gray-500 mb-3">
            Record a single Woolworths product by its sku.
          </p>
          <div className="flex gap-3">
            <input
              value={sku}
              onChange={(e) => setSku(e.target.value)}
              placeholder="e.g. 282768"
              className="flex-1 px-4 py-2 rounded-xl border border-gray-200 outline-none focus:border-kiwi"
            />
            <button
              onClick={() =>
                run((t) => backfillWoolworthsBySku(sku.trim(), t), setSkuState)
              }
              disabled={skuState.running || !sku.trim()}
              className="px-6 py-2 bg-kiwi text-white rounded-xl font-bold hover:bg-kiwi-dark disabled:opacity-50 transition-all"
            >
              Run
            </button>
          </div>
          <ResultView {...skuState} />
        </section>

        {/* API 3: Foodstuffs backfill missing */}
        <section className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
          <h2 className="font-bold text-kiwi-dark">
            Foodstuffs — backfill missing GTINs
          </h2>
          <p className="text-sm text-gray-500 mb-3">
            Resolve GTINs for Foodstuffs rows that have a product id but no GTIN,
            via their detail endpoints. Rate-limited by the delay.
          </p>
          <div className="flex flex-wrap gap-3 items-end">
            <label className="text-sm text-gray-600">
              Count
              <input
                type="number"
                min={1}
                max={500}
                value={count}
                onChange={(e) => setCount(Number(e.target.value))}
                className="block mt-1 w-28 px-3 py-2 rounded-xl border border-gray-200 outline-none focus:border-kiwi"
              />
            </label>
            <label className="text-sm text-gray-600">
              Delay (ms)
              <input
                type="number"
                min={0}
                max={5000}
                step={100}
                value={delayMs}
                onChange={(e) => setDelayMs(Number(e.target.value))}
                className="block mt-1 w-28 px-3 py-2 rounded-xl border border-gray-200 outline-none focus:border-kiwi"
              />
            </label>
            <button
              onClick={() =>
                run(
                  (t) => backfillMissingFoodstuffs(count, delayMs, t),
                  setMissingState,
                )
              }
              disabled={missingState.running || count <= 0}
              className="px-6 py-2 bg-kiwi text-white rounded-xl font-bold hover:bg-kiwi-dark disabled:opacity-50 transition-all"
            >
              Run
            </button>
          </div>
          <ResultView {...missingState} />
          {missingState.running && (
            <p className="text-xs text-gray-400 mt-1">
              This may take a while: ~{count} requests with {delayMs}ms between
              each.
            </p>
          )}
        </section>
      </div>
    </div>
  )
}

interface PanelState {
  running: boolean
  result: GtinBackfillResult | null
  error: string | null
}

const emptyState: PanelState = { running: false, result: null, error: null }
