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
import FeedbackBoard from './components/FeedbackBoard.tsx'
import AdminGtin from './components/AdminGtin.tsx'

// Protect the My Kitchen route so only authenticated users can access it
const ProtectedMyKitchen = withAuthenticationRequired(MyKitchen)
// Hidden admin tools; requires login, and the page itself gates on the admin role.
const ProtectedAdminGtin = withAuthenticationRequired(AdminGtin)

export const routes = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<App />}>
      <Route index element={<ProductComparison />} />
      <Route path="developer" element={<DeveloperProfile />} />
      <Route path="kitchen" element={<ProtectedMyKitchen />} />
      <Route path="feedback" element={<FeedbackBoard />} />
      <Route path="admin" element={<ProtectedAdminGtin />} />
    </Route>,
  ),
)
