import { useEffect, useRef, useState } from 'react'
import { getSupermarkets } from '../apis/products'

interface Supermarket {
  id: number
  name: string
  address: string
  lat: number
  lng: number
}

declare global {
  interface Window {
    google: any
  }
}

export default function StoreMap() {
  const mapRef = useRef<HTMLDivElement>(null)
  const searchInputRef = useRef<HTMLInputElement>(null)
  const [supermarkets, setSupermarkets] = useState<Supermarket[]>([])
  const [mapInstance, setMapInstance] = useState<any>(null)
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null)
  const [isLoaded, setIsLoaded] = useState(false)
  const [mapError, setMapError] = useState('')
  const userMarkerRef = useRef<any>(null)

  // 1. Get User's current location via Browser Geolocation API
  useEffect(() => {
    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const { latitude, longitude } = position.coords
          console.log('Location found:', latitude, longitude)
          setUserLocation({ lat: latitude, lng: longitude })
        },
        (error) => {
          console.error('Geolocation error:', error.message)
        },
        { enableHighAccuracy: true }
      )
    }
  }, [])

  // 2. Dynamic script loading for Google Maps API using Environment Variable
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

  // 3. Fetch all supermarket store data from the local API
  useEffect(() => {
    getSupermarkets()
      .then(setSupermarkets)
      .catch((err) => console.error('Failed to fetch supermarkets:', err))
  }, [])

  // 4. Initialize Google Map Instance & Places Autocomplete
  useEffect(() => {
    if (isLoaded && mapRef.current && !mapInstance) {
      const map = new window.google.maps.Map(mapRef.current, {
        center: { lat: -36.8485, lng: 174.7633 }, // Default to Auckland CBD
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

        // Handle place selection from the autocomplete search box
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

  // 5. Update user marker position and center map when location changes
  useEffect(() => {
    if (mapInstance && userLocation && window.google) {
      const pos = new window.google.maps.LatLng(userLocation.lat, userLocation.lng)
      mapInstance.setCenter(pos)
      mapInstance.setZoom(14)

      if (userMarkerRef.current) {
        userMarkerRef.current.setPosition(pos)
      } else {
        // Create a custom blue circle marker for the user
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
          zIndex: 1000
        })
      }
    }
  }, [mapInstance, userLocation])

  // 6. Render individual supermarket markers with brand logos
  useEffect(() => {
    if (mapInstance && supermarkets.length > 0) {
      supermarkets.forEach((store) => {
        const marker = new window.google.maps.Marker({
          position: { lat: store.lat, lng: store.lng },
          map: mapInstance,
          title: store.name,
          icon: {
            // Select logo based on supermarket name
            url: store.name.toLowerCase().includes('pak') 
              ? '/images/pak-n-save.webp' 
              : store.name.toLowerCase().includes('new') 
                ? '/images/new-world.webp' 
                : '/images/woolworths.webp',
            scaledSize: new window.google.maps.Size(30, 30),
          }
        })

        // Information window displayed upon clicking a store marker
        const infoWindow = new window.google.maps.InfoWindow({
          content: `<div style="color: #1a2e35; padding: 5px;">
                      <h4 style="margin: 0; font-weight: bold;">${store.name}</h4>
                      <p style="margin: 5px 0 0; font-size: 12px;">${store.address}</p>
                    </div>`,
        })

        marker.addListener('click', () => {
          infoWindow.open(mapInstance, marker)
        })
      })
    }
  }, [mapInstance, supermarkets])

  return (
    <div className="w-full h-full relative bg-gray-50 rounded-xl overflow-hidden shadow-inner border border-gray-100">
      <div className="absolute top-3 left-3 right-3 z-10">
        <div className="relative group">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-600 group-focus-within:text-kiwi transition-colors" aria-hidden="true">
            📍
          </span>
          <input
            ref={searchInputRef}
            type="text"
            aria-label="Search for a location"
            placeholder="Search for a location..."
            className="w-full pl-9 pr-4 py-2.5 bg-white/95 backdrop-blur-sm rounded-xl shadow-lg border border-gray-100 focus:ring-2 focus:ring-kiwi/20 focus:border-kiwi outline-none text-sm transition-all placeholder:text-gray-600"
          />
        </div>
      </div>

      <div ref={mapRef} className="w-full h-full min-h-[400px]" />
      
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
