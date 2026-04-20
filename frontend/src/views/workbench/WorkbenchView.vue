<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import Viewer3D from './components/Viewer3D.vue'
import AnalysisSidebar from './components/AnalysisSidebar.vue'
import { Play, FileDown, ChevronLeft, Upload, Loader2, FolderOpen, Search, Building2, ArrowRight } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { useAuthStore } from '@/stores/auth.store'
import { complianceService } from '@/services/complianceService'
import { councilPackService } from '@/services/councilPackService'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })

const router = useRouter()
const projectsStore = useProjectsStore()
const uiStore = useUiStore()
const authStore = useAuthStore()

const viewer3DRef = ref(null)
const analysisSidebarRef = ref(null)
const isAnalyzing = ref(false)
const isLoadingProject = ref(false)
const complianceResult = ref(null)
const ifcStats = ref(null)
const ifcDimensions = ref(null)
const ifcAreaSchedule = ref([])

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
      const project = await projectsStore.fetchProject(id)
      projectData.id = project.id
      projectData.name = project.name
      projectData.muni = project.municipality
      projectData.zoning = project.zoningScheme
      projectData.siteAreaM2 = project.siteAreaM2
      projectData.erf = project.erf
      projectData.ifcPath = project.ifcPath ?? null
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
}

function handleIfcAreas(areas) {
  ifcAreaSchedule.value = areas
}

async function handleRunAnalysis(params) {
  if (!projectData.id) {
    toast.warning('Select a project first')
    return
  }
  isAnalyzing.value = true
  try {
    const result = await complianceService.evaluate({
      ...params,
      siteAreaM2: projectData.siteAreaM2,
      zoningScheme: projectData.zoning,
    })
    complianceResult.value = result
    const checks = result.checks ?? []
    const passed = checks.filter((c) => c.passed).length
    toast.success('Analysis complete', { description: `${passed}/${checks.length} checks passed` })
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

function triggerHeaderAnalysis() {
  analysisSidebarRef.value?.triggerAnalysis()
}
</script>

<template>
  <div class="h-[calc(100vh-140px)] flex flex-col gap-6 overflow-hidden">
    <!-- Page Header -->
    <header class="flex items-center justify-between bg-white border border-slate-200 p-4 rounded-xl shadow-sm shrink-0">
      <div class="flex items-center gap-4">
        <Button variant="ghost" size="icon" class="rounded-lg hover:bg-slate-100" @click="router.push({ name: 'dashboard' })">
          <ChevronLeft class="h-4 w-4 text-slate-600" />
        </Button>
        <div class="h-8 w-[1px] bg-slate-200" />
        <div>
          <div v-if="isLoadingProject" class="flex items-center gap-2">
            <Loader2 class="h-4 w-4 animate-spin text-slate-400" />
            <span class="text-xs text-slate-400">Loading project…</span>
          </div>
          <template v-else-if="!projectId">
            <h1 class="text-sm font-bold uppercase tracking-widest text-slate-400">No Project Selected</h1>
            <p class="text-[10px] font-bold text-slate-400 uppercase tracking-tighter">Open a project from the Dashboard</p>
          </template>
          <template v-else>
            <h1 class="text-sm font-bold uppercase tracking-widest text-slate-900">{{ projectData.name }}</h1>
            <p class="text-[10px] font-bold text-slate-500 uppercase tracking-tighter">
              {{ projectData.muni }} • {{ projectData.zoning }}
            </p>
          </template>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <div v-if="complianceScore !== null" class="text-right mr-4 hidden md:block">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Compliance Score</p>
          <p class="text-xl font-mono font-bold" :class="scoreColorClass">{{ complianceScore }}%</p>
        </div>
        <div class="flex items-center gap-2">
          <Button
            variant="outline"
            class="gap-2 uppercase text-xs font-bold h-9"
            @click="viewer3DRef?.openFilePicker()"
          >
            <Upload class="h-3.5 w-3.5 text-blue-600" />
            <span class="hidden sm:inline">{{ projectData.hasModel ? projectData.fileSize : 'Load IFC' }}</span>
          </Button>
          <Button
            variant="outline"
            class="gap-2 uppercase text-xs font-bold h-9 border-blue-200 bg-blue-50/30 text-blue-700 hover:bg-blue-50"
            :disabled="!projectId || isAnalyzing"
            @click="triggerHeaderAnalysis"
          >
            <Loader2 v-if="isAnalyzing" class="h-3.5 w-3.5 animate-spin" />
            <Play v-else class="h-3.5 w-3.5 fill-current" />
            <span class="hidden sm:inline">{{ isAnalyzing ? 'Analyzing…' : 'Run Analysis' }}</span>
          </Button>
          <Button
            class="bg-slate-950 hover:bg-slate-900 text-white gap-2 uppercase text-xs font-bold h-9"
            :disabled="!projectId"
            @click="handleCouncilPack"
          >
            <FileDown class="h-3.5 w-3.5" />
            <span class="hidden sm:inline">Council Pack</span>
          </Button>
        </div>
      </div>
    </header>

    <!-- No project selected: inline project picker -->
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
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
              @click="router.push({ name: 'workbench', params: { projectId: p.id } })"
            >
              <div class="flex-shrink-0 h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center">
                <Building2 class="h-4 w-4 text-primary" />
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-slate-800 truncate">{{ p.name }}</p>
                <p class="text-[11px] text-slate-400">
                  {{ p.municipality }} &middot; {{ p.zoningScheme }}
                </p>
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

    <!-- Workspace Grid -->
    <main v-else class="flex-1 grid grid-cols-1 lg:grid-cols-[1fr_400px] gap-6 min-h-0">
      <!-- 3D Viewport -->
      <section class="min-h-[400px] relative rounded-xl border border-slate-200 bg-[#0f172a] overflow-hidden shadow-2xl">
        <Viewer3D
          ref="viewer3DRef"
          :project-id="projectId"
          :tenant-id="authStore.profile?.tenant_id ?? null"
          :ifc-path="projectData.ifcPath"
          @ifc-loaded="handleIfcLoaded"
          @ifc-stats="handleIfcStats"
          @ifc-dimensions="handleIfcDimensions"
          @ifc-areas="handleIfcAreas"
          @ifc-path-saved="handleIfcPathSaved"
        />
        <div class="absolute bottom-4 left-4 flex gap-2">
          <Badge
            variant="secondary"
            class="bg-slate-950/80 text-white border-slate-700 backdrop-blur-md uppercase text-[10px]"
          >
            Engine: WebGL v2
          </Badge>
        </div>
      </section>

      <!-- Analysis Sidebar -->
      <aside class="flex flex-col min-h-0 bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <AnalysisSidebar
          ref="analysisSidebarRef"
          :project="{ siteAreaM2: projectData.siteAreaM2, zoningScheme: projectData.zoning }"
          :compliance-result="complianceResult"
          :ifc-stats="ifcStats"
          :ifc-dimensions="ifcDimensions"
          :is-analyzing="isAnalyzing"
          @run-analysis="handleRunAnalysis"
        />
      </aside>
    </main>
  </div>
</template>
