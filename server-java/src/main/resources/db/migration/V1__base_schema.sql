-- V1: Base schema for KiwiCart
CREATE TABLE IF NOT EXISTS stores (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    brand VARCHAR(100),
    logo_url VARCHAR(500),
    address VARCHAR(500),
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    external_id VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS products (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(500) NOT NULL,
    category VARCHAR(255),
    image_url VARCHAR(1000),
    brand VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS favorites (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    product_name VARCHAR(500) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_favorites_user_product UNIQUE (user_id, product_name)
);

CREATE INDEX IF NOT EXISTS idx_favorites_user_id ON favorites(user_id);
