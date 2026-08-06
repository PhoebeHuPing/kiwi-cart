-- V4: Seed NZ supermarket store data
INSERT INTO stores (name, brand, logo_url, address, latitude, longitude, external_id) VALUES
    ('Pak''nSave Henderson', 'Pak''nSave', '/images/pak-n-save.webp', 'Henderson, West Auckland', -36.8819, 174.6336, '65defcf2-bc15-490e-a84f-1f13b769cd22'),
    ('Pak''nSave Albany', 'Pak''nSave', '/images/pak-n-save.webp', 'Albany, North Shore', -36.7277, 174.7089, 'a3f2c1d4-5678-4abc-b901-234567890abc'),
    ('New World Victoria Park', 'New World', '/images/new-world.webp', 'Victoria Park, Auckland CBD', -36.8485, 174.7523, 'dbdfdd2a-55f7-4870-9b51-979286323647'),
    ('New World Mt Eden', 'New World', '/images/new-world.webp', 'Mt Eden, Auckland', -36.8721, 174.7567, 'c2d3e4f5-6789-4def-a012-345678901234'),
    ('Woolworths Grey Lynn', 'Woolworths', '/images/woolworths.webp', 'Grey Lynn, Auckland', -36.8645, 174.7431, NULL),
    ('Woolworths Ponsonby', 'Woolworths', '/images/woolworths.webp', 'Ponsonby, Auckland', -36.8561, 174.7445, NULL),
    ('Woolworths Newmarket', 'Woolworths', '/images/woolworths.webp', 'Newmarket, Auckland', -36.8696, 174.7771, NULL)
ON CONFLICT DO NOTHING;
