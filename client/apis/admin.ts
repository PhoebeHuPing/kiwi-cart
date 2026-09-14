import request from 'superagent'
import { buildApiUrl } from './apiBaseUrl'

/** Summary returned by every GTIN backfill endpoint. */
export interface GtinBackfillResult {
  fetched: number
  inserted: number
  updated: number
  skipped: number
}

const base = '/v1/admin/gtins'

/**
 * Deep-fetch all Woolworths products matching a search term and record their
 * GTINs. Admin only.
 */
export async function backfillWoolworthsByName(
  q: string,
  token: string,
): Promise<GtinBackfillResult> {
  const response = await request
    .post(buildApiUrl(`${base}/woolworths/by-name`))
    .set('Authorization', `Bearer ${token}`)
    .send({ q })
  return response.body
}

/**
 * Record a single Woolworths product's GTIN by its sku. Admin only.
 */
export async function backfillWoolworthsBySku(
  sku: string,
  token: string,
): Promise<GtinBackfillResult> {
  const response = await request
    .post(buildApiUrl(`${base}/woolworths/by-sku`))
    .set('Authorization', `Bearer ${token}`)
    .send({ sku })
  return response.body
}

/**
 * Resolve missing GTINs for Foodstuffs rows via their detail endpoints,
 * rate-limited by delayMs between calls. Admin only.
 */
export async function backfillMissingFoodstuffs(
  count: number,
  delayMs: number,
  token: string,
): Promise<GtinBackfillResult> {
  const response = await request
    .post(buildApiUrl(`${base}/foodstuffs/backfill-missing`))
    .set('Authorization', `Bearer ${token}`)
    .send({ count, delayMs })
  return response.body
}
