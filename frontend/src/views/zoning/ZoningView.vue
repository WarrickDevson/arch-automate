<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  Map,
  Info,
  RefreshCw,
  Printer,
  FolderOpen,
  Search,
  Building2,
  ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { zoningService } from '@/services/zoningService'
import { toast } from 'vue-sonner'

const props = defineProps({
  projectId: { type: String, default: null },
})

const router = useRouter()
const projectsStore = useProjectsStore()
const uiStore = useUiStore()

// Project picker
const pickerSearch = ref('')
const pickerProjects = computed(() => {
  const q = pickerSearch.value.trim().toLowerCase()
  if (!q) return projectsStore.projects
  return projectsStore.projects.filter(
    (p) =>
      p.name.toLowerCase().includes(q) ||
      p.municipality.toLowerCase().includes(q),
  )
})

// ── State ────────────────────────────────────────────────────────────────────
const project = ref(null)
const isLoadingProject = ref(false)

const allSchemes = ref([])   // [{ name, description, maxCoveragePercent, maxFar, … }]
const isLoadingSchemes = ref(false)

// Active scheme override (user can switch to a different zone to compare)
const schemeOverride = ref(null)

const params = ref({
  footprintM2: 0,
  proposedGfaM2: 0,
  buildingHeightM: 0,
  frontSetbackM: 0,
  rearSetbackM: 0,
  sideSetbackM: 0,
  parkingBaysProvided: 0,
  glaForParkingM2: 0,
  numberOfStoreys: 1,
})

const complianceResult = ref(null)
const isEvaluating = ref(false)

// ── Derived ──────────────────────────────────────────────────────────────────
const siteAreaM2 = computed(() => project.value?.siteAreaM2 ?? 0)
const effectiveScheme = computed(
  () => schemeOverride.value ?? project.value?.zoningScheme ?? null,
)
const schemeInfo = computed(
  () => allSchemes.value.find((s) => s.name === effectiveScheme.value) ?? null,
)

// Live calculations — no API needed
const liveCoveragePercent = computed(() =>
  siteAreaM2.value > 0
    ? parseFloat(((params.value.footprintM2 / siteAreaM2.value) * 100).toFixed(2))
    : 0,
)
const liveFar = computed(() =>
  siteAreaM2.value > 0
    ? parseFloat((params.value.proposedGfaM2 / siteAreaM2.value).toFixed(3))
    : 0,
)

// Gauge status helpers
function coverageStatus() {
  const max = schemeInfo.value?.maxCoveragePercent
  if (max == null || siteAreaM2.value === 0) return 'neutral'
  return liveCoveragePercent.value > max ? 'fail' : 'pass'
}
function farStatus() {
  const max = schemeInfo.value?.maxFar
  if (max == null || siteAreaM2.value === 0) return 'neutral'
  return liveFar.value > max ? 'fail' : 'pass'
}
function heightStatus() {
  const max = schemeInfo.value?.maxHeightM
  if (max == null) return 'neutral'
  return params.value.buildingHeightM > max ? 'fail' : 'pass'
}

const statusBarClass = {
  pass: 'bg-emerald-500',
  fail: 'bg-rose-500',
  neutral: 'bg-slate-300',
}
const statusTextClass = {
  pass: 'text-emerald-600',
  fail: 'text-rose-600',
  neutral: 'text-slate-500',
}
const statusBadgeClass = {
  pass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  fail: 'bg-rose-50 text-rose-700 border-rose-200',
  neutral: 'bg-slate-50 text-slate-500 border-slate-200',
}

// Setback helpers
function setbackStatus(provided, required) {
  if (!required && required !== 0) return 'neutral'
  return provided >= required ? 'pass' : 'fail'
}

// Compliance score from API result
const apiPassedChecks = computed(() =>
  (complianceResult.value?.checks ?? []).filter((c) => c.passed).length,
)
const apiTotalChecks = computed(() => (complianceResult.value?.checks ?? []).length)
const apiScore = computed(() =>
  apiTotalChecks.value > 0
    ? Math.round((apiPassedChecks.value / apiTotalChecks.value) * 100)
    : null,
)
const apiScoreClass = computed(() => {
  if (apiScore.value === null) return 'text-slate-400'
  if (apiScore.value >= 80) return 'text-emerald-600'
  if (apiScore.value >= 60) return 'text-amber-500'
  return 'text-rose-600'
})

const severityClass = {
  Advisory: 'bg-blue-50 text-blue-700 border-blue-200',
  Warning: 'bg-amber-50 text-amber-700 border-amber-200',
  NonCompliant: 'bg-rose-50 text-rose-700 border-rose-200',
  0: 'bg-blue-50 text-blue-700 border-blue-200',
  1: 'bg-amber-50 text-amber-700 border-amber-200',
  2: 'bg-rose-50 text-rose-700 border-rose-200',
}

// ── Gauge bar width (capped at 100%) ────────────────────────────────────────
function gaugeWidth(value, max) {
  if (!max || max === 0) return 0
  return Math.min((value / max) * 100, 100)
}

// ── Data loading ─────────────────────────────────────────────────────────────
onMounted(async () => {
  isLoadingSchemes.value = true
  try {
    allSchemes.value = await zoningService.getSchemes()
  } catch {
    // non-critical — scheme lookup just won't show permitted values
  } finally {
    isLoadingSchemes.value = false
  }

  if (props.projectId) {
    await loadProject(props.projectId)
  } else {
    projectsStore.fetchProjects()
  }
})

watch(() => props.projectId, (id) => {
  if (id) loadProject(id)
})

async function loadProject(id) {
  isLoadingProject.value = true
  try {
    project.value = await projectsStore.fetchProject(id)
    seedParamsFromProject(project.value)
    // Try to restore cached result from workbench session
    try {
      const cached = sessionStorage.getItem(`compliance_${id}`)
      if (cached) complianceResult.value = JSON.parse(cached)
    } catch { /* noop */ }
  } catch (err) {
    toast.error('Could not load project', { description: err.message })
  } finally {
    isLoadingProject.value = false
  }
}

function seedParamsFromProject(proj) {
  if (!proj) return
  params.value.footprintM2 = proj.footprintM2 ?? 0
  params.value.proposedGfaM2 = proj.proposedGfaM2 ?? 0
  params.value.buildingHeightM = proj.buildingHeightM ?? 0
  params.value.numberOfStoreys = proj.numberOfStoreys ?? 1
  params.value.frontSetbackM = proj.frontSetbackM ?? 0
  params.value.rearSetbackM = proj.rearSetbackM ?? 0
  params.value.sideSetbackM = proj.sideSetbackM ?? 0
  params.value.parkingBaysProvided = proj.parkingBays ?? 0
  params.value.glaForParkingM2 = proj.glaForParkingM2 ?? 0
}

// ── Actions ──────────────────────────────────────────────────────────────────
async function runFullCompliance() {
  if (!project.value) {
    toast.warning('Select a project first')
    return
  }
  if (!effectiveScheme.value) {
    toast.warning('No zoning scheme set on this project')
    return
  }
  isEvaluating.value = true
  complianceResult.value = null
  try {
    const result = await zoningService.evaluate({
      ...params.value,
      siteAreaM2: siteAreaM2.value,
      zoningScheme: effectiveScheme.value,
    })
    complianceResult.value = result
    // Cache for workbench cross-navigation
    try {
      sessionStorage.setItem(`compliance_${project.value.id}`, JSON.stringify(result))
    } catch { /* quota */ }
    const passed = result.checks?.filter((c) => c.passed).length ?? 0
    toast.success('Zoning compliance evaluated', {
      description: `${passed}/${result.checks?.length ?? 0} checks passed`,
    })
  } catch (err) {
    toast.error('Evaluation failed', { description: err.message })
  } finally {
    isEvaluating.value = false
  }
}

</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col print:h-auto print:block">

    <!-- No project: inline project picker -->
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Zoning</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to run zoning compliance calculations.</p>
          </div>
          <Button
            class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold"
            @click="uiStore.openCreateProjectSheet()"
          >
            <ArrowRight class="h-3.5 w-3.5" /> New Project
          </Button>
        </div>

        <div class="relative shrink-0">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
          <Input v-model="pickerSearch" class="pl-9 h-9 text-sm" placeholder="Search by name or municipality…" />
        </div>

        <div class="flex-1 overflow-y-auto">
          <div v-if="projectsStore.loading" class="space-y-2">
            <div v-for="n in 5" :key="n" class="h-14 bg-slate-100 rounded-lg animate-pulse" />
          </div>

          <div
            v-else-if="pickerProjects.length === 0"
            class="flex flex-col items-center justify-center h-full py-16 text-center"
          >
            <FolderOpen class="h-10 w-10 text-slate-200 mb-3" />
            <p class="text-sm font-medium text-slate-400">
              {{ pickerSearch ? 'No projects match your search.' : 'No projects yet — create your first one.' }}
            </p>
            <Button
              class="mt-4 bg-blue-600 hover:bg-blue-700 uppercase text-xs font-bold gap-2"
              @click="uiStore.openCreateProjectSheet()"
            >
              New Project
            </Button>
          </div>

          <ul v-else class="space-y-1.5">
            <li
              v-for="p in pickerProjects"
              :key="p.id"
              class="flex items-center gap-4 px-4 py-3 rounded-lg border border-slate-100 bg-slate-50/60 hover:bg-white hover:border-blue-200 hover:shadow-sm cursor-pointer transition-all group"
              @click="router.push({ name: 'zoning', params: { projectId: p.id } })"
            >
              <div class="flex-shrink-0 h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center">
                <Building2 class="h-4 w-4 text-primary" />
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-slate-800 truncate">{{ p.name }}</p>
                <p class="text-[11px] text-slate-400">{{ p.municipality }} &middot; {{ p.zoningScheme }}</p>
              </div>
              <div class="flex-shrink-0 flex items-center gap-2">
                <span class="text-[10px] font-bold text-slate-400 bg-slate-100 px-2 py-0.5 rounded-full uppercase">
                  {{ p.status === 'SubmittedToCouncil' ? 'In Council' : p.status }}
                </span>
                <ArrowRight class="h-4 w-4 text-slate-300 group-hover:text-blue-500 transition-colors" />
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <!-- Has project -->
    <main v-else class="relative flex-1 min-h-0 overflow-auto">

      <!-- Loading overlay (kept out of document flow to prevent layout shift) -->
      <div class="pointer-events-none absolute inset-x-0 top-0 z-10 flex justify-center print:hidden">
        <div
          class="mt-1 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 shadow-sm transition-all duration-200"
          :class="isLoadingProject ? 'translate-y-0 opacity-100' : '-translate-y-1 opacity-0'"
          role="status"
          aria-live="polite"
        >
          <Loader2 class="h-3.5 w-3.5 animate-spin text-blue-400" />
          <span class="text-xs text-slate-500">Loading project...</span>
        </div>
      </div>

      <div v-if="project" class="space-y-4 pb-6">

        <!-- Print-only project header -->
        <div class="hidden print:block mb-6">
        <h2 class="text-2xl font-bold text-slate-900">Zoning Compliance Report</h2>
        <p class="text-sm text-slate-600 mt-1">
          Project: <strong>{{ project.name }}</strong> &middot;
          ERF: {{ project.erf || '—' }} &middot;
          Scheme: {{ effectiveScheme || '—' }} &middot;
          Site Area: {{ siteAreaM2.toLocaleString() }} m²
        </p>
        <p class="text-xs text-slate-400 mt-0.5">Generated {{ new Date().toLocaleDateString('en-ZA') }}</p>
        <hr class="mt-4" />
      </div>

      <!-- Action cards -->
      <!-- ── Site context strip ─────────────────────────────────────────────── -->
      <div class="bg-white border border-slate-200 rounded-xl p-4 flex flex-wrap items-center gap-x-8 gap-y-3 print:border-0 print:p-0">
        <div>
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Site Area</p>
          <p class="text-lg font-mono font-bold text-slate-900 mt-0.5">
            {{ siteAreaM2 > 0 ? siteAreaM2.toLocaleString() + ' m²' : '—' }}
          </p>
        </div>
        <div>
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">ERF</p>
          <p class="text-lg font-mono font-bold text-slate-900 mt-0.5">{{ project.erf || '—' }}</p>
        </div>
        <div>
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Municipality</p>
          <p class="text-lg font-bold text-slate-900 mt-0.5 text-sm">{{ project.municipality || '—' }}</p>
        </div>
        <div class="flex-1 min-w-48">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Zoning Scheme</p>
          <Select
            :model-value="effectiveScheme ?? undefined"
            class="print:hidden"
            @update:model-value="(v) => { schemeOverride = v; complianceResult = null }"
          >
            <SelectTrigger class="h-8 text-sm w-full max-w-xs">
              <SelectValue :placeholder="project.zoningScheme || 'Select scheme…'" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem
                v-for="s in allSchemes"
                :key="s.name"
                :value="s.name"
              >
                {{ s.name }}
              </SelectItem>
            </SelectContent>
          </Select>
          <p class="hidden print:block text-sm font-bold text-slate-900">{{ effectiveScheme || '—' }}</p>
        </div>
        <div
          v-if="schemeInfo"
          class="flex items-center gap-1 text-xs text-slate-500 italic print:hidden"
        >
          <Info class="h-3.5 w-3.5 shrink-0" />
          {{ schemeInfo.description }}
        </div>
        <!-- Action buttons — far right -->
        <div class="ml-auto flex items-center gap-2 shrink-0 print:hidden">
          <Button
            variant="outline"
            class="h-9 gap-2 px-3 text-[10px] font-bold uppercase border-slate-200 bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-700"
            @click="window.print()"
          >
            <Printer class="h-3.5 w-3.5" />
            Print Report
          </Button>
          <Button
            class="h-9 gap-2 bg-slate-950 px-3 text-[10px] font-bold uppercase text-white hover:bg-slate-900"
            :disabled="isEvaluating || !effectiveScheme"
            @click="runFullCompliance"
          >
            <Loader2 v-if="isEvaluating" class="h-3.5 w-3.5 animate-spin" />
            <RefreshCw v-else class="h-3.5 w-3.5" />
            {{ isEvaluating ? 'Evaluating…' : 'Run Compliance Check' }}
          </Button>
        </div>
      </div>

      <!-- ── Warning if no site area ─────────────────────────────────────────── -->
      <div
        v-if="siteAreaM2 === 0"
        class="flex items-start gap-2 rounded-xl bg-amber-50 border border-amber-200 px-4 py-3 text-sm text-amber-700 print:hidden"
      >
        <AlertTriangle class="h-4 w-4 mt-0.5 shrink-0" />
        <span>Site area is not set on this project. Live calculations cannot be performed. Update the project's site area in the Command Center.</span>
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-[380px_1fr] gap-6">
        <!-- ── LEFT: Input parameters ──────────────────────────────────────────── -->
        <div class="space-y-5 print:hidden">
          <div class="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
            <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">Building Measurements</p>

            <div class="grid grid-cols-2 gap-3">
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Footprint (m²)</Label>
                <Input
                  v-model.number="params.footprintM2"
                  type="number"
                  min="0"
                  class="h-9 text-sm"
                  placeholder="0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">GFA (m²)</Label>
                <Input
                  v-model.number="params.proposedGfaM2"
                  type="number"
                  min="0"
                  class="h-9 text-sm"
                  placeholder="0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Height (m)</Label>
                <Input
                  v-model.number="params.buildingHeightM"
                  type="number"
                  min="0"
                  step="0.1"
                  class="h-9 text-sm"
                  placeholder="0.0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Storeys</Label>
                <Input
                  v-model.number="params.numberOfStoreys"
                  type="number"
                  min="1"
                  class="h-9 text-sm"
                  placeholder="1"
                />
              </div>
            </div>

            <div class="h-px bg-slate-100" />

            <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">Building Lines (setbacks)</p>

            <div class="grid grid-cols-3 gap-3">
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Front (m)</Label>
                <Input
                  v-model.number="params.frontSetbackM"
                  type="number"
                  min="0"
                  step="0.1"
                  class="h-9 text-sm"
                  placeholder="0.0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Rear (m)</Label>
                <Input
                  v-model.number="params.rearSetbackM"
                  type="number"
                  min="0"
                  step="0.1"
                  class="h-9 text-sm"
                  placeholder="0.0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Side (m)</Label>
                <Input
                  v-model.number="params.sideSetbackM"
                  type="number"
                  min="0"
                  step="0.1"
                  class="h-9 text-sm"
                  placeholder="0.0"
                />
              </div>
            </div>

            <div class="h-px bg-slate-100" />

            <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">Parking</p>

            <div class="grid grid-cols-2 gap-3">
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">Bays Provided</Label>
                <Input
                  v-model.number="params.parkingBaysProvided"
                  type="number"
                  min="0"
                  class="h-9 text-sm"
                  placeholder="0"
                />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase text-slate-500">GLA for Parking (m²)</Label>
                <Input
                  v-model.number="params.glaForParkingM2"
                  type="number"
                  min="0"
                  class="h-9 text-sm"
                  placeholder="0"
                />
              </div>
            </div>
          </div>

          <!-- Scheme limits reference card -->
          <div
            v-if="schemeInfo"
            class="bg-white border border-slate-200 rounded-xl p-5 space-y-3"
          >
            <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">
              Permitted Limits — {{ schemeInfo.name }}
            </p>
            <div class="grid grid-cols-2 gap-x-6 gap-y-1.5 text-xs">
              <div class="flex justify-between">
                <span class="text-slate-500">Max Coverage</span>
                <span class="font-mono font-bold text-slate-800">{{ schemeInfo.maxCoveragePercent }}%</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Max FAR</span>
                <span class="font-mono font-bold text-slate-800">{{ schemeInfo.maxFar }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Max Height</span>
                <span class="font-mono font-bold text-slate-800">{{ schemeInfo.maxHeightM }} m</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Front Setback</span>
                <span class="font-mono font-bold text-slate-800">≥ {{ schemeInfo.frontSetbackM }} m</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Rear Setback</span>
                <span class="font-mono font-bold text-slate-800">≥ {{ schemeInfo.rearSetbackM }} m</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Side Setback</span>
                <span class="font-mono font-bold text-slate-800">≥ {{ schemeInfo.sideSetbackM }} m</span>
              </div>
            </div>
            <p class="text-[10px] text-slate-400 italic leading-relaxed pt-1">
              Indicative limits. Verify against the applicable municipal zoning scheme by-law before submitting.
            </p>
          </div>
        </div>

        <!-- ── RIGHT: Live gauges + compliance results ──────────────────────────── -->
        <div class="space-y-5">

          <!-- Live gauge cards -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">

            <!-- Coverage gauge -->
            <div
              class="bg-white border rounded-xl p-5 space-y-3 print:border-slate-200"
              :class="siteAreaM2 > 0 ? (coverageStatus() === 'fail' ? 'border-rose-200' : 'border-emerald-200') : 'border-slate-200'"
            >
              <div class="flex items-center justify-between">
                <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">Site Coverage</p>
                <span
                  v-if="schemeInfo && siteAreaM2 > 0"
                  class="text-[9px] font-bold uppercase border rounded-full px-2 py-0.5"
                  :class="statusBadgeClass[coverageStatus()]"
                >
                  {{ coverageStatus() === 'pass' ? '✓ Pass' : '✗ Fail' }}
                </span>
              </div>
              <p
                class="text-4xl font-mono font-black leading-none"
                :class="siteAreaM2 > 0 ? statusTextClass[coverageStatus()] : 'text-slate-300'"
              >
                {{ siteAreaM2 > 0 ? liveCoveragePercent + '%' : '—' }}
              </p>
              <div v-if="siteAreaM2 > 0">
                <!-- Gauge bar -->
                <div class="relative w-full h-2.5 bg-slate-100 rounded-full overflow-visible">
                  <div
                    class="h-full rounded-full transition-all duration-300"
                    :class="statusBarClass[coverageStatus()]"
                    :style="{ width: gaugeWidth(liveCoveragePercent, schemeInfo?.maxCoveragePercent ?? 100) + '%' }"
                  />
                  <!-- Max marker -->
                  <div
                    v-if="schemeInfo"
                    class="absolute top-0 h-2.5 w-0.5 bg-slate-400 rounded-full"
                    :style="{ left: Math.min(schemeInfo.maxCoveragePercent, 100) + '%' }"
                    :title="'Max: ' + schemeInfo.maxCoveragePercent + '%'"
                  />
                </div>
                <div class="flex justify-between mt-1">
                  <span class="text-[9px] text-slate-400">0%</span>
                  <span v-if="schemeInfo" class="text-[9px] text-slate-500 font-bold">
                    Max {{ schemeInfo.maxCoveragePercent }}%
                  </span>
                </div>
              </div>
              <div v-if="siteAreaM2 > 0 && params.footprintM2 > 0" class="text-[10px] text-slate-400">
                {{ params.footprintM2.toLocaleString() }} m² / {{ siteAreaM2.toLocaleString() }} m²
              </div>
            </div>

            <!-- FAR gauge -->
            <div
              class="bg-white border rounded-xl p-5 space-y-3 print:border-slate-200"
              :class="siteAreaM2 > 0 ? (farStatus() === 'fail' ? 'border-rose-200' : 'border-emerald-200') : 'border-slate-200'"
            >
              <div class="flex items-center justify-between">
                <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">Floor Area Ratio</p>
                <span
                  v-if="schemeInfo && siteAreaM2 > 0"
                  class="text-[9px] font-bold uppercase border rounded-full px-2 py-0.5"
                  :class="statusBadgeClass[farStatus()]"
                >
                  {{ farStatus() === 'pass' ? '✓ Pass' : '✗ Fail' }}
                </span>
              </div>
              <p
                class="text-4xl font-mono font-black leading-none"
                :class="siteAreaM2 > 0 ? statusTextClass[farStatus()] : 'text-slate-300'"
              >
                {{ siteAreaM2 > 0 ? liveFar : '—' }}
              </p>
              <div v-if="siteAreaM2 > 0">
                <div class="relative w-full h-2.5 bg-slate-100 rounded-full overflow-visible">
                  <div
                    class="h-full rounded-full transition-all duration-300"
                    :class="statusBarClass[farStatus()]"
                    :style="{ width: gaugeWidth(liveFar, schemeInfo?.maxFar ?? 10) + '%' }"
                  />
                  <div
                    v-if="schemeInfo"
                    class="absolute top-0 h-2.5 w-0.5 bg-slate-400 rounded-full"
                    :style="{ left: Math.min((schemeInfo.maxFar / (schemeInfo.maxFar + 1)) * 100, 100) + '%' }"
                  />
                </div>
                <div class="flex justify-between mt-1">
                  <span class="text-[9px] text-slate-400">0.0</span>
                  <span v-if="schemeInfo" class="text-[9px] text-slate-500 font-bold">
                    Max {{ schemeInfo.maxFar }}
                  </span>
                </div>
              </div>
              <div v-if="siteAreaM2 > 0 && params.proposedGfaM2 > 0" class="text-[10px] text-slate-400">
                {{ params.proposedGfaM2.toLocaleString() }} m² GFA / {{ siteAreaM2.toLocaleString() }} m² site
              </div>
            </div>

            <!-- Height gauge -->
            <div
              class="bg-white border rounded-xl p-5 space-y-3 print:border-slate-200"
              :class="schemeInfo && params.buildingHeightM > 0 ? (heightStatus() === 'fail' ? 'border-rose-200' : 'border-emerald-200') : 'border-slate-200'"
            >
              <div class="flex items-center justify-between">
                <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">Building Height</p>
                <span
                  v-if="schemeInfo && params.buildingHeightM > 0"
                  class="text-[9px] font-bold uppercase border rounded-full px-2 py-0.5"
                  :class="statusBadgeClass[heightStatus()]"
                >
                  {{ heightStatus() === 'pass' ? '✓ Pass' : '✗ Fail' }}
                </span>
              </div>
              <p
                class="text-4xl font-mono font-black leading-none"
                :class="params.buildingHeightM > 0 ? statusTextClass[heightStatus()] : 'text-slate-300'"
              >
                {{ params.buildingHeightM > 0 ? params.buildingHeightM + ' m' : '—' }}
              </p>
              <div v-if="schemeInfo && params.buildingHeightM > 0">
                <div class="relative w-full h-2.5 bg-slate-100 rounded-full overflow-visible">
                  <div
                    class="h-full rounded-full transition-all duration-300"
                    :class="statusBarClass[heightStatus()]"
                    :style="{ width: gaugeWidth(params.buildingHeightM, schemeInfo.maxHeightM) + '%' }"
                  />
                  <div
                    class="absolute top-0 h-2.5 w-0.5 bg-slate-400 rounded-full"
                    style="left: 100%"
                  />
                </div>
                <div class="flex justify-between mt-1">
                  <span class="text-[9px] text-slate-400">0 m</span>
                  <span class="text-[9px] text-slate-500 font-bold">Max {{ schemeInfo.maxHeightM }} m</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Setbacks comparison -->
          <div
            v-if="schemeInfo"
            class="bg-white border border-slate-200 rounded-xl p-5 print:border-slate-200"
          >
            <p class="text-xs font-bold text-slate-500 uppercase tracking-widest mb-4">Building Line Setbacks</p>
            <div class="grid grid-cols-3 gap-4">
              <div
                v-for="({ label, provided, required }) in [
                  { label: 'Front', provided: params.frontSetbackM, required: schemeInfo.frontSetbackM },
                  { label: 'Rear',  provided: params.rearSetbackM,  required: schemeInfo.rearSetbackM  },
                  { label: 'Side',  provided: params.sideSetbackM,  required: schemeInfo.sideSetbackM  },
                ]"
                :key="label"
                class="rounded-lg border p-3 text-center"
                :class="setbackStatus(provided, required) === 'pass' ? 'border-emerald-200 bg-emerald-50' : setbackStatus(provided, required) === 'fail' ? 'border-rose-200 bg-rose-50' : 'border-slate-100 bg-slate-50'"
              >
                <p class="text-[9px] font-bold uppercase tracking-widest mb-1"
                  :class="setbackStatus(provided, required) === 'pass' ? 'text-emerald-600' : setbackStatus(provided, required) === 'fail' ? 'text-rose-600' : 'text-slate-400'"
                >{{ label }}</p>
                <p class="text-xl font-mono font-bold"
                  :class="setbackStatus(provided, required) === 'pass' ? 'text-emerald-700' : setbackStatus(provided, required) === 'fail' ? 'text-rose-700' : 'text-slate-600'"
                >{{ provided }} m</p>
                <p class="text-[10px] mt-0.5"
                  :class="setbackStatus(provided, required) === 'pass' ? 'text-emerald-500' : setbackStatus(provided, required) === 'fail' ? 'text-rose-500' : 'text-slate-400'"
                >≥ {{ required }} m required</p>
              </div>
            </div>
          </div>

          <!-- ── Full compliance result (from API) ───────────────────────────── -->
          <div v-if="complianceResult" class="bg-white border border-slate-200 rounded-xl p-5 space-y-4 print:border-slate-200">
            <div class="flex items-center justify-between">
              <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">Full Compliance Evaluation</p>
              <div class="flex items-center gap-2">
                <span class="text-[10px] text-slate-500">Score</span>
                <span
                  class="text-base font-mono font-black"
                  :class="apiScoreClass"
                >{{ apiScore ?? '—' }}%</span>
                <span class="text-[10px] text-slate-400">{{ apiPassedChecks }}/{{ apiTotalChecks }} checks</span>
              </div>
            </div>

            <!-- Checks table -->
            <div class="space-y-1.5">
              <div
                v-for="check in complianceResult.checks"
                :key="check.rule"
                class="flex items-center justify-between gap-3 px-3 py-2.5 rounded-lg border"
                :class="check.passed ? 'border-emerald-100 bg-emerald-50/40 dark:border-emerald-900/50 dark:bg-emerald-950/30' : 'border-rose-100 bg-rose-50/40 dark:border-rose-900/50 dark:bg-rose-950/30'"
              >
                <div class="flex items-center gap-3 min-w-0">
                  <CheckCircle2 v-if="check.passed" class="h-4 w-4 text-emerald-500 shrink-0" />
                  <XCircle v-else class="h-4 w-4 text-rose-500 shrink-0" />
                  <div class="flex-1 min-w-0">
                    <p class="text-xs font-bold text-slate-700 dark:text-slate-200">{{ check.description }}</p>
                  </div>
                </div>
                <div class="text-right shrink-0">
                  <p class="text-xs font-mono text-slate-700 dark:text-slate-200 font-bold hover:dark:bg-transparent">
                    {{ check.providedValue }} <span class="text-slate-400 dark:text-slate-500 font-normal">{{ check.unit }}</span>
                  </p>
                  <p class="text-[10px] text-slate-400 dark:text-slate-500 font-mono">
                    {{ check.passed ? '≤' : '>' }} {{ check.requiredValue }} {{ check.unit }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Violations -->
            <div
              v-if="complianceResult.violations?.length"
              class="space-y-2 pt-1"
            >
              <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Violations &amp; Advisories</p>
              <div
                v-for="v in complianceResult.violations"
                :key="v.rule + v.message"
                class="p-3 rounded-lg border text-xs"
                :class="severityClass[v.severity] ?? 'bg-slate-50 text-slate-700 border-slate-200'"
              >
                <div class="flex items-center gap-2 mb-0.5">
                  <AlertTriangle class="h-3.5 w-3.5 shrink-0" />
                  <span class="font-bold">{{ v.clauseReference }}</span>
                </div>
                <p class="leading-snug ml-5">{{ v.message }}</p>
              </div>
            </div>

            <!-- All-pass banner -->
            <div
              v-if="!complianceResult.violations?.length"
              class="flex items-center gap-2 rounded-lg bg-emerald-50 border border-emerald-200 px-4 py-3 text-sm text-emerald-700 font-medium"
            >
              <CheckCircle2 class="h-4 w-4 shrink-0" />
              All zoning checks passed. No violations detected.
            </div>
          </div>

          <!-- No result yet -->
          <div
            v-else
            class="bg-white border border-dashed border-slate-200 rounded-xl p-8 flex flex-col items-center gap-3 text-center print:hidden"
          >
            <Map class="h-10 w-10 text-slate-200" />
            <p class="text-sm font-bold text-slate-500">No compliance evaluation yet</p>
            <p class="text-xs text-slate-400 max-w-xs">
              Fill in the parameters on the left, then click
              <strong>Run Compliance Check</strong> to evaluate all 6 checks
              against the scheme limits.
            </p>
            <Button
              size="sm"
              class="mt-1 bg-blue-600 hover:bg-blue-700 gap-1.5 text-xs"
              :disabled="isEvaluating || !effectiveScheme"
              @click="runFullCompliance"
            >
              <RefreshCw class="h-3.5 w-3.5" />
              Run Now
            </Button>
          </div>

        </div>
      </div>

      </div>
    </main>
  </div>
</template>

<style>
@media print {
  header, .print\:hidden { display: none !important; }
  .print\:block { display: block !important; }
  body { background: white; }
}
</style>
