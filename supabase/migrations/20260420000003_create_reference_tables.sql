-- ============================================================
-- Migration: 20260420000003_create_reference_tables
-- Lookup / reference data for municipalities and zoning schemes.
-- Populated separately via seed files.
-- ============================================================

-- ── Provinces ────────────────────────────────────────────────
create table if not exists public.provinces (
    id      smallint    primary key generated always as identity,
    code    char(3)     not null unique,   -- e.g. GP, WC, KZN
    name    text        not null unique
);

comment on table public.provinces is 'South African provinces.';

-- ── Municipalities ───────────────────────────────────────────
create table if not exists public.municipalities (
    id              smallint    primary key generated always as identity,
    province_id     smallint    not null references public.provinces (id),
    name            text        not null,
    short_name      text        not null,   -- used in council doc references
    category        char(1)     not null,   -- A (metro), B (local), C (district)
    zoning_scheme   text,                   -- name of the applicable scheme
    created_at      timestamptz not null default now()
);

comment on table public.municipalities is
    'South African local & metro municipalities with their zoning scheme names.';

create index if not exists municipalities_province_idx on public.municipalities (province_id);
create unique index if not exists municipalities_name_idx on public.municipalities (name);

-- ── Zoning Scheme Zones ──────────────────────────────────────
create table if not exists public.zoning_scheme_zones (
    id                  smallint    primary key generated always as identity,
    municipality_id     smallint    not null references public.municipalities (id),
    zone_code           text        not null,    -- e.g. "Residential 1", "GR2"
    description         text        not null,
    max_coverage_pct    numeric(5,2),            -- site coverage %
    max_far             numeric(5,2),            -- floor area ratio
    max_height_m        numeric(6,2),
    front_setback_m     numeric(5,2),
    rear_setback_m      numeric(5,2),
    side_setback_m      numeric(5,2),
    permitted_uses      text[],                  -- array of use descriptions
    notes               text,
    created_at          timestamptz not null default now(),
    unique (municipality_id, zone_code)
);

comment on table public.zoning_scheme_zones is
    'Zoning parameters per zone per municipality. Source of truth for BIM engine overrides.';

create index if not exists zones_municipality_idx on public.zoning_scheme_zones (municipality_id);

-- ── RLS: reference tables are public read-only ────────────────
alter table public.provinces          enable row level security;
alter table public.municipalities     enable row level security;
alter table public.zoning_scheme_zones enable row level security;

create policy "Public read" on public.provinces
    for select to authenticated, anon using (true);

create policy "Public read" on public.municipalities
    for select to authenticated, anon using (true);

create policy "Public read" on public.zoning_scheme_zones
    for select to authenticated, anon using (true);

create policy "Service role full access" on public.provinces
    for all to service_role using (true) with check (true);

create policy "Service role full access" on public.municipalities
    for all to service_role using (true) with check (true);

create policy "Service role full access" on public.zoning_scheme_zones
    for all to service_role using (true) with check (true);
