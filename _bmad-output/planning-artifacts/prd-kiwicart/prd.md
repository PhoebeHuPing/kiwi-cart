---
title: KiwiCart Product Requirements Document
status: final
created: 2026-06-07
updated: 2026-06-07
project: KiwiCart
stakeholder: Phoebe
---

# KiwiCart — Product Requirements Document

## 1. Executive Summary

KiwiCart is a community-driven, non-profit web platform that provides real-time supermarket price comparison across New Zealand's three major grocery brands (Pak'nSave, New World, Woolworths). Initially targeting Auckland, it empowers consumers to find the best prices for everyday essentials during the NZ cost of living crisis.

## 2. Problem Statement

New Zealand's grocery market is dominated by two duopoly groups (Foodstuffs and Woolworths NZ). Consumers lack a unified tool to compare prices across brands without visiting multiple websites or stores. Existing supermarket apps only show their own prices, creating information asymmetry that disadvantages shoppers.

## 3. Vision & Goals

**Vision:** Every Kiwi can instantly find the cheapest option for their grocery shop without effort.

**Goals:**
- G1: Provide accurate, real-time price comparison across Pak'nSave, New World, and Woolworths
- G2: Enable basket-level comparison so users can optimize their total shop, not just individual items
- G3: Help users find the nearest cheapest store via location-based search
- G4: Build a trusted community resource with zero commercial agenda

## 4. Target Users

**Primary Persona: Budget-Conscious Auckland Families**
- Parents managing weekly grocery budgets ($150-400/week)
- Price-sensitive, willing to switch stores for meaningful savings
- Comfortable using web apps on desktop and mobile browsers

**Secondary Persona: Flatmates & Students**
- Shared household shopping, splitting costs
- Highly price-sensitive, smaller basket sizes
- Primarily mobile users

## 5. Platform & Access

| Platform | Priority | Delivery |
|----------|----------|----------|
| Desktop Web | P0 | Responsive SPA (React) |
| Mobile Web | P0 | Responsive design, touch-optimized |
| Native Mobile / PWA | V2 | Progressive Web App |

## 6. Functional Requirements

### FR-1: Product Search & Price Comparison (Core)

- **FR-1.1:** Users can search products by name with debounced input
- **FR-1.2:** Search results display prices from all three supermarkets side-by-side, sorted cheapest-first
- **FR-1.3:** Each result shows: product name, image, price, supermarket brand, store logo, unit price
- **FR-1.4:** Unit price is calculated automatically (per L, per kg, per 100g) based on product name parsing
- **FR-1.5:** Results come from hybrid cache (DB) with 24-hour expiry; stale/missing data triggers real-time API fetch

### FR-2: Basket Comparison

- **FR-2.1:** Users can add multiple items with quantities to a virtual basket
- **FR-2.2:** System calculates total basket cost per supermarket
- **FR-2.3:** Missing items per store are clearly identified
- **FR-2.4:** Results are sorted by completeness (items found) then total price
- **FR-2.5:** Potential savings between cheapest and most expensive options are displayed

### FR-3: Store Locator

- **FR-3.1:** Google Maps integration showing nearby supermarkets
- **FR-3.2:** Users can filter by brand
- **FR-3.3:** Store distance calculated from user's coordinates (browser geolocation)
- **FR-3.4:** Radius-based search (default 5km)

### FR-4: User Accounts & Favorites

- **FR-4.1:** Auth0-based authentication (social login + email)
- **FR-4.2:** Authenticated users can save/remove favorite products
- **FR-4.3:** Favorites persist across sessions
- **FR-4.4:** Unauthenticated users can use all search/compare features without account

### FR-5: Data Freshness & Caching

- **FR-5.1:** Background cache with 24-hour expiry per product-supermarket pair
- **FR-5.2:** Cache miss triggers parallel real-time fetch from all three APIs
- **FR-5.3:** Successful fetches upsert into database cache for future requests
- **FR-5.4:** Cache updates are non-blocking (do not delay response)

## 7. Non-Functional Requirements

### NFR-1: Performance

- **NFR-1.1:** Search results returned within 3 seconds (cache hit <500ms, cache miss <3s) `[ASSUMPTION]`
- **NFR-1.2:** Basket comparison completes within 5 seconds for up to 10 items `[ASSUMPTION]`

### NFR-2: Availability & Scale

- **NFR-2.1:** Target 99% uptime during NZ business hours (8am-10pm NZST) `[ASSUMPTION]`
- **NFR-2.2:** Support 500 concurrent users at launch `[ASSUMPTION]`

### NFR-3: Data Accuracy

- **NFR-3.1:** Prices no more than 24 hours stale under normal operation
- **NFR-3.2:** When real-time fetch fails, cached data is served with a "last updated" indicator `[ASSUMPTION]`

### NFR-4: Accessibility

- **NFR-4.1:** WCAG AA compliant
- **NFR-4.2:** High-contrast theme support
- **NFR-4.3:** Full screen-reader compatibility

### NFR-5: Security

- **NFR-5.1:** Auth0 JWT-based authentication for protected endpoints
- **NFR-5.2:** No PII stored beyond Auth0 user ID
- **NFR-5.3:** All API communication over HTTPS

## 8. Data Integration & Compliance

**Critical Risk:** API integrations are unofficial/non-authorized.

- **DR-1:** No web scraping is employed; data retrieved via standard API calls
- **DR-2:** Monitor for API changes/blocks; implement graceful degradation per-supermarket
- **DR-3:** Respect rate limits; implement request throttling `[ASSUMPTION: no formal rate limit documentation exists]`
- **DR-4:** If a supermarket blocks access, that brand shows "temporarily unavailable" rather than crashing
- **DR-5:** Legal review recommended before public launch to assess ToS compliance risk

## 9. Success Metrics

| Metric | Target | Counter-Metric |
|--------|--------|----------------|
| Weekly active users (Auckland) | 1,000 within 3 months of launch `[ASSUMPTION]` | Bounce rate <60% |
| Average searches per session | ≥3 | Search-to-basket conversion >15% |
| Price data freshness | >95% of results <24h old | API failure rate <5% |
| User satisfaction (NPS) | >40 `[ASSUMPTION]` | Support complaints <10/week |

## 10. Scope & Boundaries

### In Scope (V1)
- Product search with real-time multi-store price comparison
- Basket comparison with totals per supermarket
- Google Maps store locator
- User accounts with favorites
- Desktop + mobile responsive web
- Auckland region stores

### Out of Scope (V2+)
- Historical price tracking & trend charts
- Gemini AI shopping recommendations
- Native mobile app / PWA
- Regions beyond Auckland
- Community-contributed prices
- Barcode scanning
- Price alerts / notifications

## 11. Technical Constraints

- **TC-1:** Single PostgreSQL database (production on Render)
- **TC-2:** Unofficial API dependency creates fragility; no SLA from data sources
- **TC-3:** Google Maps API has usage-based pricing; budget cap needed
- **TC-4:** Auth0 free tier limits (7,000 MAU)

## 12. Open Questions

- [ ] Legal review of unofficial API usage against supermarket ToS
- [ ] Google Maps API budget cap for location services
- [ ] Fallback strategy if one or more supermarkets permanently block API access
- [ ] Decision on whether to display "last updated" timestamp per price result

---

*Document generated: 2026-06-07 | Status: Draft*
