-- Migration: 20260421000014_create_project_roof_checks
-- Stores SANS 10400-L / SANS 10400-K roof callout checklist per project.
-- Run in Supabase SQL Editor.

create table if not exists project_roof_checks (
  id           uuid primary key default gen_random_uuid(),
  project_id   uuid not null references projects(id) on delete cascade,
  tenant_id    uuid not null,
  checked_at   timestamptz not null default now(),
  input        jsonb not null default '{}',
  results      jsonb not null default '{}',
  overall_pass boolean not null default false,
  roof_type    text not null default ''
);

create unique index if not exists project_roof_checks_project_id_key
  on project_roof_checks(project_id);

create index if not exists project_roof_checks_tenant_id_idx
  on project_roof_checks(tenant_id);

alter table project_roof_checks enable row level security;

create policy "tenant_isolation" on project_roof_checks
  using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_insert" on project_roof_checks
  for insert with check (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);

create policy "tenant_update" on project_roof_checks
  for update using (tenant_id = (auth.jwt() ->> 'tenant_id')::uuid);
