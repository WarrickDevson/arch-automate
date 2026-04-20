-- ============================================================
-- Migration: 20260420000007_create_council_documents
-- Stores references to uploaded/generated council-pack files.
-- Actual binaries live in Supabase Storage bucket "council-docs".
-- ============================================================

create type public.document_type as enum (
    'rejection_letter',        -- incoming PDF from council
    'council_submission',      -- full submission pack
    'generated_dxf',           -- CAD table output from ArchAutomate.CAD
    'site_plan',
    'floor_plan',
    'elevation',
    'section',
    'specification',
    'other'
);

create table if not exists public.council_documents (
    id              uuid        primary key default gen_random_uuid(),
    project_id      uuid        not null references public.projects (id) on delete cascade,
    tenant_id       uuid        not null references public.tenants (id) on delete cascade,
    uploaded_by     uuid        not null references auth.users (id),

    document_type   public.document_type not null default 'other',
    display_name    text        not null,
    storage_path    text        not null,     -- Supabase Storage object path
    mime_type       text        not null default 'application/octet-stream',
    size_bytes      bigint      not null default 0,
    revision        text        not null default 'P1',
    is_current      boolean     not null default true,

    -- Link back to rejection that triggered a re-submission (optional)
    rejection_id    uuid        references public.rejection_comments (id),

    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

comment on table public.council_documents is
    'Metadata for documents uploaded to or generated for a project. Binaries in Storage.';

create index if not exists docs_project_idx on public.council_documents (project_id);
create index if not exists docs_tenant_idx  on public.council_documents (tenant_id);
create index if not exists docs_type_idx    on public.council_documents (document_type);

create trigger council_documents_updated_at
    before update on public.council_documents
    for each row execute procedure public.set_updated_at();

-- ── Row Level Security ────────────────────────────────────────
alter table public.council_documents enable row level security;

create policy "Tenant members can view documents"
    on public.council_documents for select
    to authenticated
    using (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Tenant members can upload documents"
    on public.council_documents for insert
    to authenticated
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Uploader or admin can update document"
    on public.council_documents for update
    to authenticated
    using (
        uploaded_by = auth.uid()
        or (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
    )
    with check (tenant_id = (select tenant_id from public.profiles where id = auth.uid()));

create policy "Admins can delete documents"
    on public.council_documents for delete
    to authenticated
    using (
        (select role from public.profiles where id = auth.uid()) in ('owner', 'admin')
        and tenant_id = (select tenant_id from public.profiles where id = auth.uid())
    );

create policy "Service role full access"
    on public.council_documents for all
    to service_role using (true) with check (true);

-- ── Storage bucket policy (apply via Supabase dashboard or CLI) ─
-- Bucket name: council-docs
-- Recommended policy: authenticated users can read/write objects
-- that start with their tenant_id prefix: {tenant_id}/{project_id}/{filename}
