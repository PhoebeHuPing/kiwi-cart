import {
  Route,
  createBrowserRouter,
  createRoutesFromElements,
} from 'react-router'
import { withAuthenticationRequired } from '@auth0/auth0-react'

import App from './components/App.tsx'
import ProductComparison from './components/ProductComparison.tsx'
import DeveloperProfile from './components/DeveloperProfile.tsx'
import MyKitchen from './components/MyKitchen.tsx'

// Protect the My Kitchen route so only authenticated users can access it
const ProtectedMyKitchen = withAuthenticationRequired(MyKitchen)

export const routes = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<App />}>
      <Route index element={<ProductComparison />} />
      <Route path="developer" element={<DeveloperProfile />} />
      <Route path="kitchen" element={<ProtectedMyKitchen />} />
    </Route>,
  ),
)
