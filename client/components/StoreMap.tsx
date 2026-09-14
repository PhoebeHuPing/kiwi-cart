import { useEffect, useRef, useState } from 'react'
import { DEFAULT_LOCATION } from '../constants/location'

/** A store derived from the current price-comparison results. */
export interface ResultStore {
  name: string
  address: string
  latitude: number
  longitude: number
}

interface StoreMapProps {
  /**
   * Stores from the current price-comparison results. The map shows exactly
   * these stores and zooms to fit them.
   */
  resultStores?: ResultStore[]
  /**
   * The user's resolved location (real or default). Supplied by the parent so
   * the map and price search share a single location source — the map does not
   * run its own geolocation. `null` while the parent is still resolving.
   */
  userLocation?: { lat: number; lng: number } | null
}

declare global {
  interface Window {
    google: any
  }
}

/**
 * StoreMap Component: Renders a Google Map with the user's location and the
 * stores from the current price comparison.
 *
 * This is a controlled component: it does NOT perform its own geolocation or
 * store fetching. The parent (ProductComparison) resolves the user's location
 * once and passes both `userLocation` and the derived `resultStores`, so the
 * map and the price search always agree on a single location.
 */
export default function StoreMap({ resultStores, userLocation }: StoreMapProps = {}) {
  const mapRef = useRef<HTMLDivElement>(null)
  const searchInputRef = useRef<HTMLInputElement>(null)
  const [mapInstance, setMapInstance] = useState<any>(null)
  const [isLoaded, setIsLoaded] = useState(false)
  const [mapError, setMapError] = useState('')
  const userMarkerRef = useRef<any>(null)
  // Track store markers so we can clear them before redrawing.
  const storeMarkersRef = useRef<any[]>([])

  const hasResultStores = !!resultStores && resultStores.length > 0

  // 1. Dynamic script loading for Google Maps API using Environment Variable
  useEffect(() => {
    const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY
    if (!apiKey) {
      setMapError('Missing Google Maps API Key in .env')
      return
    }

    if (window.google) {
      setIsLoaded(true)
      return
    }

    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places`
    script.async = true
    script.defer = true
    script.onload = () => setIsLoaded(true)
    script.onerror = () => setMapError('Failed to load Google Maps script')
    document.head.appendChild(script)
  }, [])

  // 2. Initialize Google Map Instance & Places Autocomplete
  useEffect(() => {
    if (isLoaded && mapRef.current && !mapInstance) {
      const map = new window.google.maps.Map(mapRef.current, {
        center: DEFAULT_LOCATION, // Auckland CBD until the user location resolves
        zoom: 12,
        mapTypeControl: false,
        fullscreenControl: false,
        styles: [
          {
            featureType: 'poi',
            elementType: 'labels',
            stylers: [{ visibility: 'off' }],
          },
        ],
      })
      setMapInstance(map)

      if (searchInputRef.current) {
        const autocomplete = new window.google.maps.places.Autocomplete(searchInputRef.current)
        autocomplete.bindTo('bounds', map)

        // Recenter the map when the user picks a place from autocomplete.
        autocomplete.addListener('place_changed', () => {
          const place = autocomplete.getPlace()
          if (!place.geometry || !place.geometry.location) return

          if (place.geometry.viewport) {
            map.fitBounds(place.geometry.viewport)
          } else {
            map.setCenter(place.geometry.location)
            map.setZoom(15)
          }
        })
      }
    }
  }, [isLoaded, mapInstance])

  // 3. Update the user marker when the supplied location changes. When no
  // result stores are driving the viewport, also center on the user.
  useEffect(() => {
    if (!mapInstance || !userLocation || !window.google) return

    const pos = new window.google.maps.LatLng(userLocation.lat, userLocation.lng)

    if (!hasResultStores) {
      mapInstance.setCenter(pos)
      mapInstance.setZoom(14)
    }

    if (userMarkerRef.current) {
      userMarkerRef.current.setPosition(pos)
    } else {
      userMarkerRef.current = new window.google.maps.Marker({
        position: pos,
        map: mapInstance,
        title: 'Your Location',
        icon: {
          path: window.google.maps.SymbolPath.CIRCLE,
          fillColor: '#4285F4',
          fillOpacity: 1,
          strokeColor: 'white',
          strokeWeight: 2,
          scale: 8,
        },
        zIndex: 1000,
      })
    }
  }, [mapInstance, userLocation, hasResultStores])

  // 4. Render the result-store markers and fit the viewport to them
  // (plus the user location, if known).
  useEffect(() => {
    if (!mapInstance || !window.google) return

    // Clear any previously drawn store markers.
    storeMarkersRef.current.forEach((m) => m.setMap(null))
    storeMarkersRef.current = []

    if (!hasResultStores) return

    const bounds = new window.google.maps.LatLngBounds()

    resultStores!.forEach((store) => {
      const position = { lat: store.latitude, lng: store.longitude }
      const marker = new window.google.maps.Marker({
        position,
        map: mapInstance,
        title: store.name,
        icon: {
          url: store.name.toLowerCase().includes('pak')
            ? '/images/pak-n-save.webp'
            : store.name.toLowerCase().includes('new')
              ? '/images/new-world.webp'
              : '/images/woolworths.webp',
          scaledSize: new window.google.maps.Size(30, 30),
        },
      })

      const infoWindow = new window.google.maps.InfoWindow({
        content: `<div style="color: #1a2e35; padding: 5px;">
                    <h4 style="margin: 0; font-weight: bold;">${store.name}</h4>
                    <p style="margin: 5px 0 0; font-size: 12px;">${store.address}</p>
                  </div>`,
      })
      marker.addListener('click', () => infoWindow.open(mapInstance, marker))

      storeMarkersRef.current.push(marker)
      bounds.extend(position)
    })

    if (userLocation) {
      bounds.extend(new window.google.maps.LatLng(userLocation.lat, userLocation.lng))
    }
    mapInstance.fitBounds(bounds)
    // Guard against over-zoom when all stores are very close together.
    window.google.maps.event.addListenerOnce(mapInstance, 'idle', () => {
      if (mapInstance.getZoom() > 15) mapInstance.setZoom(15)
    })
  }, [mapInstance, resultStores, hasResultStores, userLocation])

  return (
    <div className="w-full h-full relative flex flex-col overflow-hidden">
      {/* Search row: sits above the map in normal flow so it never
          overlaps the map content or the card title. */}
      <div className="flex-shrink-0 p-3">
        <div className="relative group">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-600 group-focus-within:text-kiwi transition-colors" aria-hidden="true">
            📍
          </span>
          <input
            ref={searchInputRef}
            type="text"
            aria-label="Search for a location"
            placeholder="Search for a location..."
            className="w-full pl-9 pr-4 py-2.5 bg-white rounded-xl shadow-sm border border-gray-200 focus:ring-2 focus:ring-kiwi/20 focus:border-kiwi outline-none text-sm transition-all placeholder:text-gray-600"
          />
        </div>
      </div>

      {/* Map fills the remaining space below the search row. */}
      <div className="relative flex-1 min-h-0">
        <div ref={mapRef} className="w-full h-full" />
      </div>

      {(!isLoaded || mapError) && (
        <div className="absolute inset-0 flex flex-col items-center justify-center p-6 text-center bg-gray-100/90 backdrop-blur-[2px] z-20">
          {mapError ? (
            <>
              <div className="text-3xl mb-3" aria-hidden="true">⚠️</div>
              <p className="text-red-600 font-bold text-sm">{mapError}</p>
              <p className="text-xs text-gray-600 mt-2 uppercase tracking-widest">Check your .env and API console</p>
            </>
          ) : (
            <>
              <div className="w-8 h-8 border-3 border-kiwi border-t-transparent rounded-full animate-spin mb-3"></div>
              <p className="text-kiwi-dark font-medium text-xs uppercase tracking-widest">Loading Map...</p>
            </>
          )}
        </div>
      )}
    </div>
  )
}
