-- ============================================================
-- Migration: 20260420000002_create_profiles
-- Extends auth.users with app-level data.
-- One profile per Supabase auth user.
-- ============================================================

create table if not exists public.profiles (
    id              uuid primary key references auth.users (id) on delete cascade,
    tenant_id       uuid        not null references public.tenants (id) on delete cascade,
    display_name    text        not null default '',
    email           text        not null,
    role            text        not null default 'member',  -- owner | admin | member
    avatar_url      text,
    is_active       boolean     not null default true,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

comment on table public.profiles is
    'Application-level user profile, linked 1-1 to auth.users and scoped to a tenant.';

comment on column public.profiles.role is
    'owner: full tenant admin; admin: manage projects & team; member: read/write own projects.';

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists profiles_tenant_id_idx on public.profiles (tenant_id);
create index if not exists profiles_email_idx     on public.profiles (email);

-- ── updated_at trigger ───────────────────────────────────────
create trigger profiles_updated_at
    before update on public.profiles
    for each row execute procedure public.set_updated_at();

-- Auto-create profile on new auth user sign-up ONLY when a tenant_id is
-- present in app_meta_data (i.e. the user was invited/provisioned).
-- Self-signups have no tenant yet — the onboarding flow handles that.
create or replace function public.handle_new_user()
returns trigger language plpgsql security definer as $$
declare
  v_tenant_id uuid;
begin
  v_tenant_id := (new.raw_app_meta_data ->> 'tenant_id')::uuid;

  if v_tenant_id is null then
    return new;
  end if;

  insert into public.profiles (id, tenant_id, display_name, email, role)
  values (
    new.id,
    v_tenant_id,
    coalesce(new.raw_user_meta_data ->> 'full_name', new.email),
    new.email,
    coalesce(new.raw_app_meta_data ->> 'role', 'member')
  );
  return new;
end;
$$;

create trigger on_auth_user_created
    after insert on auth.users
    for each row execute procedure public.handle_new_user();

-- ── Row Level Security ────────────────────────────────────────
alter table public.profiles enable row level security;

-- Users can read/update their own profile.
create policy "Users can view own profile"
    on public.profiles for select
    to authenticated
    using (auth.uid() = id);

create policy "Users can update own profile"
    on public.profiles for update
    to authenticated
    using (auth.uid() = id)
    with check (auth.uid() = id);

-- Tenant admins/owners can view all profiles in their tenant.
create policy "Tenant admins can view team profiles"
    on public.profiles for select
    to authenticated
    using (
        tenant_id = (
            select tenant_id from public.profiles
            where id = auth.uid()
        )
        and (
            select role from public.profiles where id = auth.uid()
        ) in ('owner', 'admin')
    );

-- Service role bypass for backend API.
create policy "Service role full access"
    on public.profiles for all
    to service_role
    using (true) with check (true);
