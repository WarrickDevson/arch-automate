<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import Viewer3D from './components/Viewer3D.vue'
import AnalysisSidebar from './components/AnalysisSidebar.vue'
import { FileDown, Upload, Loader2, FolderOpen, Search, Building2, ArrowRight } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { useAuthStore } from '@/stores/auth.store'
import { useMunicipalitiesStore } from '@/stores/municipalities.store'
import { useSchedulesStore } from '@/stores/schedules.store'
import { useTallyStore } from '@/stores/tally.store'
import { useSpecStore } from '@/stores/spec.store'
import { complianceService } from '@/services/complianceService'
import { councilPackService } from '@/services/councilPackService'
import { showPrompt } from '@/services/promptService'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })

const router = useRouter()
const projectsStore = useProjectsStore()
const uiStore = useUiStore()
const authStore = useAuthStore()
const municipalitiesStore = useMunicipalitiesStore()
const schedulesStore = useSchedulesStore()
const tallyStore = useTallyStore()
const specStore = useSpecStore()

const viewer3DRef = ref(null)
const isAnalyzing = ref(false)
const isLoadingProject = ref(false)
const complianceResult = ref(null)
const energyResult = ref(null)
const ifcStats = ref(null)
const ifcDimensions = ref(null)
const ifcAreaSchedule = ref([])
const ifcGlazingUValue = ref(null)  // extracted from IFC window attributes
const selectedIfcElement = ref(null)
const measurementResult = ref(null)
const ifcLoadProgress = reactive({
  active: false,
  label: '',
  percent: 0,
  source: 'manual',
})

// Project picker (shown when no projectId in route)
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

const projectData = reactive({
  id: null,
  name: 'No Project Loaded',
  muni: '',
  zoning: '-',
  siteAreaM2: 0,
  erf: '',
  fileSize: '0 MB',
  hasModel: false,
  ifcPath: null,
  municipalityId: null,
  province: '',
  // Persisted analysis params (loaded from project, saved back on analysis)
  proposedGfaM2: null,
  footprintM2: null,
  numberOfStoreys: null,
  buildingHeightM: null,
  frontSetbackM: null,
  rearSetbackM: null,
  sideSetbackM: null,
  parkingBays: null,
  glaForParkingM2: null,
})

const complianceScore = computed(() => {
  if (!complianceResult.value) return null
  const checks = complianceResult.value.checks ?? []
  if (!checks.length) return 0
  return Math.round((checks.filter((c) => c.passed).length / checks.length) * 100)
})

const scoreColorClass = computed(() => {
  if (complianceScore.value === null) return 'text-slate-900'
  if (complianceScore.value >= 80) return 'text-emerald-600'
  if (complianceScore.value >= 60) return 'text-amber-500'
  return 'text-rose-600'
})

const passRateLabel = computed(() => {
  if (complianceScore.value === null) return 'Pass Rate: --'
  return `Pass Rate: ${complianceScore.value}%`
})

const isManualIfcProgress = computed(
  () => ifcLoadProgress.active && ifcLoadProgress.source === 'manual',
)

const showWorkbenchRestoreSkeleton = computed(() => {
  if (projectData.hasModel) return false
  if (ifcLoadProgress.active && ifcLoadProgress.source === 'restore') return true
  if (projectData.ifcPath) return true
  return Boolean(props.projectId && isLoadingProject.value)
})

const showWorkbenchEmptyState = computed(
  () => !projectData.hasModel && !showWorkbenchRestoreSkeleton.value,
)

// Watches projectId (optional param on same route) — fires on mount AND when
// the user picks a project from the inline picker, since the component is
// reused (not remounted) when the optional param changes.
watch(
  () => props.projectId,
  async (id) => {
    if (!id) {
      if (uiStore.lastProjectId) {
        router.replace({ name: 'workbench', params: { projectId: uiStore.lastProjectId } })
      } else {
        projectsStore.fetchProjects()
      }
      return
    }

    isLoadingProject.value = true
    try {
      // Ensure municipality list is loaded so we can resolve province
      municipalitiesStore.fetchMunicipalities()

      const project = await projectsStore.fetchProject(id)
      projectData.id = project.id
      projectData.name = project.name
      projectData.muni = project.municipality
      projectData.zoning = project.zoningScheme
      projectData.siteAreaM2 = project.siteAreaM2
      projectData.erf = project.erf
      projectData.ifcPath = project.ifcPath ?? null
      projectData.municipalityId = project.municipalityId ?? null
      projectData.proposedGfaM2 = project.proposedGfaM2 ?? null
      projectData.footprintM2 = project.footprintM2 ?? null
      projectData.numberOfStoreys = project.numberOfStoreys ?? null
      projectData.buildingHeightM = project.buildingHeightM ?? null
      projectData.frontSetbackM = project.frontSetbackM ?? null
      projectData.rearSetbackM = project.rearSetbackM ?? null
      projectData.sideSetbackM = project.sideSetbackM ?? null
      projectData.parkingBays = project.parkingBays ?? null
      projectData.glaForParkingM2 = project.glaForParkingM2 ?? null

      // Restore last analysis results from session cache (survives navigation, not hard refresh)
      try {
        const cached = window.sessionStorage.getItem(`compliance_${project.id}`)
        const cachedEnergy = window.sessionStorage.getItem(`energy_${project.id}`)
        if (cached) complianceResult.value = JSON.parse(cached)
        if (cachedEnergy) energyResult.value = JSON.parse(cachedEnergy)
      } catch { /* corrupted cache */ }

      // Resolve province from municipality store (may be async — watch municipalityId below)
      projectData.province = municipalitiesStore.getProvinceByMunicipalityId(project.municipalityId)
      uiStore.setLastProject(project.id, project.name)
      uiStore.markWorkbenchOpened()
    } catch (err) {
      toast.error('Failed to load project', { description: err.message })
    } finally {
      isLoadingProject.value = false
    }
  },
  { immediate: true },
)

// Back-fill province once municipalities have finished loading asynchronously
watch(
  () => municipalitiesStore.loaded,
  (loaded) => {
    if (loaded && projectData.municipalityId && !projectData.province) {
      projectData.province = municipalitiesStore.getProvinceByMunicipalityId(projectData.municipalityId)
    }
  },
)

function handleIfcLoaded({ fileSize }) {
  projectData.fileSize = `${(fileSize / 1024 / 1024).toFixed(2)} MB`
  projectData.hasModel = true
}

function handleIfcPathSaved(path) {
  projectData.ifcPath = path
  // Persist to the backend so other devices can restore the model
  if (projectData.id) {
    projectsStore.updateIfcPath(projectData.id, path).catch((e) =>
      console.warn('Failed to persist IFC path:', e),
    )
  }
}

function handleIfcStats(stats) {
  ifcStats.value = stats
}

function handleIfcDimensions(dims) {
  ifcDimensions.value = dims
  persistIfcData()
}

function handleIfcAreas(areas) {
  ifcAreaSchedule.value = areas
  persistIfcData()
}

function handleIfcThermal({ glazingUValue, sampleCount }) {
  ifcGlazingUValue.value = glazingUValue
  console.warn(`IFC thermal: U-value ${glazingUValue} W/(m²·K) averaged from ${sampleCount} window(s)`)
}

function handleIfcSchedule({ doors, windows }) {
  if (!projectData.id) return
  schedulesStore
    .saveSchedule(projectData.id, { doors, windows })
    .catch((e) => console.warn('Schedule save failed:', e))
}

function handleIfcTally(items) {
  // Persist tally even when zero fixtures are found so the API has a row
  // and downstream views can show explicit 0-count state instead of 404.
  if (!projectData.id || !Array.isArray(items)) return
  tallyStore
    .saveTally(projectData.id, items)
    .catch((e) => console.warn('Tally save failed:', e))
}

function handleIfcMaterials(materialItems) {
  if (!projectData.id || !materialItems?.length) return
  specStore
    .compile(projectData.id, materialItems)
    .catch((e) => console.warn('Spec compile failed:', e))
}

function handleIfcElementSelected(element) {
  selectedIfcElement.value = element
}

function handleMeasurementUpdated(result) {
  measurementResult.value = result
}

function handleIfcLoadError(payload) {
  ifcLoadProgress.active = false
  ifcLoadProgress.label = ''
  ifcLoadProgress.percent = 0
  ifcLoadProgress.source = 'manual'
  const message = payload?.message || 'Could not parse this IFC file.'
  const detail = payload?.detail || 'Ensure the file is valid IFC2x3/IFC4 and exported fully.'
  toast.error(message, { description: detail })
}

function handleIfcLoadProgress(payload) {
  const active = Boolean(payload?.active)
  const label = payload?.label ?? ''
  const rawPercent = payload?.percent
  const source = payload?.source === 'restore' ? 'restore' : 'manual'

  ifcLoadProgress.active = active
  ifcLoadProgress.label = label
  ifcLoadProgress.source = source

  if (typeof rawPercent === 'number' && Number.isFinite(rawPercent)) {
    ifcLoadProgress.percent = Math.max(0, Math.min(100, Math.round(rawPercent)))
  } else if (!active) {
    ifcLoadProgress.percent = 0
    ifcLoadProgress.source = 'manual'
  }
}

function clearCurrentIfcContext() {
  complianceResult.value = null
  energyResult.value = null
  ifcStats.value = null
  ifcDimensions.value = null
  ifcAreaSchedule.value = []
  ifcGlazingUValue.value = null
  selectedIfcElement.value = null
  measurementResult.value = null
  projectData.hasModel = false
  projectData.fileSize = '0 MB'
}

async function handleIfcActionClick() {
  if (!projectData.hasModel) {
    viewer3DRef.value?.openFilePicker()
    return
  }

  const confirmed = await showPrompt({
    mode: 'confirm',
    title: 'Replace IFC model?',
    message:
      'This will clear current analysis and IFC-derived data from this session before loading a new file.',
    confirmText: 'Replace IFC',
    cancelText: 'Keep Current',
    isDestructive: true,
  })

  if (!confirmed) return

  clearCurrentIfcContext()
  if (projectData.id) {
    try {
      window.sessionStorage.removeItem(`compliance_${projectData.id}`)
      window.sessionStorage.removeItem(`energy_${projectData.id}`)
    } catch {
      // Ignore storage failures and proceed with replacement.
    }
  }
  viewer3DRef.value?.openFilePicker()
}

// Silently persist IFC-extracted data to the project record.
// Called from both handleIfcDimensions and handleIfcAreas so it fires
// once both datasets are available (the later one will have both).
function persistIfcData() {
  if (!projectData.id) return
  const dims = ifcDimensions.value
  const areas = ifcAreaSchedule.value
  if (!dims) return

  const proposedGfaM2 =
    areas.length > 0
      ? parseFloat(areas.reduce((sum, r) => sum + (r.areaM2 || 0), 0).toFixed(2))
      : null

  projectsStore
    .saveIfcData(projectData.id, {
      proposedGfaM2,
      footprintM2: dims.footprintM2 ?? null,
      buildingHeightM: dims.heightM ?? null,
      numberOfStoreys: dims.numberOfStoreys ?? null,
    })
    .catch((e) => console.warn('Failed to persist IFC data:', e))
}

async function handleRunAnalysis(params) {
  if (!projectData.id) {
    toast.warning('Select a project first')
    return
  }
  isAnalyzing.value = true
  try {
    const [result, xaResult] = await Promise.all([
      complianceService.evaluate({
        ...params,
        siteAreaM2: projectData.siteAreaM2,
        zoningScheme: projectData.zoning,
      }),
      complianceService.evaluateEnergy({
        ...params.energy,
        proposedGfaM2: params.proposedGfaM2,
        footprintM2: params.footprintM2,
        buildingHeightM: params.buildingHeightM,
        numberOfStoreys: params.numberOfStoreys,
      }),
    ])
    complianceResult.value = result
    energyResult.value = xaResult
    // Silently persist the sidebar params so they survive refresh
    projectsStore.saveParams(projectData.id, {
      proposedGfaM2: params.proposedGfaM2 || null,
      footprintM2: params.footprintM2 || null,
      buildingHeightM: params.buildingHeightM || null,
      numberOfStoreys: params.numberOfStoreys || null,
      frontSetbackM: params.frontSetbackM || null,
      rearSetbackM: params.rearSetbackM || null,
      sideSetbackM: params.sideSetbackM || null,
      parkingBays: params.parkingBaysProvided || null,
      glaForParkingM2: params.glaForParkingM2 || null,
    }).catch((e) => console.warn('Failed to persist params:', e))
    // Cache analysis results in sessionStorage so they survive navigation (not hard refresh)
    try {
      window.sessionStorage.setItem(`compliance_${projectData.id}`, JSON.stringify(result))
      window.sessionStorage.setItem(`energy_${projectData.id}`, JSON.stringify(xaResult))
    } catch { /* storage quota */ }
    const checks = result.checks ?? []
    const passed = checks.filter((c) => c.passed).length
    toast.success('Analysis complete', {
      description: `${passed}/${checks.length} checks passed · Energy rating: ${xaResult.energyRating ?? '–'}`,
    })
  } catch (err) {
    toast.error('Analysis failed', { description: err.message })
  } finally {
    isAnalyzing.value = false
  }
}

async function handleCouncilPack() {
  if (!projectData.id) {
    toast.warning('Select a project first')
    return
  }
  const checks = complianceResult.value?.checks ?? []
  const siteStatistics = checks.map((c) => ({
    description: c.description,
    permitted: `${c.requiredValue} ${c.unit}`,
    proposed: `${c.providedValue} ${c.unit}`,
    compliant: c.passed,
  }))
  try {
    const safeName = projectData.name.replace(/\s+/g, '_')
    await councilPackService.generateTables(
      {
        projectName: projectData.name,
        erf: projectData.erf,
        municipality: projectData.muni,
        zoningScheme: projectData.zoning,
        architect: '',
        drawnBy: '',
        checkedBy: '',
        date: new Date().toISOString(),
        scale: '1:100',
        drawingNumber: `ARCH-${projectData.erf || '001'}`,
        revision: 'P1',
        siteStatistics,
        areaSchedule: ifcAreaSchedule.value,
      },
      `CouncilTables_${safeName}_P1.dxf`,
    )
    toast.success('Council Pack downloaded')
  } catch (err) {
    toast.error('Council Pack failed', { description: err.message })
  }
}
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">
    <!-- No project selected: inline project picker -->
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm p-6 flex flex-col gap-4 h-full">
        <!-- Header -->
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Workbench</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to open in the Workbench, or create a new one.</p>
          </div>
          <Button
            class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold"
            @click="uiStore.openCreateProjectSheet()"
          >
            <ArrowRight class="h-3.5 w-3.5" /> New Project
          </Button>
        </div>

        <!-- Search -->
        <div class="relative shrink-0">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
          <Input
            v-model="pickerSearch"
            class="pl-9 h-9 text-sm"
            placeholder="Search by name or municipality…"
          />
        </div>

        <!-- Project list -->
        <div class="flex-1 overflow-y-auto">
          <div v-if="projectsStore.loading" class="space-y-2">
            <div v-for="n in 5" :key="n" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-lg animate-pulse" />
          </div>

          <div
            v-else-if="pickerProjects.length === 0"
            class="flex flex-col items-center justify-center h-full py-16 text-center"
          >
            <FolderOpen class="h-10 w-10 text-slate-200 dark:text-slate-700 mb-3" />
            <p class="text-sm font-medium text-slate-400 dark:text-slate-500">
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
              class="flex items-center gap-4 px-4 py-3 rounded-lg border border-slate-100 dark:border-slate-800 bg-slate-50/60 dark:bg-slate-950/60 hover:bg-white dark:hover:bg-slate-900 hover:border-blue-200 dark:hover:border-blue-900 hover:shadow-sm cursor-pointer transition-all group"
              @click="router.push({ name: 'workbench', params: { projectId: p.id } })"
            >
              <div class="flex-shrink-0 h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center">
                <Building2 class="h-4 w-4 text-primary" />
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-slate-800 dark:text-slate-200 truncate">{{ p.name }}</p>
                <p class="text-[11px] text-slate-400 dark:text-slate-500">
                  {{ p.municipality }} &middot; {{ p.zoningScheme }}
                </p>
              </div>
              <div class="flex-shrink-0 flex items-center gap-2">
                <span class="text-[10px] font-bold text-slate-400 dark:text-slate-500 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-full uppercase">
                  {{ p.status === 'SubmittedToCouncil' ? 'In Council' : p.status }}
                </span>
                <ArrowRight class="h-4 w-4 text-slate-300 dark:text-slate-600 group-hover:text-blue-500 dark:group-hover:text-blue-400 transition-colors" />
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <!-- Workspace Grid -->
    <main v-else class="flex-1 min-h-0">
      <div class="relative h-full min-h-0">
        <div
          class="h-full transition-opacity duration-300"
          :class="projectData.hasModel ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'"
        >
          <div class="grid h-full min-h-0 grid-cols-1 gap-6 lg:grid-cols-[1fr_400px]">
            <!-- 3D Viewport -->
            <section class="min-h-[400px] relative rounded-xl border border-slate-200 bg-[#0f172a] overflow-hidden shadow-2xl">
              <Viewer3D
                ref="viewer3DRef"
                :project-id="projectId"
                :tenant-id="authStore.profile?.tenant_id ?? null"
                :ifc-path="projectData.ifcPath"
                @ifc-loaded="handleIfcLoaded"
                @ifc-load-progress="handleIfcLoadProgress"
                @ifc-stats="handleIfcStats"
                @ifc-dimensions="handleIfcDimensions"
                @ifc-areas="handleIfcAreas"
                @ifc-path-saved="handleIfcPathSaved"
                @ifc-thermal="handleIfcThermal"
                @ifc-schedule="handleIfcSchedule"
                @ifc-tally="handleIfcTally"
                @ifc-materials="handleIfcMaterials"
                @element-selected="handleIfcElementSelected"
                @measurement-updated="handleMeasurementUpdated"
                @ifc-load-error="handleIfcLoadError"
              />
            </section>

            <div v-if="projectData.hasModel" class="flex min-h-0 flex-col gap-3">
              <!-- Standalone action cards -->
              <div class="shrink-0 relative grid grid-cols-3 gap-3">
                <div class="pointer-events-none absolute inset-x-0 -top-1 z-10 flex justify-center">
                  <div
                    class="flex items-center gap-2 rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 px-4 py-2.5 shadow-sm transition-all duration-200"
                    :class="isLoadingProject ? 'translate-y-0 opacity-100' : '-translate-y-1 opacity-0'"
                    role="status"
                    aria-live="polite"
                  >
                    <Loader2 class="h-3.5 w-3.5 animate-spin text-slate-400 dark:text-slate-500" />
                    <span class="text-xs text-slate-400 dark:text-slate-500">Loading project...</span>
                  </div>
                </div>

                <Button
                  variant="outline"
                  class="h-10 w-full gap-2 px-3 text-[10px] font-bold uppercase rounded-xl border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 dark:text-slate-200 shadow-sm hover:bg-slate-50 dark:hover:bg-slate-800"
                  @click="handleIfcActionClick"
                >
                  <Upload class="h-3.5 w-3.5 text-blue-600 dark:text-blue-500" />
                  <span>{{ projectData.hasModel ? 'Replace IFC' : 'Load IFC' }}</span>
                </Button>

                <Button
                  class="h-10 w-full gap-2 bg-slate-900 dark:bg-slate-800 px-3 text-[10px] font-bold uppercase text-white dark:text-slate-100 hover:bg-slate-800 dark:hover:bg-slate-700 rounded-xl shadow-sm"
                  :disabled="!projectId"
                  @click="handleCouncilPack"
                >
                  <FileDown class="h-3.5 w-3.5" />
                  <span>Council Pack</span>
                </Button>

                <div
                  class="h-10 flex items-center justify-center rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm px-3 text-[10px] font-bold uppercase whitespace-nowrap"
                  :class="complianceScore === null ? 'text-slate-400 dark:text-slate-500' : scoreColorClass"
                >
                  {{ passRateLabel }}
                </div>
              </div>

              <!-- Analysis Sidebar -->
              <aside class="flex flex-col min-h-0 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden">
                <AnalysisSidebar
                  :project="{ siteAreaM2: projectData.siteAreaM2, zoningScheme: projectData.zoning,
                    proposedGfaM2: projectData.proposedGfaM2, footprintM2: projectData.footprintM2,
                    numberOfStoreys: projectData.numberOfStoreys, buildingHeightM: projectData.buildingHeightM,
                    frontSetbackM: projectData.frontSetbackM, rearSetbackM: projectData.rearSetbackM,
                    sideSetbackM: projectData.sideSetbackM, parkingBays: projectData.parkingBays,
                    glaForParkingM2: projectData.glaForParkingM2 }"
                  :compliance-result="complianceResult"
                  :energy-result="energyResult"
                  :ifc-stats="ifcStats"
                  :ifc-dimensions="ifcDimensions"
                  :is-analyzing="isAnalyzing"
                  :province="projectData.province"
                  :ifc-glazing-u-value="ifcGlazingUValue"
                  @run-analysis="handleRunAnalysis"
                />
              </aside>
            </div>
          </div>
        </div>

        <div
          v-if="showWorkbenchRestoreSkeleton"
          class="absolute inset-0 rounded-2xl border border-slate-200 dark:border-slate-800 bg-gradient-to-b from-slate-50 to-white dark:from-slate-950 dark:to-slate-950 p-4 md:p-6"
        >
          <div class="mb-4 flex items-center gap-2 text-slate-500 dark:text-slate-400">
            <Loader2 class="h-4 w-4 animate-spin" />
            <p class="text-xs font-semibold uppercase tracking-wide">
              {{ ifcLoadProgress.label || 'Restoring existing IFC workspace...' }}
            </p>
          </div>

          <div class="grid h-[calc(100%-2rem)] min-h-0 grid-cols-1 gap-6 lg:grid-cols-[1fr_400px]">
            <div class="h-full min-h-[340px] animate-pulse rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-100/70 dark:bg-slate-800/70" />

            <div class="hidden min-h-0 flex-col gap-4 lg:flex">
              <div class="h-14 animate-pulse rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-100/80 dark:bg-slate-800/80" />
              <div class="flex-1 animate-pulse rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-100/70 dark:bg-slate-800/70" />
            </div>
          </div>
        </div>

        <div
          v-if="showWorkbenchEmptyState"
          class="absolute inset-0 flex items-center justify-center rounded-2xl border border-slate-200 dark:border-slate-800 bg-[radial-gradient(ellipse_at_top,#e0f2fe_0%,#ffffff_55%,#f8fafc_100%)] dark:bg-[radial-gradient(ellipse_at_top,#0f172a_0%,#020617_55%,#020617_100%)] p-6"
        >
          <div class="w-full max-w-2xl rounded-2xl border border-slate-200/80 dark:border-slate-800/80 bg-white/85 dark:bg-slate-900/85 p-8 shadow-xl backdrop-blur-sm">
            <div class="mx-auto mb-6 flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-50 dark:bg-slate-800 text-blue-600 dark:text-blue-400 ring-1 ring-blue-100 dark:ring-slate-700">
              <Upload class="h-6 w-6" />
            </div>

            <div class="text-center">
              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-blue-600/80 dark:text-blue-400/80">Workbench</p>
              <h2 class="mt-2 text-2xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100">Load IFC Model</h2>
              <p class="mx-auto mt-3 max-w-xl text-sm text-slate-500 dark:text-slate-400">
                Start by loading an IFC file to open the 3D workspace, analysis panel, and compliance tools.
              </p>
            </div>

            <div class="mt-8 flex flex-col items-center gap-3">
              <Button
                class="h-10 gap-2 bg-blue-600 px-5 text-xs font-bold uppercase tracking-wide text-white hover:bg-blue-700"
                :disabled="isLoadingProject || isManualIfcProgress"
                @click="handleIfcActionClick"
              >
                <Loader2 v-if="isLoadingProject || isManualIfcProgress" class="h-3.5 w-3.5 animate-spin" />
                <Upload v-else class="h-3.5 w-3.5" />
                <span>{{ isLoadingProject ? 'Preparing Workspace...' : (isManualIfcProgress ? 'Loading IFC...' : 'Load IFC Model') }}</span>
              </Button>
              <p class="text-[11px] text-slate-400">IFC2x3 and IFC4 formats are supported.</p>

              <div v-if="isManualIfcProgress" class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 p-3">
                <div class="mb-1.5 flex items-center justify-between gap-2 text-[11px]">
                  <span class="truncate text-slate-600 dark:text-slate-400">{{ ifcLoadProgress.label || 'Processing IFC...' }}</span>
                  <span class="font-mono font-bold text-slate-500 dark:text-slate-400">{{ ifcLoadProgress.percent }}%</span>
                </div>
                <div class="h-2 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                  <div
                    class="h-full rounded-full bg-blue-600 transition-all duration-300"
                    :style="{ width: `${ifcLoadProgress.percent}%` }"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
