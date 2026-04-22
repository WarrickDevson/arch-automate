<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  Download, FileDown, FileText, Loader2,
  TableProperties, Building2, Map, Zap, DoorOpen,
  Square, List, FolderOpen, Search, ArrowRight,
  CheckCircle2,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { exportPdf, exportExcel } from '@/services/exportService'
import { councilPackService } from '@/services/councilPackService'
import { useProjectsStore } from '@/stores/projects.store'
import { useSchedulesStore } from '@/stores/schedules.store'
import { useTallyStore } from '@/stores/tally.store'
import { useSpecStore } from '@/stores/spec.store'
import { useUiStore } from '@/stores/ui.store'
import { zoningService } from '@/services/zoningService'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })

const router = useRouter()
const projectsStore = useProjectsStore()
const schedulesStore = useSchedulesStore()
const tallyStore = useTallyStore()
const specStore = useSpecStore()
const uiStore = useUiStore()

const isLoading = ref(false)
const isExportingPdf = ref(false)
const isExportingXls = ref(false)
const isExportingSans = ref(false)

const pickerSearch = ref('')

const zoningResult = ref(null)
const zoningParams = ref(null)
const schemeInfo = ref(null)
const energyResult = ref(null)
const energyParams = ref(null)

const pdfOpts = ref({
  includeZoning: true,
  includeEnergy: true,
  includeSchedules: true,
  includeTally: true,
  includeSpec: true,
})

const xlsOpts = ref({
  includeZoning: true,
  includeEnergy: true,
  includeSchedules: true,
  includeTally: true,
  includeSpec: true,
})

const resolvedProjectId = computed(() => props.projectId || null)

const pickerProjects = computed(() => {
  const q = pickerSearch.value.trim().toLowerCase()
  if (!q) return projectsStore.projects
  return projectsStore.projects.filter(
    (p) =>
      p.name.toLowerCase().includes(q) ||
      p.municipality.toLowerCase().includes(q),
  )
})

const project = computed(
  () => projectsStore.projects.find((p) => p.id === resolvedProjectId.value) ?? null,
)

const schedule = computed(() => schedulesStore.getSchedule(resolvedProjectId.value))
const doors = computed(() => schedule.value?.doors ?? [])
const windows = computed(() => schedule.value?.windows ?? [])

const tally = computed(() => tallyStore.getTally(resolvedProjectId.value))
const tallyItems = computed(() => tally.value?.items ?? [])

const specData = computed(() => specStore.getSpec(resolvedProjectId.value))
const specSections = computed(() => specData.value?.spec?.sections ?? [])

const sectionsReadyCount = computed(() => {
  let count = 0
  if (project.value) count += 1
  if (zoningResult.value) count += 1
  if (energyResult.value) count += 1
  if (doors.value.length || windows.value.length) count += 1
  if (tallyItems.value.length) count += 1
  if (specSections.value.length) count += 1
  return count
})

const hasAnyQuickExport = computed(
  () => doors.value.length || windows.value.length || tallyItems.value.length,
)

onMounted(async () => {
  if (!projectsStore.projects.length) await projectsStore.fetchProjects()
  if (resolvedProjectId.value) await loadAllData()
})

watch(
  () => props.projectId,
  (id) => {
    if (id) loadAllData()
  },
)

function selectProject(projectItem) {
  uiStore.setLastProject(projectItem.id, projectItem.name)
  router.push({ name: 'exports', params: { projectId: projectItem.id } })
}

async function loadAllData() {
  if (!resolvedProjectId.value) return

  isLoading.value = true
  const id = resolvedProjectId.value

  await Promise.allSettled([
    schedulesStore.fetchSchedule(id),
    tallyStore.fetchTally(id),
    specStore.fetchSpec(id),
    loadZoningFromSession(id),
    loadEnergyFromSession(id),
  ])

  isLoading.value = false
}

function loadZoningFromSession(id) {
  try {
    const raw = window.sessionStorage.getItem(`compliance_${id}`)
    if (raw) zoningResult.value = JSON.parse(raw)
    else zoningResult.value = null
  } catch {
    zoningResult.value = null
  }

  const proj = project.value
  if (proj) {
    zoningParams.value = {
      footprintM2: proj.footprintM2 ?? 0,
      proposedGfaM2: proj.proposedGfaM2 ?? 0,
      buildingHeightM: proj.buildingHeightM ?? 0,
      frontSetbackM: proj.frontSetbackM ?? 0,
      rearSetbackM: proj.rearSetbackM ?? 0,
      sideSetbackM: proj.sideSetbackM ?? 0,
      siteAreaM2: proj.siteAreaM2 ?? 0,
    }
  }

  if (proj?.zoningScheme) {
    zoningService
      .getScheme(proj.zoningScheme)
      .then((s) => {
        schemeInfo.value = s
      })
      .catch(() => {
        schemeInfo.value = null
      })
  }
}

function loadEnergyFromSession(id) {
  try {
    const raw = window.sessionStorage.getItem(`energy_${id}`)
    energyResult.value = raw ? JSON.parse(raw) : null
  } catch {
    energyResult.value = null
  }
}

function buildPayload(opts) {
  return {
    project: project.value,
    zoning: opts.includeZoning
      ? { result: zoningResult.value, params: zoningParams.value, scheme: schemeInfo.value }
      : null,
    energy: opts.includeEnergy
      ? { result: energyResult.value, params: energyParams.value }
      : null,
    schedule: opts.includeSchedules ? schedule.value : null,
    tally: opts.includeTally ? tally.value : null,
    spec: opts.includeSpec ? specData.value?.spec : null,
  }
}

async function handleExportPdf() {
  isExportingPdf.value = true
  try {
    exportPdf(buildPayload(pdfOpts.value))
    toast.success('PDF downloaded successfully')
  } catch (e) {
    console.error(e)
    toast.error('PDF generation failed', { description: e.message })
  } finally {
    isExportingPdf.value = false
  }
}

async function handleExportExcel() {
  isExportingXls.value = true
  try {
    exportExcel(buildPayload(xlsOpts.value))
    toast.success('Excel workbook downloaded successfully')
  } catch (e) {
    console.error(e)
    toast.error('Excel generation failed', { description: e.message })
  } finally {
    isExportingXls.value = false
  }
}

async function handleExportSans() {
  isExportingSans.value = true
  try {
    const p = project.value
    if (!p) throw new Error("No active project")
    
    await councilPackService.generateSansForms(p.id, `SANS_Form_1_${p.erf || 'Project'}.pdf`)
    toast.success('SANS Forms downloaded successfully')
  } catch (e) {
    console.error(e)
    toast.error('SANS forms generation failed', { description: e.message })
  } finally {
    isExportingSans.value = false
  }
}

function downloadCsv(filename, rows) {
  const bom = '\uFEFF'
  const csv =
    bom +
    rows
      .map((r) => r.map((c) => `"${String(c ?? '').replace(/"/g, '""')}"`).join(','))
      .join('\r\n')

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

function exportDoorsCsv() {
  const pn = project.value?.name?.replace(/[^a-zA-Z0-9]/g, '_') ?? 'Project'
  downloadCsv(`${pn}_Doors.csv`, [
    ['Mark', 'Name', 'Type', 'Width (mm)', 'Height (mm)', 'Area (m2)', 'Level'],
    ...doors.value.map((d) => [d.mark, d.name, d.type, d.widthMm, d.heightMm, d.areaM2, d.level]),
  ])
  toast.success('Door schedule CSV downloaded')
}

function exportWindowsCsv() {
  const pn = project.value?.name?.replace(/[^a-zA-Z0-9]/g, '_') ?? 'Project'
  downloadCsv(`${pn}_Windows.csv`, [
    ['Mark', 'Name', 'Type', 'Width (mm)', 'Height (mm)', 'Area (m2)', 'Level'],
    ...windows.value.map((w) => [w.mark, w.name, w.type, w.widthMm, w.heightMm, w.areaM2, w.level]),
  ])
  toast.success('Window schedule CSV downloaded')
}

function exportTallyCsv() {
  const pn = project.value?.name?.replace(/[^a-zA-Z0-9]/g, '_') ?? 'Project'
  downloadCsv(`${pn}_Fixture_BOQ.csv`, [
    ['Category', 'IFC Type', 'Mark', 'Name', 'Level'],
    ...tallyItems.value.map((i) => [i.category, i.ifcType, i.mark, i.name, i.level]),
  ])
  toast.success('Fixture tally CSV downloaded')
}
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">
    <div v-if="!resolvedProjectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Document Export</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to export council and QS deliverables.</p>
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
          <Input v-model="pickerSearch" class="pl-9 h-9 text-sm" placeholder="Search by name or municipality..." />
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
              {{ pickerSearch ? 'No projects match your search.' : 'No projects yet - create your first one.' }}
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
              @click="selectProject(p)"
            >
              <div class="flex-shrink-0 h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center">
                <Building2 class="h-4 w-4 text-primary" />
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-slate-800 truncate">{{ p.name }}</p>
                <p class="text-[11px] text-slate-400">{{ p.municipality }} &middot; {{ p.zoningScheme }}</p>
              </div>
              <ArrowRight class="h-4 w-4 text-slate-300 group-hover:text-blue-500 transition-colors" />
            </li>
          </ul>
        </div>
      </div>
    </div>

    <main v-else class="flex-1 min-h-0 flex flex-col gap-3">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-3 shrink-0">
        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-indigo-50 flex items-center justify-center shrink-0">
            <Download class="h-4 w-4 text-indigo-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Ready Sections</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ sectionsReadyCount }}<span class="text-xs font-normal text-slate-400"> / 6</span></p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-violet-50 flex items-center justify-center shrink-0">
            <DoorOpen class="h-4 w-4 text-violet-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Door + Window Rows</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ doors.length + windows.length }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <List class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Fixture Items</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ tallyItems.length }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-sky-50 flex items-center justify-center shrink-0">
            <FileText class="h-4 w-4 text-sky-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Spec Sections</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ specSections.length }}</p>
          </div>
        </div>
      </div>

      <div class="flex-1 min-h-0 rounded-xl border border-slate-200 bg-white shadow-sm flex flex-col overflow-hidden">
        <div class="px-4 py-3 border-b border-slate-100 shrink-0">
          <p class="text-[10px] font-bold uppercase tracking-[0.2em] text-primary/70">Document Export</p>
        </div>

        <div class="flex-1 min-h-0 overflow-auto p-4">
          <div v-if="isLoading" class="h-full flex flex-col items-center justify-center gap-3">
            <Loader2 class="h-6 w-6 animate-spin text-slate-400" />
            <p class="text-sm text-slate-500">Loading export data...</p>
          </div>

          <div v-else class="space-y-4">
            <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4 items-stretch">
              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm flex flex-col gap-4">
                <div class="flex items-start gap-3">
                  <div class="w-10 h-10 rounded-lg bg-indigo-50 flex items-center justify-center shrink-0">
                    <FileText class="w-5 h-5 text-indigo-600" />
                  </div>
                  <div>
                    <h3 class="font-semibold text-slate-800 text-sm">Council Submission PDF</h3>
                    <p class="text-xs text-slate-500 mt-1">Includes project metadata, zoning, energy, schedules, tally and spec sections based on selected options.</p>
                  </div>
                </div>

                <div class="flex flex-wrap gap-1.5">
                  <Badge variant="secondary" :class="project ? 'bg-emerald-100 text-emerald-700' : ''">Project</Badge>
                  <Badge variant="secondary" :class="zoningResult ? 'bg-emerald-100 text-emerald-700' : ''">Zoning</Badge>
                  <Badge variant="secondary" :class="energyResult ? 'bg-emerald-100 text-emerald-700' : ''">Energy</Badge>
                  <Badge variant="secondary" :class="doors.length || windows.length ? 'bg-emerald-100 text-emerald-700' : ''">Schedules</Badge>
                  <Badge variant="secondary" :class="tallyItems.length ? 'bg-emerald-100 text-emerald-700' : ''">Tally</Badge>
                  <Badge variant="secondary" :class="specSections.length ? 'bg-emerald-100 text-emerald-700' : ''">Spec</Badge>
                </div>

                <div class="space-y-2 text-xs text-slate-600">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="pdfOpts.includeZoning" type="checkbox" class="rounded" />
                    Include zoning compliance
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="pdfOpts.includeEnergy" type="checkbox" class="rounded" />
                    Include energy compliance
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="pdfOpts.includeSchedules" type="checkbox" class="rounded" />
                    Include door and window schedules
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="pdfOpts.includeTally" type="checkbox" class="rounded" />
                    Include fixture tally
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="pdfOpts.includeSpec" type="checkbox" class="rounded" />
                    Include specification clauses
                  </label>
                </div>

                <Button :disabled="isExportingPdf" class="w-full mt-auto bg-slate-900 dark:bg-slate-800 text-white dark:text-slate-100 hover:bg-slate-800 dark:hover:bg-slate-700" @click="handleExportPdf">
                  <Loader2 v-if="isExportingPdf" class="w-4 h-4 mr-2 animate-spin" />
                  <FileDown v-else class="w-4 h-4 mr-2" />
                  {{ isExportingPdf ? 'Generating...' : 'Download PDF' }}
                </Button>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm flex flex-col gap-4">
                <div class="flex items-start gap-3">
                  <div class="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center shrink-0">
                    <TableProperties class="w-5 h-5 text-emerald-600" />
                  </div>
                  <div>
                    <h3 class="font-semibold text-slate-800 text-sm">QS Excel Workbook</h3>
                    <p class="text-xs text-slate-500 mt-1">Multi-sheet workbook with project summary and available compliance/schedule/BOQ/spec exports.</p>
                  </div>
                </div>

                <div class="space-y-2 text-xs text-slate-600">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="xlsOpts.includeZoning" type="checkbox" class="rounded" />
                    Include zoning sheet
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="xlsOpts.includeEnergy" type="checkbox" class="rounded" />
                    Include energy sheet
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="xlsOpts.includeSchedules" type="checkbox" class="rounded" />
                    Include door and window sheets
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="xlsOpts.includeTally" type="checkbox" class="rounded" />
                    Include fixture BOQ sheet
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input v-model="xlsOpts.includeSpec" type="checkbox" class="rounded" />
                    Include specification sheet
                  </label>
                </div>

                <Button
                  :disabled="isExportingXls"
                  variant="outline"
                  class="w-full mt-auto border-emerald-300 text-emerald-700 hover:bg-emerald-50 dark:border-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-400 dark:hover:bg-emerald-900/50"
                  @click="handleExportExcel"
                >
                  <Loader2 v-if="isExportingXls" class="w-4 h-4 mr-2 animate-spin" />
                  <TableProperties v-else class="w-4 h-4 mr-2" />
                  {{ isExportingXls ? 'Generating...' : 'Download Excel (.xlsx)' }}
                </Button>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden flex flex-col">
                <div class="p-5 flex flex-col flex-1 bg-slate-50/50">
                  <div class="flex items-center justify-between mb-2">
                    <h3 class="text-sm font-semibold text-slate-700">Official SANS Forms</h3>
                  </div>
                  <p class="text-xs text-slate-500 mt-1 mb-4">Generates pre-filled SANS 10400 PDF declaration forms using project data, ready for signing.</p>
                  
                  <div class="mt-auto">
                    <Button
                      variant="default"
                      class="w-full bg-blue-600 hover:bg-blue-700 text-white"
                      :disabled="isExportingSans"
                      @click="handleExportSans"
                    >
                      <Loader2 v-if="isExportingSans" class="w-4 h-4 mr-2 animate-spin" />
                      <FileText v-else class="w-4 h-4 mr-2" />
                      {{ isExportingSans ? 'Generating...' : 'Download SANS Forms' }}
                    </Button>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden flex flex-col h-full">
                <div class="px-5 py-4 border-b border-slate-100 flex flex-col gap-1">
                  <h3 class="text-sm font-semibold text-slate-700">Quick Exports</h3>
                  <span class="text-[11px] text-slate-400">
                    {{ hasAnyQuickExport ? 'CSV shortcuts enabled' : 'No schedule or tally data loaded yet' }}
                  </span>
                </div>
                <div class="divide-y divide-slate-100 flex-1 overflow-y-auto">
                <div class="flex items-center justify-between px-5 py-3 gap-4">
                  <div>
                    <p class="text-sm font-medium text-slate-700">Door schedule only</p>
                    <p class="text-xs text-slate-400">CSV of all extracted doors</p>
                  </div>
                  <Button variant="outline" size="sm" class="h-8 text-xs" :disabled="!doors.length" @click="exportDoorsCsv">
                    <CheckCircle2 class="h-3.5 w-3.5 mr-1" /> CSV
                  </Button>
                </div>

                <div class="flex items-center justify-between px-5 py-3 gap-4">
                  <div>
                    <p class="text-sm font-medium text-slate-700">Window schedule only</p>
                    <p class="text-xs text-slate-400">CSV of all extracted windows</p>
                  </div>
                  <Button variant="outline" size="sm" class="h-8 text-xs" :disabled="!windows.length" @click="exportWindowsCsv">
                    <CheckCircle2 class="h-3.5 w-3.5 mr-1" /> CSV
                  </Button>
                </div>

                <div class="flex items-center justify-between px-5 py-3 gap-4">
                  <div>
                    <p class="text-sm font-medium text-slate-700">Fixture tally (BOQ) only</p>
                    <p class="text-xs text-slate-400">CSV of all fixture and MEP items</p>
                  </div>
                  <Button variant="outline" size="sm" class="h-8 text-xs" :disabled="!tallyItems.length" @click="exportTallyCsv">
                    <CheckCircle2 class="h-3.5 w-3.5 mr-1" /> CSV
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      </div>
    </main>
  </div>
</template>
