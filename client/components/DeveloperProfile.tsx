import { Link } from 'react-router'

function DeveloperProfile() {
  return (
    <div className="max-w-5xl mx-auto py-16 px-6 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div className="bg-white rounded-[3rem] shadow-2xl overflow-hidden border border-gray-100">
        {/* Header/Cover Image Placeholder */}
        <div className="h-64 bg-gradient-to-r from-kiwi to-kiwi-dark relative">
          <div className="absolute -bottom-20 left-16">
            <div className="w-40 h-40 rounded-[2.5rem] bg-white p-3 shadow-2xl border-4 border-white overflow-hidden">
              <div className="w-full h-full bg-gray-100 rounded-[2rem] flex items-center justify-center text-7xl">
                👨‍💻
              </div>
            </div>
          </div>
        </div>

        <div className="pt-28 pb-16 px-16">
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-10">
            <div>
              <h2 className="text-5xl font-black text-kiwi-dark tracking-tighter">
                Phoebe
              </h2>
              <p className="text-kiwi font-black text-2xl mt-2">Full-Stack Developer & KiwiCart Creator</p>
            </div>
            <div className="flex gap-4">
              <a 
                href="https://github.com" 
                target="_blank" 
                rel="noopener noreferrer"
                className="bg-gray-900 text-white px-8 py-4 rounded-2xl font-black text-base hover:bg-black transition-all shadow-xl border-none"
              >
                GitHub
              </a>
              <Link 
                to="/"
                className="bg-kiwi-light text-kiwi-dark px-8 py-4 rounded-2xl font-black text-base hover:bg-kiwi/20 transition-all border-none"
              >
                Back to Search
              </Link>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-16 mt-20">
            <div className="md:col-span-2 space-y-12">
              <section>
                <h3 className="text-2xl font-black text-kiwi-dark flex items-center gap-3 mb-6">
                  <span className="text-3xl">🚀</span> Mission
                </h3>
                <p className="text-gray-600 leading-relaxed text-xl font-medium">
                  KiwiCart was born from a simple observation: grocery prices in New Zealand are confusing and often unfairly high. 
                  My mission is to leverage technology to provide transparency and help Kiwi families make better financial decisions 
                  during the cost-of-living crisis.
                </p>
              </section>

              <section>
                <h3 className="text-2xl font-black text-kiwi-dark flex items-center gap-3 mb-6">
                  <span className="text-3xl">🛠️</span> Tech Stack
                </h3>
                <div className="flex flex-wrap gap-3">
                  {['React', 'TypeScript', 'Node.js', 'PostgreSQL', 'Tailwind CSS', 'Vite'].map(tech => (
                    <span key={tech} className="px-6 py-3 bg-gray-50 rounded-2xl text-base font-black text-gray-500 border border-gray-100">
                      {tech}
                    </span>
                  ))}
                </div>
              </section>

              <section>
                <h3 className="text-2xl font-black text-kiwi-dark flex items-center gap-3 mb-6">
                  <span className="text-3xl">💡</span> Future Vision
                </h3>
                <p className="text-gray-600 leading-relaxed text-xl font-medium">
                  I'm currently working on integrating Gemini AI to provide personalized shopping advice, 
                  receipt OCR for community-driven price updates, and expanded location-based mapping 
                  to include local butchers and produce markets.
                </p>
              </section>
            </div>

            <div className="space-y-8">
              <div className="bg-kiwi/5 rounded-[2rem] p-10 border border-kiwi/10">
                <h4 className="text-kiwi font-black text-sm uppercase tracking-[0.2em] mb-6">Current Projects</h4>
                <ul className="space-y-6">
                  <li className="flex items-start gap-4">
                    <span className="text-kiwi text-xl mt-0.5">✓</span>
                    <span className="text-lg font-black text-kiwi-dark">KiwiCart Price Tracker</span>
                  </li>
                  <li className="flex items-start gap-4">
                    <span className="text-kiwi text-xl mt-0.5">○</span>
                    <span className="text-lg text-gray-400 font-bold">NZ Supermarket API SDK</span>
                  </li>
                  <li className="flex items-start gap-4">
                    <span className="text-kiwi text-xl mt-0.5">○</span>
                    <span className="text-lg text-gray-400 font-bold">OpenFoodFacts Contributor</span>
                  </li>
                </ul>
              </div>

              <div className="bg-price/5 rounded-[2rem] p-10 border border-price/10">
                <h4 className="text-price font-black text-sm uppercase tracking-[0.2em] mb-6">Contact</h4>
                <p className="text-lg text-gray-700 mb-6 font-bold leading-tight">Interested in collaborating?</p>
                <button className="w-full bg-price text-white py-5 rounded-2xl font-black text-lg hover:scale-[1.02] active:scale-[0.98] transition-all border-none cursor-pointer shadow-xl shadow-price/20">
                  SEND MESSAGE
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default DeveloperProfile
