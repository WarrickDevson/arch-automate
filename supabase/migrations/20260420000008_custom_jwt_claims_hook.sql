-- ============================================================
-- Migration: 20260420000008_custom_jwt_claims_hook
-- Injects tenant_id and user_role into every Supabase JWT.
-- This runs server-side before the token is signed, so the
-- C# API can read them as standard JWT claims.
--
-- After running this migration, enable the hook in the dashboard:
--   Authentication → Hooks → Custom Access Token Hook
--   → Select function: public.custom_access_token_hook
-- ============================================================

create or replace function public.custom_access_token_hook(event jsonb)
returns jsonb
language plpgsql
stable
security definer
set search_path = public
as $$
declare
    claims    jsonb;
    tenant_id uuid;
    user_role text;
begin
    select p.tenant_id, p.role
    into   tenant_id, user_role
    from   public.profiles p
    where  p.id = (event ->> 'user_id')::uuid;

    claims := event -> 'claims';

    if tenant_id is not null then
        claims := jsonb_set(claims, '{tenant_id}', to_jsonb(tenant_id::text));
        claims := jsonb_set(claims, '{user_role}', to_jsonb(user_role));
    end if;

    return jsonb_set(event, '{claims}', claims);
end;
$$;

-- Grant supabase_auth_admin permission to call this function.
grant execute
    on function public.custom_access_token_hook
    to supabase_auth_admin;

-- Revoke from other roles to prevent abuse.
revoke execute
    on function public.custom_access_token_hook
    from authenticated, anon, public;

comment on function public.custom_access_token_hook is
    'Adds tenant_id and user_role claims to the Supabase JWT. '
    'Must be registered in: Authentication → Hooks → Custom Access Token Hook.';
