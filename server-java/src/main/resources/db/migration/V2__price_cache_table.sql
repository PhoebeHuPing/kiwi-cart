-- V2: Price cache table for hybrid caching strategy
CREATE TABLE IF NOT EXISTS price_cache (
    id BIGSERIAL PRIMARY KEY,
    search_query VARCHAR(500) NOT NULL,
    supermarket_name VARCHAR(100) NOT NULL,
    product_name VARCHAR(500) NOT NULL,
    image_url VARCHAR(1000),
    logo_url VARCHAR(500),
    address VARCHAR(500),
    lat DOUBLE PRECISION,
    lng DOUBLE PRECISION,
    price DOUBLE PRECISION NOT NULL,
    unit_price VARCHAR(100),
    cached_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_price_cache_query ON price_cache(search_query);
CREATE INDEX IF NOT EXISTS idx_price_cache_query_supermarket ON price_cache(search_query, supermarket_name);
CREATE INDEX IF NOT EXISTS idx_price_cache_cached_at ON price_cache(cached_at);
