-- Migration: 20260421000012_create_project_specs
-- Run this in the Supabase SQL Editor.

create table if not exists project_specs (
  id           uuid primary key default gen_random_uuid(),
  project_id   uuid not null references projects(id) on delete cascade,
  tenant_id    uuid not null,
  extracted_at timestamptz not null default now(),
  compiled_at  timestamptz,
  materials    jsonb not null default '[]',
  spec         jsonb not null default '{}',
  clause_count integer not null default 0,
  unique (project_id)
);

create index if not exists project_specs_tenant_id_idx on project_specs (tenant_id);

-- Row-Level Security
alter table project_specs enable row level security;

create policy "tenant_select_specs"
  on project_specs for select
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_insert_specs"
  on project_specs for insert
  with check (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_update_specs"
  on project_specs for update
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);
