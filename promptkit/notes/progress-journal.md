# Progress Journal - KiwiCart Refactoring

## 2026-04-25: Strategic Planning & Technical Roadmap

### 1. Project Mission & Pivot
- **Goal**: Community-driven price sharing platform for the NZ cost of living crisis.
- **Tech Stack**: React/TS, Node/Express, PostgreSQL (Render), Gemini AI, Tailwind CSS, Supertest.

### 2. Key Technical Decisions
- **Database**: Migrating from **SQLite to PostgreSQL**.
    - *Why?* Production persistence, high concurrency, and industry alignment.
- **Data Model**: Many-to-Many relationship via `product_prices` junction table.
- **Data Acquisition Strategy (Hybrid Model)**:
    - **Metadata**: Open Food Facts API (Product info/images).
    - **Initial Prices**: AI-assisted Knex Seeds (Simulated local prices).
    - **Real-time Updates**: User-Generated Content (UGC) via Auth0.
    - **Future Tech**: Gemini AI for receipt OCR scanning.

### 3. User Interaction Flow (The "Xiao Wang" Scenario)
1. **Discovery**: User shares location; Google Maps shows nearby supermarkets.
2. **Search**: User searches "Bread"; System returns a sorted price list from all nearby stores.
3. **AI Advice**: User asks Gemini for advice on combined shopping lists.
4. **Contribution**: User logs in via Auth0 to update a price they found in-store.

### 4. The 7-Day Sprint Roadmap
- **D1-2**: Data Foundation (PostgreSQL, Junction Tables, AI-assisted Seeding).
- **D3-4**: Core API & Swagger Docs (Comparison logic, Distance sorting).
- **D5**: Gemini AI Integration (Advice endpoint).
- **D6**: UI Overhaul (Tailwind) & Integration Testing (Supertest).
- **D7**: Deployment (Render) & Documentation.

### 5. Commitment
- **Daily Time**: 3-5 hours.
- **How to Resume**: Check `GEMINI.md` for instructions.

### Next Steps
- Initialize PostgreSQL connection in `knexfile.js`.
- Create migration for the `product_prices` table.
