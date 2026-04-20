-- ============================================================
-- Migration: 20260420000009_ifc_storage
-- Adds ifc_path column to projects + creates the ifc-models
-- Supabase Storage bucket with per-tenant RLS policies.
-- ============================================================

-- 1. Column on projects table
alter table public.projects
  add column if not exists ifc_path text;

-- 2. Create the private storage bucket
insert into storage.buckets (id, name, public)
values ('ifc-models', 'ifc-models', false)
on conflict (id) do nothing;

-- 3. Storage RLS policies
--    Path convention: {tenant_id}/{project_id}/model.ifc
--    Users may only access files whose first path segment matches their tenant_id.

-- Allow authenticated users to upload/replace their own tenant's IFC files
create policy "Tenant members can upload IFC" on storage.objects
  for insert to authenticated
  with check (
    bucket_id = 'ifc-models'
    and (storage.foldername(name))[1] = (
      select tenant_id::text from public.profiles where id = auth.uid()
    )
  );

-- Allow authenticated users to download their own tenant's IFC files
create policy "Tenant members can download IFC" on storage.objects
  for select to authenticated
  using (
    bucket_id = 'ifc-models'
    and (storage.foldername(name))[1] = (
      select tenant_id::text from public.profiles where id = auth.uid()
    )
  );

-- Allow authenticated users to replace (update) their own tenant's IFC files
create policy "Tenant members can update IFC" on storage.objects
  for update to authenticated
  using (
    bucket_id = 'ifc-models'
    and (storage.foldername(name))[1] = (
      select tenant_id::text from public.profiles where id = auth.uid()
    )
  );

-- Allow authenticated users to delete their own tenant's IFC files
create policy "Tenant members can delete IFC" on storage.objects
  for delete to authenticated
  using (
    bucket_id = 'ifc-models'
    and (storage.foldername(name))[1] = (
      select tenant_id::text from public.profiles where id = auth.uid()
    )
  );
