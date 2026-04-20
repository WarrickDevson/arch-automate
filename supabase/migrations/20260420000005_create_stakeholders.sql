-- ============================================================
-- Migration: 20260420000005_create_stakeholders
-- People associated with a project (design team, client, council).
-- ============================================================

create type public.stakeholder_role as enum (
    'architect',
    'client',
    'structural_engineer',
    'electrical_engineer',
    'plumbing_engineer',
    'quantity_surveyor',
    'council_officer',
    'contractor',
    'other'
);

create table if not exists public.stakeholders (
    id              uuid        primary key default gen_random_uuid(),
    project_id      uuid        not null references public.projects (id) on delete cascade,
    tenant_id       uuid        not null references public.tenants (id) on delete cascade,

    name            text        not null,
    organisation    text        not null default '',
    email           text        not null default '',
    phone           text        not null default '',
    role            public.stakeholder_role not null default 'other',
    notes           text        not null default '',

    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

comment on table public.stakeholders is
    'Design-team members and other parties associated with a project.';

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists stakeholders_project_idx on public.stakeholders (project_id);
create index if not exists stakeholders_tenant_idx  on public.stakeholders (tenant_id);

-- ── updated_at trigger ───────────────────────────────────────
create trigger stakeholders_updated_at
    before update on public.stakeholders
    for each row execute procedure public.set_updated_at();

-- ── Row Level Security ────────────────────────────────────────
alter table public.stakeholders enable row level security;

create policy "Tenant members can view stakeholders"
    on public.stakeholders for select
    to authenticated
    using (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Tenant members can manage stakeholders"
    on public.stakeholders for insert
    to authenticated
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Tenant members can update stakeholders"
    on public.stakeholders for update
    to authenticated
    using (tenant_id = (select tenant_id from public.profiles where id = auth.uid()))
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Admins can delete stakeholders"
    on public.stakeholders for delete
    to authenticated
    using (
        (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
        and tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

create policy "Service role full access"
    on public.stakeholders for all
    to service_role using (true) with check (true);
