-- ============================================================
-- Migration: 20260420000006_create_rejection_comments
-- Council rejection / fix-list items parsed from PDF letters.
-- This is the "Fix-List" tracker.
-- ============================================================

create type public.rejection_category as enum (
    'zoning',
    'building_lines',
    'parking',
    'accessibility',
    'structural_documentation',
    'fire_compliance',
    'drainage',
    'other'
);

create type public.rejection_status as enum (
    'open',
    'in_progress',
    'resolved',
    'disputed'
);

create table if not exists public.rejection_comments (
    id                  uuid        primary key default gen_random_uuid(),
    project_id          uuid        not null references public.projects (id) on delete cascade,
    tenant_id           uuid        not null references public.tenants (id) on delete cascade,

    -- Source
    source_document     text        not null default '',   -- original PDF filename
    item_number         smallint,                          -- numbered item in letter
    clause_reference    text        not null default '',   -- e.g. "SANS 10400-O cl.4"

    -- Content
    comment_text        text        not null,              -- verbatim or parsed comment
    parsed_action       text        not null default '',   -- AI-suggested corrective action
    category            public.rejection_category not null default 'other',
    status              public.rejection_status   not null default 'open',

    -- Resolution tracking
    resolution_notes    text        not null default '',
    resolved_by         uuid        references auth.users (id),
    received_at         timestamptz not null default now(),
    resolved_at         timestamptz,

    -- Metadata
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now()
);

comment on table public.rejection_comments is
    'Council rejection letter items / fix-list. Parsed from PDF and tracked to resolution.';

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists rejections_project_idx   on public.rejection_comments (project_id);
create index if not exists rejections_tenant_idx    on public.rejection_comments (tenant_id);
create index if not exists rejections_status_idx    on public.rejection_comments (status);
create index if not exists rejections_category_idx  on public.rejection_comments (category);

-- ── updated_at trigger ───────────────────────────────────────
create trigger rejection_comments_updated_at
    before update on public.rejection_comments
    for each row execute procedure public.set_updated_at();

-- ── View: open fix list summary per project ───────────────────
create or replace view public.project_fix_list_summary as
select
    project_id,
    count(*)                                            as total_items,
    count(*) filter (where status = 'open')             as open_items,
    count(*) filter (where status = 'in_progress')      as in_progress_items,
    count(*) filter (where status = 'resolved')         as resolved_items,
    count(*) filter (where status = 'disputed')         as disputed_items,
    round(
        100.0 * count(*) filter (where status = 'resolved')
        / nullif(count(*), 0),
        1
    )                                                   as resolution_pct,
    max(updated_at)                                     as last_activity
from public.rejection_comments
group by project_id;

comment on view public.project_fix_list_summary is
    'Aggregated fix-list progress per project.';

-- ── Row Level Security ────────────────────────────────────────
alter table public.rejection_comments enable row level security;

create policy "Tenant members can view rejections"
    on public.rejection_comments for select
    to authenticated
    using (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Tenant members can insert rejections"
    on public.rejection_comments for insert
    to authenticated
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Tenant members can update rejections"
    on public.rejection_comments for update
    to authenticated
    using (tenant_id = (select tenant_id from public.profiles where id = auth.uid()))
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Admins can delete rejections"
    on public.rejection_comments for delete
    to authenticated
    using (
        (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
        and tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

create policy "Service role full access"
    on public.rejection_comments for all
    to service_role using (true) with check (true);
