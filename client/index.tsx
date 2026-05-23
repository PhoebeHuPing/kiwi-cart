import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { RouterProvider } from 'react-router/dom'
import { routes } from './routes'
import { BasketProvider } from './contexts/BasketContext'

import './styles/index.css'

const queryClient = new QueryClient()

/**
 * Main application entry point.
 * Initializes React Query for state management and sets up the router.
 */
document.addEventListener('DOMContentLoaded', () => {
  createRoot(document.getElementById('app') as HTMLElement).render(
    <QueryClientProvider client={queryClient}>
      <BasketProvider>
        <RouterProvider router={routes} />
      </BasketProvider>
      <ReactQueryDevtools />
    </QueryClientProvider>,
  )
})
