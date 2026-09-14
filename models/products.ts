/**
 * Represents a base product entity in the system.
 */
export interface Product {
  id: number
  name: string
  category: string
  image_url: string
}

/**
 * Represents a supermarket brand and its physical location details.
 */
export interface Supermarket {
  id: number
  name: string
  logo_url: string
  address: string
  lat: number
  lng: number
}

/**
 * Data structure for a single product price at a specific supermarket.
 * Used for the main search results list.
 */
export interface PriceComparisonData {
  product_name: string
  display_product_name?: string // Normalized name with brand prepended if needed
  image_url: string
  supermarket_name: string
  logo_url: string
  address: string
  lat: number
  lng: number
  price: number
  product_id?: string // External product ID for Foodstuffs merging
  gtin?: string // Normalized GTIN for cross-platform grouping (null if unknown)
  volume?: string // Volume/size (e.g., "250ml", "1L")
  unit_price?: string // Calculated value (e.g., "$2.50/kg" or "$4.76/L")
}

/**
 * Summary of a full basket's cost at a specific supermarket.
 */
export interface BasketComparisonResult {
  supermarket_name: string
  logo_url: string
  total_price: number
  items_found: number
  missing_items: string[]
  details: {
    name: string
    price: number
    quantity: number
    subtotal: number
  }[]
}

/**
 * One ingredient extracted by the AI meal-plan assistant, paired with the
 * cheapest matching product across supermarkets (null when no match found).
 * Mirrors the backend `MealPlanItem` DTO.
 */
export interface MealPlanItem {
  ingredient: string
  cheapest: PriceComparisonData | null
}

/**
 * Result of an AI meal plan: the extracted ingredients with their cheapest
 * matches, plus the sum of those cheapest prices. Mirrors the backend
 * `MealPlanResponse` DTO (`POST /api/v1/ai/meal-plan`).
 */
export interface MealPlanResponse {
  items: MealPlanItem[]
  estimated_total: number
}

/**
 * One personalized suggestion for the signed-in user: the product to consider,
 * a short AI-written reason, the cheapest matching product (null when no match
 * was found), and the potential cross-store saving (null when fewer than two
 * stores had a price). Mirrors the backend `SuggestionItem` DTO.
 */
export interface SuggestionItem {
  product: string
  reason: string
  cheapest: PriceComparisonData | null
  potential_saving: number | null
}

/**
 * Personalized suggestions for the signed-in user, with the total potential
 * saving across all suggestions. Mirrors the backend `SuggestionsResponse` DTO
 * (`GET /api/v1/ai/suggestions`).
 */
export interface SuggestionsResponse {
  items: SuggestionItem[]
  total_potential_saving: number
}
