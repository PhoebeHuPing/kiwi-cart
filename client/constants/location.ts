/**
 * Shared location constants for the store map and price search.
 */

/**
 * Fallback location (Auckland Central) used when the browser cannot or is not
 * allowed to provide the user's location. Also the map's initial center.
 */
export const DEFAULT_LOCATION = { lat: -36.905, lng: 174.73 }

/**
 * Radius (km) within which a store is considered "nearby". Kept in sync with
 * the backend's StoreService.NearbyRadiusKm. Used for the nearby-store map view.
 */
export const NEARBY_RADIUS_KM = 5
