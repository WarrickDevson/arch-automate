<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import {
  FileDown, Download, Plus, Loader2, Search,
  FolderOpen, Building2, ArrowRight, CheckCircle2,
  AlertTriangle, ClipboardCheck,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { useSchedulesStore } from '@/stores/schedules.store'
import { useTallyStore } from '@/stores/tally.store'
import { councilPackService } from '@/services/councilPackService'
import { toast } from 'vue-sonner'

const projectsStore = useProjectsStore()
const uiStore = useUiStore()
const schedulesStore = useSchedulesStore()
const tallyStore = useTallyStore()

const activeProjectId = ref(uiStore.lastProjectId)
const pickerSearch = ref('')
const isGenerating = ref(false)

const drawingMeta = ref({
  architect: '',
  drawnBy: '',
  checkedBy: '',
  drawingNumber: '',
  revision: 'P1',
  scale: '1:100',
})

const complianceResult = ref(null)
const historyStorageKey = 'council-pack-history-v1'
const packHistory = ref([])

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
  () => projectsStore.projects.find((p) => p.id === activeProjectId.value) ?? null,
)

const schedule = computed(() => schedulesStore.getSchedule(activeProjectId.value))
const tally = computed(() => tallyStore.getTally(activeProjectId.value))

const checks = computed(() => complianceResult.value?.checks ?? [])
const passedChecks = computed(() => checks.value.filter((c) => c.passed).length)
const failedChecks = computed(() => checks.value.filter((c) => !c.passed).length)

const readiness = computed(() => ({
  hasCompliance: checks.value.length > 0,
  hasSchedule: (schedule.value?.doorCount ?? 0) + (schedule.value?.windowCount ?? 0) > 0,
  hasTally: (tally.value?.totalCount ?? 0) > 0,
}))

const projectHistory = computed(() =>
  packHistory.value
    .filter((item) => item.projectId === activeProjectId.value)
    .sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt)),
)

const siteStatistics = computed(() => {
  if (checks.value.length) {
    return checks.value.map((c) => ({
      description: c.description ?? c.rule ?? 'Rule',
      permitted: `${c.requiredValue ?? '-'} ${c.unit ?? ''}`.trim(),
      proposed: `${c.providedValue ?? '-'} ${c.unit ?? ''}`.trim(),
      compliant: Boolean(c.passed),
    }))
  }

  if (!project.value) return []

  const rows = []
  if (project.value.siteAreaM2 > 0 && project.value.footprintM2 > 0) {
    rows.push({
      description: 'Coverage',
      permitted: 'Scheme limit',
      proposed: `${((project.value.footprintM2 / project.value.siteAreaM2) * 100).toFixed(2)} %`,
      compliant: true,
    })
  }
  if (project.value.siteAreaM2 > 0 && project.value.proposedGfaM2 > 0) {
    rows.push({
      description: 'FAR',
      permitted: 'Scheme limit',
      proposed: (project.value.proposedGfaM2 / project.value.siteAreaM2).toFixed(3),
      compliant: true,
    })
  }
  return rows
})

function loadHistory() {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(historyStorageKey) ?? '[]')
    packHistory.value = Array.isArray(parsed) ? parsed : []
  } catch {
    packHistory.value = []
  }
}

function persistHistory() {
  window.localStorage.setItem(historyStorageKey, JSON.stringify(packHistory.value))
}

function pushHistory(filename) {
  if (!project.value) return
  packHistory.value.push({
    projectId: project.value.id,
    projectName: project.value.name,
    municipality: project.value.municipality,
    filename,
    generatedAt: new Date().toISOString(),
  })
  persistHistory()
}

function loadComplianceFromSession(projectId) {
  try {
    const raw = window.sessionStorage.getItem(`compliance_${projectId}`)
    complianceResult.value = raw ? JSON.parse(raw) : null
  } catch {
    complianceResult.value = null
  }
}

function selectProject(projectItem) {
  activeProjectId.value = projectItem.id
  uiStore.setLastProject(projectItem.id, projectItem.name)
}

async function hydrateProjectContext(projectId) {
  if (!projectId) return

  try {
    const p = await projectsStore.fetchProject(projectId)
    uiStore.setLastProject(p.id, p.name)

    if (!drawingMeta.value.drawingNumber) {
      drawingMeta.value.drawingNumber = `ARCH-${p.erf || '001'}`
    }

    await Promise.allSettled([
      schedulesStore.fetchSchedule(projectId),
      tallyStore.fetchTally(projectId),
    ])

    loadComplianceFromSession(projectId)
  } catch (err) {
    toast.error('Failed to load council pack context', { description: err.message })
  }
}

function exportHistoryCsv() {
  if (!packHistory.value.length) {
    toast.info('No council pack history to export yet')
    return
  }

  const rows = [
    ['Project', 'Municipality', 'File Name', 'Generated At'],
    ...packHistory.value.map((h) => [
      h.projectName,
      h.municipality,
      h.filename,
      new Date(h.generatedAt).toLocaleString('en-ZA'),
    ]),
  ]

  const csv = rows
    .map((row) => row.map((v) => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','))
    .join('\r\n')

  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'CouncilPack_History.csv'
  a.click()
  URL.revokeObjectURL(url)
}

async function generateCouncilPack() {
  if (!project.value) {
    toast.warning('Select a project first')
    return
  }

  const drawingNumber = drawingMeta.value.drawingNumber.trim()
  if (!drawingNumber) {
    toast.warning('Provide a drawing number before generating the pack')
    return
  }

  isGenerating.value = true
  try {
    const safeName = project.value.name.replace(/[^a-zA-Z0-9]/g, '_')
    const revision = drawingMeta.value.revision.trim() || 'P1'
    const filename = `CouncilTables_${safeName}_${revision}.dxf`

    await councilPackService.generateTables({
      projectName: project.value.name,
      erf: project.value.erf,
      municipality: project.value.municipality,
      zoningScheme: project.value.zoningScheme,
      architect: drawingMeta.value.architect,
      drawnBy: drawingMeta.value.drawnBy,
      checkedBy: drawingMeta.value.checkedBy,
      date: new Date().toISOString(),
      scale: drawingMeta.value.scale || '1:100',
      drawingNumber,
      revision,
      siteStatistics: siteStatistics.value,
      areaSchedule: [],
    }, filename)

    uiStore.markCouncilPackGenerated()
    pushHistory(filename)
    toast.success('Council pack generated', { description: filename })
  } catch (err) {
    toast.error('Council pack generation failed', { description: err.message })
  } finally {
    isGenerating.value = false
  }
}

onMounted(async () => {
  loadHistory()

  if (!projectsStore.projects.length) {
    await projectsStore.fetchProjects()
  }

  if (activeProjectId.value) {
    await hydrateProjectContext(activeProjectId.value)
  }
})

watch(activeProjectId, async (id) => {
  if (!id) return
  await hydrateProjectContext(id)
})
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">
    <div v-if="!activeProjectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Council Pack</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to generate submission tables.</p>
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
          <div class="h-8 w-8 rounded-md bg-blue-50 flex items-center justify-center shrink-0">
            <Building2 class="h-4 w-4 text-blue-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Active Project</p>
            <p class="text-sm font-bold text-slate-900 leading-tight truncate max-w-[170px]">{{ project?.name ?? 'Project' }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <ClipboardCheck class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Compliance Checks</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ passedChecks }}<span class="text-xs font-normal text-slate-400"> / {{ checks.length }}</span></p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-amber-50 flex items-center justify-center shrink-0">
            <AlertTriangle class="h-4 w-4 text-amber-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Needs Attention</p>
            <p class="text-xl font-bold leading-tight" :class="failedChecks ? 'text-rose-700' : 'text-emerald-700'">{{ failedChecks }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-slate-100 flex items-center justify-center shrink-0">
            <FileDown class="h-4 w-4 text-slate-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Generated Packs</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ projectHistory.length }}</p>
          </div>
        </div>
      </div>

      <div class="flex-1 min-h-0 rounded-xl border border-slate-200 bg-white shadow-sm flex flex-col overflow-hidden">
        <div class="flex items-center justify-between px-4 py-3 border-b border-slate-100 shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.2em] text-primary/70">Council Pack</p>
            <p class="text-xs text-slate-500 mt-0.5">{{ project?.name ?? 'Project' }} · DXF submission tables</p>
          </div>

          <div class="flex items-center gap-2">
            <Button variant="outline" size="sm" class="gap-1.5 text-xs h-8" @click="exportHistoryCsv">
              <Download class="h-3.5 w-3.5" /> Export History
            </Button>
            <Button
              size="sm"
              class="gap-1.5 text-xs h-8 bg-blue-600 hover:bg-blue-700 text-white"
              :disabled="isGenerating"
              @click="generateCouncilPack"
            >
              <Loader2 v-if="isGenerating" class="h-3.5 w-3.5 animate-spin" />
              <Plus v-else class="h-3.5 w-3.5" />
              Generate Pack
            </Button>
          </div>
        </div>

        <div class="flex-1 min-h-0 overflow-auto p-4">
          <div class="grid grid-cols-1 lg:grid-cols-[360px_1fr] gap-4">
            <section class="space-y-4">
              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Drawing Metadata</h2>
                <div class="space-y-3">
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Architect</Label>
                    <Input v-model="drawingMeta.architect" class="mt-1 h-9" placeholder="Registered architect" />
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Drawn By</Label>
                      <Input v-model="drawingMeta.drawnBy" class="mt-1 h-9" placeholder="Initials" />
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Checked By</Label>
                      <Input v-model="drawingMeta.checkedBy" class="mt-1 h-9" placeholder="Initials" />
                    </div>
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Drawing Number</Label>
                    <Input v-model="drawingMeta.drawingNumber" class="mt-1 h-9" placeholder="ARCH-001" />
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Revision</Label>
                      <Input v-model="drawingMeta.revision" class="mt-1 h-9" placeholder="P1" />
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Scale</Label>
                      <Input v-model="drawingMeta.scale" class="mt-1 h-9" placeholder="1:100" />
                    </div>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Data Readiness</h2>
                <div class="space-y-2 text-xs">
                  <div class="flex items-center justify-between rounded-md bg-slate-50 px-3 py-2 border border-slate-100">
                    <span class="text-slate-600">Zoning compliance loaded</span>
                    <Badge :class="readiness.hasCompliance ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'" variant="secondary">
                      {{ readiness.hasCompliance ? 'Ready' : 'Missing' }}
                    </Badge>
                  </div>
                  <div class="flex items-center justify-between rounded-md bg-slate-50 px-3 py-2 border border-slate-100">
                    <span class="text-slate-600">Schedules extracted</span>
                    <Badge :class="readiness.hasSchedule ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'" variant="secondary">
                      {{ readiness.hasSchedule ? 'Ready' : 'Missing' }}
                    </Badge>
                  </div>
                  <div class="flex items-center justify-between rounded-md bg-slate-50 px-3 py-2 border border-slate-100">
                    <span class="text-slate-600">Fixture tally extracted</span>
                    <Badge :class="readiness.hasTally ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'" variant="secondary">
                      {{ readiness.hasTally ? 'Ready' : 'Missing' }}
                    </Badge>
                  </div>
                </div>
                <p class="text-[11px] text-slate-400 mt-3">
                  Tip: Run the Workbench analysis first for richer council pack table content.
                </p>
              </div>
            </section>

            <section class="space-y-4 min-w-0">
              <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
                <div class="px-5 py-4 border-b border-slate-100">
                  <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500">Recent Pack History</h2>
                </div>
                <div v-if="projectHistory.length === 0" class="p-10 text-center">
                  <FileDown class="h-10 w-10 text-slate-200 mx-auto mb-3" />
                  <p class="text-sm font-medium text-slate-500">No generated packs yet</p>
                  <p class="text-xs text-slate-400 mt-1">Generate your first council pack to build a history trail.</p>
                </div>
                <ul v-else class="divide-y divide-slate-50">
                  <li v-for="item in projectHistory" :key="item.generatedAt + item.filename" class="px-5 py-3.5 flex items-center gap-3">
                    <div class="h-8 w-8 rounded-md bg-blue-50 flex items-center justify-center shrink-0">
                      <CheckCircle2 class="h-4 w-4 text-blue-600" />
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-semibold text-slate-800 truncate">{{ item.filename }}</p>
                      <p class="text-[11px] text-slate-400">{{ new Date(item.generatedAt).toLocaleString('en-ZA') }}</p>
                    </div>
                    <Badge variant="secondary" class="text-[10px]">Downloaded</Badge>
                  </li>
                </ul>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">What Gets Included</h2>
                <ul class="space-y-2 text-xs text-slate-600">
                  <li class="flex items-start gap-2">
                    <span class="mt-1 h-1.5 w-1.5 rounded-full bg-slate-300" />
                    Project metadata: municipality, erf, zoning, drawing control details.
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="mt-1 h-1.5 w-1.5 rounded-full bg-slate-300" />
                    Site statistics from latest zoning analysis, where available.
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="mt-1 h-1.5 w-1.5 rounded-full bg-slate-300" />
                    CAD-ready DXF output for municipal submission drafting workflows.
                  </li>
                </ul>
              </div>
            </section>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
