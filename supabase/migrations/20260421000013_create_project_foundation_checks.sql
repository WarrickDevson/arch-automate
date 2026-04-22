-- Migration: 20260421000013_create_project_foundation_checks
-- Stores SANS 10400-H foundation compliance checks per project.
-- Run in Supabase SQL Editor.

create table if not exists project_foundation_checks (
  id                uuid primary key default gen_random_uuid(),
  project_id        uuid not null references projects(id) on delete cascade,
  tenant_id         uuid not null,
  checked_at        timestamptz not null default now(),
  input             jsonb not null default '{}',
  results           jsonb not null default '{}',
  overall_pass      boolean not null default false,
  number_of_storeys int not null default 1
);

create unique index if not exists project_foundation_checks_project_id_key
  on project_foundation_checks(project_id);

create index if not exists project_foundation_checks_tenant_id_idx
  on project_foundation_checks(tenant_id);

alter table project_foundation_checks enable row level security;

create policy "tenant_isolation" on project_foundation_checks
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_insert" on project_foundation_checks
  for insert with check (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_update" on project_foundation_checks
  for update using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);
