-- ──────────────────────────────────────────────────────────────────────────────
-- project_schedules: Stores IFC-extracted door and window schedule data per project.
-- Upserted by the backend each time the frontend extracts schedule data from an IFC file.
-- ──────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.project_schedules (
  id             uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id     uuid        NOT NULL REFERENCES public.projects(id) ON DELETE CASCADE,
  tenant_id      uuid        NOT NULL,
  extracted_at   timestamptz NOT NULL DEFAULT now(),
  door_schedule  jsonb       NOT NULL DEFAULT '[]'::jsonb,
  window_schedule jsonb      NOT NULL DEFAULT '[]'::jsonb,
  door_count     integer     NOT NULL DEFAULT 0,
  window_count   integer     NOT NULL DEFAULT 0,
  CONSTRAINT uq_project_schedules_project UNIQUE (project_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_project_schedules_tenant ON public.project_schedules (tenant_id);
CREATE INDEX IF NOT EXISTS idx_project_schedules_project ON public.project_schedules (project_id);

-- Row Level Security
ALTER TABLE public.project_schedules ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Tenant members can read own schedules"
  ON public.project_schedules FOR SELECT
  USING (tenant_id = (auth.jwt() -> 'app_metadata' ->> 'tenant_id')::uuid);

CREATE POLICY "Tenant members can upsert own schedules"
  ON public.project_schedules FOR INSERT
  WITH CHECK (tenant_id = (auth.jwt() -> 'app_metadata' ->> 'tenant_id')::uuid);

CREATE POLICY "Tenant members can update own schedules"
  ON public.project_schedules FOR UPDATE
  USING (tenant_id = (auth.jwt() -> 'app_metadata' ->> 'tenant_id')::uuid)
  WITH CHECK (tenant_id = (auth.jwt() -> 'app_metadata' ->> 'tenant_id')::uuid);
