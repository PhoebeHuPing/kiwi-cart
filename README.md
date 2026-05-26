# 🥝 KiwiCart - NZ Supermarket Price Sharing

KiwiCart is a community-driven platform designed to tackle the **New Zealand Cost of Living Crisis**. It empowers users to compare supermarket prices across major brands (Pak'nSave, New World, and Woolworths) using real-time data and location-based search.

![Accessibility Status](https://img.shields.io/badge/Accessibility-WCAG%20AA-success)
![React](https://img.shields.io/badge/Frontend-React%20%2B%20TS-blue)
![Express](https://img.shields.io/badge/Backend-Node.js%20%2B%20Express-green)

## 🎯 Project Mission
In a market dominated by few players, KiwiCart aims to provide price transparency. We believe every Kiwi should have access to the best prices for their daily essentials without having to visit multiple websites or stores.

## ✨ Features

- **Multi-Store Price Comparison:** Compare items across Pak'nSave, New World, and Woolworths in a single view.
- **Smart Basket Calculation:** Add multiple items to your basket and instantly see which supermarket offers the lowest total cost for your entire shop.
- **Location-Aware Search:** Integrated Google Maps view to find the cheapest stores near your current location.
- **Accessibility First:** Fully compliant with **WCAG AA standards**, featuring high-contrast themes, ARIA labels for screen readers, and keyboard-friendly navigation.
- **Community Driven:** Focused on user-shared data and transparency (avoiding aggressive scraping).

## 🛠️ Tech Stack

### Frontend
- **React 18** with **TypeScript**
- **Tailwind CSS** (Utility-first styling with custom accessibility-friendly colors)
- **TanStack Query (React Query)** (Efficient data fetching and caching)
- **Vite** (Modern build tool)

### Backend
- **Node.js** & **Express**
- **Knex.js** (Query builder)
- **PostgreSQL** (Production database) / **SQLite** (Local development)

### Services & AI
- **Google Maps API** (Store location and autocomplete)
- **Gemini AI** (Smart shopping insights and price trend analysis)
- **Auth0** (Secure authentication)

## 🏁 Getting Started

### Prerequisites
- Node.js (v18+)
- A Google Maps API Key (for the store locator)

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
   Create a `.env` file in the root:
   ```env
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

## 🧪 Testing
We maintain high code quality with automated tests:
```bash
npm test          # Run all tests (Vitest)
npm run test:ui   # Run tests with a visual dashboard
```

## ♿ Accessibility Commitment
KiwiCart is designed to be usable by everyone. Recent updates include:
- **Contrast Ratios:** All text elements meet a minimum 4.5:1 ratio.
- **Screen Readers:** Comprehensive ARIA labels and roles for all interactive elements.
- **Keyboard Navigation:** Logic for focus management and skip-links.

## 📄 License
ISC

---
*Helping Kiwis save money, one basket at a time.* 🥝🇳🇿
