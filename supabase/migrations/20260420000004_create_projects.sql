-- ============================================================
-- Migration: 20260420000004_create_projects
-- Core project record — the root aggregate for all work.
-- ============================================================

create type public.project_status as enum (
    'draft',
    'in_progress',
    'submitted_to_council',
    'approved',
    'rejected',
    'revised'
);

create table if not exists public.projects (
    id                  uuid        primary key default gen_random_uuid(),
    tenant_id           uuid        not null references public.tenants (id) on delete cascade,
    owner_user_id       uuid        not null references auth.users (id),

    -- Identification
    name                text        not null,
    description         text        not null default '',
    erf                 text        not null default '',
    municipality_id     smallint    references public.municipalities (id),
    municipality_name   text        not null default '',   -- denormalised for display
    address             text        not null default '',

    -- Site parameters (inputs to BIM engines)
    site_area_m2        numeric(12,3) not null default 0,
    proposed_gfa_m2     numeric(12,3) not null default 0,
    footprint_m2        numeric(12,3) not null default 0,
    number_of_storeys   smallint    not null default 1,
    building_height_m   numeric(7,3) not null default 0,
    front_setback_m     numeric(7,3) not null default 0,
    rear_setback_m      numeric(7,3) not null default 0,
    side_setback_m      numeric(7,3) not null default 0,
    zoning_scheme       text        not null default '',
    parking_bays        smallint    not null default 0,
    gla_for_parking_m2  numeric(12,3) not null default 0,

    -- Status
    status              public.project_status not null default 'draft',

    -- Council submission tracking
    submitted_at        timestamptz,
    decision_at         timestamptz,
    council_reference   text,

    -- Metadata
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now()
);

comment on table public.projects is
    'Root entity for an architectural submission project.';

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists projects_tenant_idx        on public.projects (tenant_id);
create index if not exists projects_owner_idx         on public.projects (owner_user_id);
create index if not exists projects_status_idx        on public.projects (status);
create index if not exists projects_municipality_idx  on public.projects (municipality_id);

-- ── updated_at trigger ───────────────────────────────────────
create trigger projects_updated_at
    before update on public.projects
    for each row execute procedure public.set_updated_at();

-- ── Row Level Security ────────────────────────────────────────
alter table public.projects enable row level security;

-- Members see all projects in their tenant.
create policy "Tenant members can view projects"
    on public.projects for select
    to authenticated
    using (
        tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

-- Members can create projects within their own tenant.
create policy "Tenant members can create projects"
    on public.projects for insert
    to authenticated
    with check (
        tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

-- Members can update projects they own; admins/owners can update any project.
create policy "Project owner or admin can update"
    on public.projects for update
    to authenticated
    using (
        owner_user_id = auth.uid()
        or (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
    )
    with check (
        tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

-- Only admins/owners can delete.
create policy "Admins can delete projects"
    on public.projects for delete
    to authenticated
    using (
        (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
        and tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

create policy "Service role full access"
    on public.projects for all
    to service_role using (true) with check (true);
