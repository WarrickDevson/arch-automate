-- Migration: 20260421000011_create_project_tallies
-- Run this in the Supabase SQL editor.
-- Creates the project_tallies table with tenant-scoped RLS policies.

create table if not exists public.project_tallies (
  id              uuid primary key default gen_random_uuid(),
  project_id      uuid not null references public.projects(id) on delete cascade,
  tenant_id       uuid not null,
  extracted_at    timestamptz not null default now(),
  tally           jsonb not null default '[]'::jsonb,
  lighting_count  int not null default 0,
  electrical_count int not null default 0,
  sanitary_count  int not null default 0,
  hvac_count      int not null default 0,
  fire_count      int not null default 0,
  other_count     int not null default 0,
  total_count     int not null default 0,
  constraint project_tallies_project_id_unique unique (project_id)
);

-- Indexes
create index if not exists idx_project_tallies_tenant on public.project_tallies(tenant_id);

-- RLS
alter table public.project_tallies enable row level security;

create policy "Tenants can read own tallies"
  on public.project_tallies for select
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "Tenants can insert own tallies"
  on public.project_tallies for insert
  with check (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "Tenants can update own tallies"
  on public.project_tallies for update
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);
