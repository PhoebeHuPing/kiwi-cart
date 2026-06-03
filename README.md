# KiwiCart - NZ Supermarket Price Sharing

KiwiCart is a community-driven platform designed to tackle the New Zealand Cost of Living Crisis. It empowers users to compare supermarket prices across major brands (Pak'nSave, New World, and Woolworths) using real-time data and location-based search.

## Project Mission
In a market dominated by few players, KiwiCart aims to provide price transparency. We believe every Kiwi should have access to the best prices for their daily essentials without having to visit multiple websites or stores.

## Core Features

- **Live Data Integration:** Real-time price updates from Pak'nSave, New World, and Woolworths to ensure accurate comparison.
- **Multi-store Synchronization:** Seamlessly sync basket items across different supermarket brands to find the best total value.
- **Hybrid Caching:** Background database caching with 24-hour expiry to ensure speed while maintaining data accuracy.
- **Location-Aware Store Map:** Integrated Google Maps view to find the cheapest stores near you.
- **Favorites & Profiles:** Secure user accounts via Auth0 to save your favorite products and personalized shopping lists.
- **Accessibility First:** Fully compliant with WCAG AA standards, featuring high-contrast themes and full screen-reader support.

## Tech Stack

### Frontend
- React 18 with TypeScript
- Tailwind CSS 4.0 (Utility-first styling with custom accessibility-friendly colors)
- TanStack Query (React Query) (Efficient data fetching and caching)
- Vite 7 (Modern build tool)

### Backend
- Node.js & Express
- Knex.js (Query builder)
- PostgreSQL (Production database on Render) / SQLite (Local development)
- Auth0 (Secure authentication and identity management)

### Services & AI
- Google Maps API (Store location and autocomplete)
- Gemini AI (Smart shopping insights and price trend analysis - In Progress)

## Getting Started

### Prerequisites
- Node.js (v18+)
- PostgreSQL (Local or Docker)
- Google Maps API Key
- Auth0 Domain & Client ID

### Installation

1. **Clone the Repo:**
   ```bash
   git clone <repository-url>
   cd KiwiCart
   ```

2. **Install Dependencies:**
   ```bash
   npm install
   ```

3. **Environment Variables:**
   Create a `.env` file in the root (see `.env.example` if available):
   ```env
   # Database
   DATABASE_URL=postgres://user:password@localhost:5432/kiwicart

   # Auth0
   AUTH0_DOMAIN=your-domain.auth0.com
   AUTH0_AUDIENCE=your-api-identifier

   # Google Maps
   VITE_GOOGLE_MAPS_API_KEY=your_key_here
   ```

4. **Setup Database:**
   ```bash
   npm run knex migrate:latest
   npm run knex seed:run
   ```

5. **Run Development Server:**
   ```bash
   npm run dev
   ```
   Visit `http://localhost:5173` to see KiwiCart in action.

## Testing
We maintain high code quality with automated tests:
```bash
npm test          # Run all tests (Vitest)
npm run lint      # Run ESLint
```

## License
ISC

---
*Helping Kiwis save money, one basket at a time.*
