-- ============================================================
-- Migration: 20260420000001_create_tenants
-- Multi-tenant isolation table.
-- Each tenant maps to an organisation (practice / firm).
-- ============================================================

create extension if not exists "pgcrypto";

create table if not exists public.tenants (
    id          uuid primary key default gen_random_uuid(),
    name        text        not null,
    slug        text        not null unique,          -- URL-safe identifier
    plan        text        not null default 'free',  -- free | pro | enterprise
    is_active   boolean     not null default true,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

comment on table public.tenants is
    'Organisations / architectural practices. Every resource is scoped to a tenant.';

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists tenants_slug_idx on public.tenants (slug);

-- ── updated_at auto-maintenance ──────────────────────────────
create or replace function public.set_updated_at()
returns trigger language plpgsql as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

create trigger tenants_updated_at
    before update on public.tenants
    for each row execute procedure public.set_updated_at();

-- ── Row Level Security ────────────────────────────────────────
alter table public.tenants enable row level security;

-- Only service-role / backend can manage tenants directly.
-- Application users access tenant data through their profile join.
create policy "Service role full access"
    on public.tenants
    for all
    to service_role
    using (true)
    with check (true);
