import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { RouterProvider } from 'react-router/dom'
import { Auth0Provider } from '@auth0/auth0-react'
import { routes } from './routes'
import { BasketProvider } from './contexts/BasketContext'

import './styles/index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes: don't refetch if data is fresh
      refetchOnWindowFocus: false,
    },
  },
})

/**
 * Main application entry point.
 * Initializes React Query for state management, sets up Auth0 for authentication,
 * and configures the router.
 */
document.addEventListener('DOMContentLoaded', () => {
  createRoot(document.getElementById('app') as HTMLElement).render(
    <Auth0Provider
      domain={import.meta.env.VITE_AUTH0_DOMAIN}
      clientId={import.meta.env.VITE_AUTH0_CLIENT_ID}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      }}
    >
      <QueryClientProvider client={queryClient}>
        <BasketProvider>
          <RouterProvider router={routes} />
        </BasketProvider>
        <ReactQueryDevtools />
      </QueryClientProvider>
    </Auth0Provider>,
  )
})
