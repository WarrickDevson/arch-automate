-- ============================================================
-- Seed: 03_zoning_scheme_zones
-- Common residential and commercial zone parameters.
-- Values are indicative industry standards — override per
-- municipality's published scheme where figures differ.
-- ============================================================

-- Helper: resolve municipality id by short_name
-- Zones are linked to "Johannesburg" as the canonical reference set.
-- Each municipality's scheme should be loaded separately in production.

-- ────────────────────────────────────────────────────────────
-- GENERIC RESIDENTIAL ZONES (Gauteng / most municipalities)
-- Source: SPLUMA principles + common scheme defaults
-- ────────────────────────────────────────────────────────────
with muni as (
    select id from public.municipalities where short_name = 'Johannesburg' limit 1
)
insert into public.zoning_scheme_zones (
    municipality_id, zone_code, description,
    max_coverage_pct, max_far, max_height_m,
    front_setback_m, rear_setback_m, side_setback_m,
    permitted_uses, notes
)
select muni.id, z.zone_code, z.description,
       z.max_coverage_pct, z.max_far, z.max_height_m,
       z.front_setback_m, z.rear_setback_m, z.side_setback_m,
       z.permitted_uses, z.notes
from muni, (values
    ('Residential 1',
     'Low-density single residential. Freestanding dwelling houses.',
     50.0, 1.0, 8.5, 4.5, 3.0, 1.5,
     array['Dwelling house','Home occupation','Place of worship (consent)'],
     'Maximum 2 storeys. Secondary dwelling permitted by consent.'),

    ('Residential 2',
     'Medium-density residential. Townhouses, semi-detached.',
     60.0, 1.5, 10.0, 3.0, 2.0, 1.5,
     array['Dwelling house','Dwelling unit','Group housing','Place of instruction (consent)'],
     'Maximum 3 storeys.'),

    ('Residential 3',
     'High-density residential. Flats and apartment buildings.',
     70.0, 2.5, 16.0, 3.0, 2.0, 1.5,
     array['Dwelling unit','Flat','Residential building','Place of instruction (consent)'],
     'Height subject to street width and urban design overlay.'),

    ('Special Residential',
     'Large-stand low-density residential with strict environmental controls.',
     40.0, 0.8, 8.5, 5.0, 4.0, 2.0,
     array['Dwelling house','Home occupation (consent)'],
     'No secondary dwelling. Strict landscaping requirements.'),

    ('General Residential 1',
     'Mixed residential — houses and small blocks of flats.',
     60.0, 1.5, 11.0, 3.0, 2.0, 1.5,
     array['Dwelling house','Dwelling unit','Boarding house','Place of worship (consent)'],
     null),

    ('General Residential 2',
     'Medium-rise residential development.',
     70.0, 2.5, 18.0, 3.0, 2.0, 1.5,
     array['Dwelling unit','Residential building','Hotel (consent)'],
     null),

    ('General Residential 3',
     'High-rise residential and mixed-use residential.',
     75.0, 4.0, 30.0, 3.0, 2.0, 0.0,
     array['Dwelling unit','Residential building','Shop (consent)','Office (consent)'],
     'Zero side setbacks permitted where adjoining buildings share party wall.'),

    -- ── Business / Commercial ───────────────────────────────
    ('Business 1',
     'Central business district. High-intensity mixed use.',
     100.0, 6.0, null, 0.0, 0.0, 0.0,
     array['Office','Shop','Restaurant','Place of entertainment','Hotel','Residential building (consent)'],
     'No height limit — subject to urban design review. No setbacks in CBD.'),

    ('Business 2',
     'Neighbourhood commercial / suburban business.',
     80.0, 3.0, 18.0, 0.0, 2.0, 0.0,
     array['Office','Shop','Medical consulting rooms','Place of instruction'],
     null),

    ('Business 3',
     'Service industry and offices with limited retail.',
     70.0, 2.0, 14.0, 3.0, 2.0, 1.5,
     array['Office','Motor showroom','Warehouse (consent)'],
     null),

    ('Mixed Use 1',
     'Ground-floor retail with upper-floor residential or office.',
     80.0, 3.5, 18.0, 0.0, 2.0, 0.0,
     array['Shop','Office','Dwelling unit','Restaurant','Place of instruction'],
     'Retail mandatory on ground floor.'),

    -- ── Industrial ─────────────────────────────────────────
    ('Industrial 1',
     'General industrial. Manufacturing, warehousing.',
     70.0, 2.0, 20.0, 6.0, 3.0, 3.0,
     array['Industrial','Warehouse','Trade (consent)','Offices ancillary to industrial'],
     'No residential use permitted.'),

    ('Industrial 2',
     'Restricted industrial / business park.',
     65.0, 1.5, 15.0, 6.0, 3.0, 3.0,
     array['Light industrial','Office','Showroom'],
     'No heavy manufacturing or noxious uses.'),

    ('Special Industrial',
     'Hazardous or noxious industrial uses — requires environmental approval.',
     60.0, 1.0, 15.0, 10.0, 5.0, 5.0,
     array['Industrial (special consent)'],
     'Environmental Impact Assessment required.')

) as z (zone_code, description, max_coverage_pct, max_far, max_height_m,
        front_setback_m, rear_setback_m, side_setback_m, permitted_uses, notes)
on conflict (municipality_id, zone_code) do nothing;


-- ────────────────────────────────────────────────────────────
-- CAPE TOWN specific zones (City of Cape Town Municipal Planning By-Law 2015)
-- ────────────────────────────────────────────────────────────
with muni as (
    select id from public.municipalities where short_name = 'Cape Town' limit 1
)
insert into public.zoning_scheme_zones (
    municipality_id, zone_code, description,
    max_coverage_pct, max_far, max_height_m,
    front_setback_m, rear_setback_m, side_setback_m,
    permitted_uses, notes
)
select muni.id, z.zone_code, z.description,
       z.max_coverage_pct, z.max_far, z.max_height_m,
       z.front_setback_m, z.rear_setback_m, z.side_setback_m,
       z.permitted_uses, z.notes
from muni, (values
    ('Single Residential 1',
     'SR1 — Large plot, single dwelling. Cape Town By-Law.',
     50.0, 1.0, 8.5, 4.5, 3.0, 1.5,
     array['Dwelling house','Secondary dwelling unit','Home occupation'],
     'SDU limited to 60m² or 30% of main dwelling GFA.'),

    ('Single Residential 2',
     'SR2 — Standard plot, single dwelling with secondary dwelling.',
     60.0, 1.0, 8.5, 3.0, 2.0, 1.0,
     array['Dwelling house','Secondary dwelling unit','Home occupation'],
     null),

    ('General Residential 1',
     'GR1 — Low-density multi-unit. Cape Town By-Law.',
     60.0, 1.5, 10.0, 3.0, 2.0, 1.5,
     array['Dwelling house','Group housing','Boarding house'],
     null),

    ('General Residential 2',
     'GR2 — Medium-density. Cape Town By-Law.',
     70.0, 2.0, 14.0, 3.0, 2.0, 1.5,
     array['Dwelling unit','Residential building'],
     null),

    ('Local Business',
     'LB — Neighbourhood commercial. Cape Town By-Law.',
     80.0, 2.0, 10.0, 0.0, 2.0, 0.0,
     array['Shop','Restaurant','Office','Medical consulting rooms'],
     null),

    ('General Business 1',
     'GB1 — Suburban business. Cape Town By-Law.',
     80.0, 3.0, 18.0, 0.0, 2.0, 0.0,
     array['Office','Shop','Restaurant','Hotel (consent)'],
     null),

    ('General Business 4',
     'GB4 — Central city high-density. Cape Town By-Law.',
     100.0, null, null, 0.0, 0.0, 0.0,
     array['Office','Shop','Hotel','Residential building','Entertainment'],
     'No FAR or height cap — subject to Heritage and Urban Design overlay.')

) as z (zone_code, description, max_coverage_pct, max_far, max_height_m,
        front_setback_m, rear_setback_m, side_setback_m, permitted_uses, notes)
on conflict (municipality_id, zone_code) do nothing;
