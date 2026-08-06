-- V5: Store tokens persistence table
CREATE TABLE IF NOT EXISTS store_tokens (
    id BIGSERIAL PRIMARY KEY,
    store_brand VARCHAR(100) NOT NULL UNIQUE,
    token TEXT NOT NULL,
    expires_at TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_store_tokens_brand ON store_tokens(store_brand);
