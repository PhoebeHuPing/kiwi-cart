import { Link } from 'react-router'

function DeveloperProfile() {
  return (
    <div className="max-w-4xl mx-auto py-12 px-4 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div className="bg-white rounded-3xl shadow-xl overflow-hidden border border-gray-100">
        {/* Header/Cover Image Placeholder */}
        <div className="h-48 bg-gradient-to-r from-kiwi to-kiwi-dark relative">
          <div className="absolute -bottom-16 left-12">
            <div className="w-32 h-32 rounded-3xl bg-white p-2 shadow-2xl border-4 border-white overflow-hidden">
              <div className="w-full h-full bg-gray-100 rounded-2xl flex items-center justify-center text-5xl">
                👨‍💻
              </div>
            </div>
          </div>
        </div>

        <div className="pt-20 pb-12 px-12">
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
            <div>
              <h2 className="text-4xl font-black text-kiwi-dark tracking-tight">
                Phoebe
              </h2>
              <p className="text-kiwi font-bold text-lg">Full-Stack Developer & KiwiCart Creator</p>
            </div>
            <div className="flex gap-3">
              <a 
                href="https://github.com" 
                target="_blank" 
                rel="noopener noreferrer"
                className="bg-gray-900 text-white px-6 py-2.5 rounded-xl font-bold text-sm hover:bg-black transition-all shadow-lg border-none"
              >
                GitHub
              </a>
              <Link 
                to="/"
                className="bg-kiwi-light text-kiwi-dark px-6 py-2.5 rounded-xl font-bold text-sm hover:bg-kiwi/20 transition-all border-none"
              >
                Back to Search
              </Link>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-12 mt-16">
            <div className="md:col-span-2 space-y-8">
              <section>
                <h3 className="text-xl font-black text-kiwi-dark flex items-center gap-2 mb-4">
                  <span className="text-2xl">🚀</span> Mission
                </h3>
                <p className="text-gray-700 leading-relaxed text-lg">
                  KiwiCart was born from a simple observation: grocery prices in New Zealand are confusing and often unfairly high. 
                  My mission is to leverage technology to provide transparency and help Kiwi families make better financial decisions 
                  during the cost-of-living crisis.
                </p>
              </section>

              <section>
                <h3 className="text-xl font-black text-kiwi-dark flex items-center gap-2 mb-4">
                  <span className="text-2xl">🛠️</span> Tech Stack
                </h3>
                <div className="flex flex-wrap gap-2">
                  {['React', 'TypeScript', 'Node.js', 'PostgreSQL', 'Tailwind CSS', 'Vite'].map(tech => (
                    <span key={tech} className="px-4 py-2 bg-gray-50 rounded-xl text-sm font-bold text-gray-600 border border-gray-100">
                      {tech}
                    </span>
                  ))}
                </div>
              </section>

              <section>
                <h3 className="text-xl font-black text-kiwi-dark flex items-center gap-2 mb-4">
                  <span className="text-2xl">💡</span> Future Vision
                </h3>
                <p className="text-gray-700 leading-relaxed">
                  I'm currently working on integrating Gemini AI to provide personalized shopping advice, 
                  receipt OCR for community-driven price updates, and expanded location-based mapping 
                  to include local butchers and produce markets.
                </p>
              </section>
            </div>

            <div className="space-y-6">
              <div className="bg-kiwi/5 rounded-2xl p-6 border border-kiwi/10">
                <h4 className="text-kiwi font-bold text-xs uppercase tracking-widest mb-4">Current Projects</h4>
                <ul className="space-y-4">
                  <li className="flex items-start gap-3">
                    <span className="text-kiwi mt-1">✓</span>
                    <span className="text-sm font-bold text-kiwi-dark">KiwiCart Price Tracker</span>
                  </li>
                  <li className="flex items-start gap-3">
                    <span className="text-kiwi mt-1">○</span>
                    <span className="text-sm text-gray-600">NZ Supermarket API SDK</span>
                  </li>
                  <li className="flex items-start gap-3">
                    <span className="text-kiwi mt-1">○</span>
                    <span className="text-sm text-gray-600">OpenFoodFacts Contributor</span>
                  </li>
                </ul>
              </div>

              <div className="bg-price/5 rounded-2xl p-6 border border-price/10">
                <h4 className="text-price font-bold text-xs uppercase tracking-widest mb-4">Contact</h4>
                <p className="text-sm text-gray-700 mb-2 font-bold">Interested in collaborating?</p>
                <button className="w-full bg-price text-white py-3 rounded-xl font-black text-sm hover:scale-105 transition-all border-none cursor-pointer">
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
