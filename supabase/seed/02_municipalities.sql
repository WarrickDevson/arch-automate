-- ============================================================
-- Seed: 02_municipalities
-- Major South African municipalities with their zoning scheme names.
-- Categories: A = Metropolitan, B = Local, C = District
-- ============================================================

with provinces as (
    select id, code from public.provinces
)
insert into public.municipalities (province_id, name, short_name, category, zoning_scheme)
select p.id, m.name, m.short_name, m.category, m.zoning_scheme
from provinces p
join (values
    -- ── Gauteng ──────────────────────────────────────────────
    ('GP', 'City of Tshwane Metropolitan Municipality',   'Tshwane',       'A', 'Tshwane Town Planning Scheme 2008 (as revised)'),
    ('GP', 'City of Johannesburg Metropolitan Municipality', 'Johannesburg','A', 'City of Johannesburg Municipal Planning By-Law 2016'),
    ('GP', 'Ekurhuleni Metropolitan Municipality',        'Ekurhuleni',    'A', 'Ekurhuleni Metropolitan Municipality Town Planning Scheme 2014'),
    ('GP', 'Rand West City Local Municipality',           'Rand West',     'B', 'Randfontein Town Planning Scheme 1988'),
    ('GP', 'Mogale City Local Municipality',              'Mogale City',   'B', 'Krugersdorp Town Planning Scheme 1980'),
    ('GP', 'Emfuleni Local Municipality',                 'Emfuleni',      'B', 'Emfuleni Municipality Spatial Planning By-Law'),
    ('GP', 'Midvaal Local Municipality',                  'Midvaal',       'B', 'Midvaal Local Municipality Land Use Scheme 2018'),
    ('GP', 'Lesedi Local Municipality',                   'Lesedi',        'B', 'Lesedi Land Use Management Scheme'),

    -- ── Western Cape ─────────────────────────────────────────
    ('WC', 'City of Cape Town Metropolitan Municipality', 'Cape Town',     'A', 'City of Cape Town Municipal Planning By-Law 2015'),
    ('WC', 'Drakenstein Local Municipality',              'Drakenstein',   'B', 'Drakenstein Integrated Zoning Scheme'),
    ('WC', 'Stellenbosch Local Municipality',             'Stellenbosch',  'B', 'Stellenbosch Municipality Zoning Scheme'),
    ('WC', 'George Local Municipality',                   'George',        'B', 'George Municipality Zoning Scheme'),
    ('WC', 'Breede Valley Local Municipality',            'Breede Valley', 'B', 'Breede Valley Zoning Scheme'),
    ('WC', 'Swartland Local Municipality',                'Swartland',     'B', 'Swartland Zoning Scheme'),
    ('WC', 'Mossel Bay Local Municipality',               'Mossel Bay',    'B', 'Mossel Bay Municipality Zoning Scheme'),

    -- ── KwaZulu-Natal ─────────────────────────────────────────
    ('KZN', 'eThekwini Metropolitan Municipality',        'eThekwini',     'A', 'eThekwini Municipality Scheme'),
    ('KZN', 'Msunduzi Local Municipality',                'Msunduzi',      'B', 'Msunduzi Municipality Town Planning Scheme'),
    ('KZN', 'uMhlathuze Local Municipality',              'uMhlathuze',    'B', 'uMhlathuze Land Use Management Scheme'),
    ('KZN', 'Newcastle Local Municipality',               'Newcastle',     'B', 'Newcastle Town Planning Scheme'),
    ('KZN', 'KwaDukuza Local Municipality',               'KwaDukuza',     'B', 'KwaDukuza Spatial Planning and Land Use Scheme'),

    -- ── Eastern Cape ─────────────────────────────────────────
    ('EC', 'Buffalo City Metropolitan Municipality',      'Buffalo City',  'A', 'Buffalo City Metro Municipality Zoning Scheme'),
    ('EC', 'Nelson Mandela Bay Metropolitan Municipality','NMB Metro',     'A', 'NMB Municipal Spatial Planning and Land Use Scheme'),
    ('EC', 'Makana Local Municipality',                   'Makana',        'B', 'Makana Land Use Management Scheme'),
    ('EC', 'Kouga Local Municipality',                    'Kouga',         'B', 'Kouga Land Use Management Scheme'),

    -- ── Free State ───────────────────────────────────────────
    ('FS', 'Mangaung Metropolitan Municipality',          'Mangaung',      'A', 'Mangaung Metropolitan Municipality Planning By-Law'),
    ('FS', 'Matjhabeng Local Municipality',               'Matjhabeng',    'B', 'Matjhabeng Town Planning Scheme'),

    -- ── Limpopo ──────────────────────────────────────────────
    ('LP', 'Polokwane Local Municipality',                'Polokwane',     'B', 'Polokwane Municipality Land Use Management By-Law'),
    ('LP', 'Greater Tzaneen Local Municipality',          'Tzaneen',       'B', 'Greater Tzaneen Land Use Management Scheme'),

    -- ── Mpumalanga ───────────────────────────────────────────
    ('MP', 'Mbombela Local Municipality',                 'Mbombela',      'B', 'Mbombela Land Use Management Scheme'),
    ('MP', 'Steve Tshwete Local Municipality',            'Steve Tshwete', 'B', 'Steve Tshwete Town Planning Scheme'),

    -- ── Northern Cape ────────────────────────────────────────
    ('NC', 'Sol Plaatje Local Municipality',              'Sol Plaatje',   'B', 'Sol Plaatje Town Planning Scheme'),

    -- ── North West ───────────────────────────────────────────
    ('NW', 'Rustenburg Local Municipality',               'Rustenburg',    'B', 'Rustenburg Town Planning Scheme'),
    ('NW', 'Madibeng Local Municipality',                 'Madibeng',      'B', 'Madibeng Local Municipality Spatial Planning By-Law')

) as m (province_code, name, short_name, category, zoning_scheme)
on p.code = m.province_code
on conflict (name) do nothing;
