-- ============================================================
-- Seed: 01_provinces
-- All nine South African provinces.
-- ============================================================

insert into public.provinces (code, name) values
    ('EC',  'Eastern Cape'),
    ('FS',  'Free State'),
    ('GP',  'Gauteng'),
    ('KZN', 'KwaZulu-Natal'),
    ('LP',  'Limpopo'),
    ('MP',  'Mpumalanga'),
    ('NC',  'Northern Cape'),
    ('NW',  'North West'),
    ('WC',  'Western Cape')
on conflict (code) do nothing;
