import { Outlet, Link, useLocation } from 'react-router'
import { useAuth0 } from '@auth0/auth0-react'
import { Toaster } from 'react-hot-toast'
import { useBasket } from '../contexts/BasketContext'
import BasketDrawer from './BasketDrawer'

function App() {
  const { basket, setIsDrawerOpen } = useBasket()
  const { loginWithRedirect, logout, isAuthenticated, user, isLoading } = useAuth0()
  const location = useLocation()
  const totalItems = basket.reduce((sum, item) => sum + item.quantity, 0)
  const isHomePage = location.pathname === '/'

  const handleLogin = () => loginWithRedirect()
  const handleLogout = () => logout({ logoutParams: { returnTo: window.location.origin } })

  return (
    <>
      <Toaster position="bottom-center" />
      <nav className="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100 px-6 py-6">
        <div className="max-w-[1600px] mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link to="/" className="text-4xl hover:scale-110 transition-transform no-underline" aria-label="KiwiCart Home">🥝</Link>
            <div className="flex flex-col">
              <Link to="/" className="text-2xl font-black tracking-tighter text-kiwi-dark no-underline leading-none">
                KiwiCart
              </Link>
              <Link 
                to="/developer" 
                className="text-xs font-black text-kiwi hover:text-kiwi-dark hover:scale-105 transition-all no-underline mt-1 tracking-widest"
              >
                By Phoebe
              </Link>
            </div>
          </div>

          <div className="flex items-center gap-6">
            {isAuthenticated && (
              <Link 
                to="/kitchen" 
                className={`text-base font-bold transition-colors no-underline ${
                  location.pathname === '/kitchen' ? 'text-kiwi' : 'text-gray-600 hover:text-kiwi'
                }`}
              >
                My Kitchen
              </Link>
            )}

            {isLoading ? (
              <div className="w-8 h-8 border-2 border-kiwi border-t-transparent rounded-full animate-spin" />
            ) : isAuthenticated ? (
              <div className="flex items-center gap-4">
                <div className="flex flex-col items-end hidden sm:flex">
                  <span className="text-sm font-black text-kiwi-dark">{user?.nickname || user?.name}</span>
                  <button 
                    onClick={handleLogout}
                    className="text-[10px] font-black text-gray-400 uppercase tracking-widest hover:text-red-500 transition-colors bg-transparent border-none cursor-pointer p-0"
                  >
                    Logout
                  </button>
                </div>
                <img 
                  src={user?.picture} 
                  alt={user?.name} 
                  className="w-10 h-10 rounded-xl border-2 border-kiwi shadow-sm"
                />
              </div>
            ) : (
              <button
                onClick={handleLogin}
                className="text-base font-black text-kiwi hover:text-kiwi-dark transition-colors bg-transparent border-none cursor-pointer"
              >
                Sign In
              </button>
            )}

            <button
              onClick={() => setIsDrawerOpen(true)}
              className="group relative flex items-center gap-3 bg-kiwi-dark text-white px-6 py-3 rounded-2xl shadow-lg shadow-kiwi-dark/20 hover:scale-105 transition-all border-none cursor-pointer"
              aria-label="Open basket comparison"
            >
            <div className="relative">
              <span className="text-2xl group-hover:rotate-12 transition-transform block" aria-hidden="true">
                🛒
              </span>
              {totalItems > 0 && (
                <span className="absolute -top-2 -right-2 bg-price text-white text-xs font-black w-6 h-6 flex items-center justify-center rounded-full border-2 border-kiwi-dark animate-in zoom-in">
                  {totalItems}
                </span>
              )}
            </div>
            <span className="font-bold text-base hidden sm:block">My Basket</span>
          </button>
          </div>
        </div>
      </nav>

      {isHomePage && (
        <header className="relative bg-kiwi text-white py-24 px-6 overflow-hidden shadow-2xl">
          <div className="absolute inset-0 z-0">
            <img
              src="/images/supermarket.avif"
              alt=""
              className="w-full h-full object-cover opacity-40 mix-blend-overlay scale-110"
            />

            <div className="absolute inset-0 bg-gradient-to-r from-kiwi via-kiwi/60 to-transparent" />
          </div>

          <div className="relative z-10 max-w-[1600px] mx-auto">
            <div className="inline-block bg-white/20 backdrop-blur-md px-4 py-1.5 rounded-full text-sm font-bold uppercase tracking-[0.2em] mb-6 border border-white/30">
              🇳🇿 NZ&apos;s Community Price Tracker
            </div>
            <div className="flex flex-col items-start">
              <h1 className="text-7xl md:text-9xl font-black tracking-tighter drop-shadow-2xl leading-none">
                KiwiCart
              </h1>
              <Link 
                to="/developer" 
                className="mt-4 text-2xl md:text-4xl font-black text-white hover:text-kiwi-light hover:scale-105 hover:underline decoration-white/30 underline-offset-8 transition-all no-underline drop-shadow-2xl"
              >
                By Phoebe
              </Link>
            </div>
            <p className="mt-8 text-2xl md:text-3xl font-bold text-kiwi-light drop-shadow-lg max-w-3xl leading-tight">
              Stop overpaying. Compare prices across{' '}
              <span className="text-white underline decoration-kiwi-light/50 underline-offset-8">
                Pak&apos;nSave, New World, and Woolworths
              </span>{' '}
              in real-time.
            </p>

            <div className="mt-12 flex items-center gap-8 opacity-90">
              <div className="flex flex-col">
                <span className="text-4xl font-black">20k+</span>
                <span className="text-sm uppercase font-bold tracking-widest text-kiwi-light">
                  Products
                </span>
              </div>
              <div className="w-px h-12 bg-white/20" />
              <div className="flex flex-col">
                <span className="text-4xl font-black">100%</span>
                <span className="text-sm uppercase font-bold tracking-widest text-kiwi-light">
                  NZ Owned
                </span>
              </div>
            </div>
          </div>
        </header>
      )}
      <main className="max-w-[1600px] mx-auto py-12 px-6 md:px-12">
        <Outlet />
      </main>

      <BasketDrawer />
    </>
  )
}

export default App
