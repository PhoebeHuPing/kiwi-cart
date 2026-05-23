import { Outlet } from 'react-router'

function App() {
  return (
    <>
      <header className="relative bg-kiwi text-white py-20 px-6 overflow-hidden shadow-2xl">
        <div className="absolute inset-0 z-0">
          <img
            src="/images/supermarket.avif"
            alt="Supermarket Background"
            className="w-full h-full object-cover opacity-40 mix-blend-overlay scale-110"
          />

          <div className="absolute inset-0 bg-gradient-to-r from-kiwi via-kiwi/60 to-transparent" />
        </div>

        <div className="relative z-10 max-w-7xl mx-auto">
          <div className="inline-block bg-white/20 backdrop-blur-md px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-[0.2em] mb-4 border border-white/30">
            🇳🇿 NZ's Community Price Tracker
          </div>
          <h1 className="text-6xl md:text-8xl font-black tracking-tighter drop-shadow-2xl">
            KiwiCart
          </h1>
          <p className="mt-6 text-xl md:text-2xl font-bold text-kiwi-light drop-shadow-lg max-w-2xl leading-tight">
            Stop overpaying. Compare prices across{' '}
            <span className="text-white underline decoration-kiwi-light/50 underline-offset-8">
              Pak'nSave, New World, and Woolworths
            </span>{' '}
            in real-time.
          </p>

          <div className="mt-10 flex items-center gap-6 opacity-80">
            <div className="flex flex-col">
              <span className="text-3xl font-black">20k+</span>
              <span className="text-[10px] uppercase font-bold tracking-widest text-kiwi-light">
                Products
              </span>
            </div>
            <div className="w-px h-10 bg-white/20" />
            <div className="flex flex-col">
              <span className="text-3xl font-black">100%</span>
              <span className="text-[10px] uppercase font-bold tracking-widest text-kiwi-light">
                NZ Owned
              </span>
            </div>
          </div>
        </div>
      </header>
      <main className="max-w-7xl mx-auto py-10 px-4 md:px-8">
        <Outlet />
      </main>
    </>
  )
}

export default App
