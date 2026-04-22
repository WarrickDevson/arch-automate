<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  Loader2, HardHat, CheckCircle2, XCircle,
  RefreshCw, Printer, FolderOpen, Search, Building2, ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { useProjectsStore } from '@/stores/projects.store'
import { useFoundationStore } from '@/stores/foundation.store'
import { useUiStore } from '@/stores/ui.store'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })
const router = useRouter()
const projectsStore = useProjectsStore()
const foundationStore = useFoundationStore()
const uiStore = useUiStore()

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

const project = ref(null)
const isLoadingProject = ref(false)

const form = ref({
  numberOfStoreys: 1,
  foundationType: 'Strip',
  externalWidthMm: 600,
  externalDepthMm: 200,
  internalWidthMm: 450,
  internalDepthMm: 200,
  concreteMpa: 25,
  belowNglMm: 300,
  finishedFloorAboveNglMm: 170,
  dpcSpecified: true,
  dpcMicron: 375,
  brickForceSpecified: true,
  compactionSpec: true,
  engineerAppointed: false,
  pestTreatmentSpecified: true,
})

const FOUNDATION_TYPES = ['Strip', 'Pad', 'Raft', 'Pile']
const STOREY_OPTIONS = [1, 2, 3, 4]

const MINIMUMS = {
  1: { extW: 600, extD: 200, intW: 450, intD: 200 },
  2: { extW: 700, extD: 250, intW: 600, intD: 250 },
}
const currentMin = computed(() => {
  const s = Math.min(form.value.numberOfStoreys, 2)
  return MINIMUMS[s] ?? MINIMUMS[2]
})
const engineerRequired = computed(
  () => form.value.numberOfStoreys >= 3 || ['Pad', 'Raft', 'Pile'].includes(form.value.foundationType)
)

const result = computed(() => foundationStore.getCheck(props.projectId))
const isLoading = computed(() => foundationStore.isLoading(props.projectId))
const isSaving = ref(false)

const passCount = computed(() => result.value?.results?.checks?.filter(c => c.passed).length ?? 0)
const totalCount = computed(() => result.value?.results?.checks?.length ?? 0)
const failCount = computed(() => totalCount.value - passCount.value)
const scorePercent = computed(() => {
  if (!totalCount.value) return 0
  return Math.round((passCount.value / totalCount.value) * 100)
})

function checkTypeBadge(type) {
  return type === 'Numeric' ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-700'
}

async function runCheck() {
  if (!props.projectId) return toast.error('No project selected')
  isSaving.value = true
  try {
    await foundationStore.evaluate(props.projectId, form.value)
    toast.success('Foundation check complete')
  } catch {
    toast.error('Failed to run foundation check')
  } finally {
    isSaving.value = false
  }
}

function print() { window.print() }

function selectProject(projectItem) {
  uiStore.setLastProject(projectItem.id, projectItem.name)
  router.push({ name: 'foundation', params: { projectId: projectItem.id } })
}

async function loadProject(id) {
  isLoadingProject.value = true
  try {
    project.value = await projectsStore.fetchProject(id)
    if (project.value) {
      uiStore.setLastProject(project.value.id, project.value.name)
      if (project.value.numberOfStoreys) form.value.numberOfStoreys = project.value.numberOfStoreys
    }
    const saved = await foundationStore.fetchCheck(id)
    if (saved?.input) Object.assign(form.value, saved.input)
  } catch {
    toast.error('Failed to load foundation check context')
  } finally {
    isLoadingProject.value = false
  }
}

watch(() => props.projectId, async (id) => {
  if (id) {
    await loadProject(id)
  } else if (!projectsStore.projects.length) {
    projectsStore.fetchProjects()
  }
}, { immediate: true })
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col print:h-auto print:block">
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden print:hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Foundation Compliance</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to run a foundation compliance check against SANS 10400-H.</p>
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

    <main v-else class="relative flex-1 min-h-0 flex flex-col gap-3">
      <div class="hidden print:block mb-6">
        <h1 class="text-xl font-bold">Foundation Compliance Check - SANS 10400-H:2010</h1>
        <p class="text-sm text-slate-600">{{ project?.name }} · Checked: {{ result?.checkedAt ? new Date(result.checkedAt).toLocaleDateString('en-ZA') : '-' }}</p>
      </div>

      <div class="pointer-events-none absolute inset-x-0 top-0 z-10 flex justify-center px-2 print:hidden">
        <div
          class="mt-1 rounded-xl border border-slate-200 bg-white shadow-sm px-4 py-2.5 flex items-center gap-2 transition-all duration-200"
          :class="isLoadingProject ? 'translate-y-0 opacity-100' : '-translate-y-1 opacity-0'"
          role="status"
          aria-live="polite"
        >
          <Loader2 class="h-3.5 w-3.5 animate-spin text-slate-400" />
          <span class="text-xs text-slate-500">Loading project...</span>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-4 gap-3 shrink-0 print:hidden">
        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-amber-50 flex items-center justify-center shrink-0">
            <HardHat class="h-4 w-4 text-amber-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Foundation Type</p>
            <p class="text-sm font-bold text-slate-900 leading-tight">{{ form.foundationType }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <CheckCircle2 class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Checks Passed</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ passCount }}<span class="text-xs font-normal text-slate-400"> / {{ totalCount }}</span></p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-rose-50 flex items-center justify-center shrink-0">
            <XCircle class="h-4 w-4 text-rose-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Require Attention</p>
            <p class="text-xl font-bold leading-tight" :class="failCount ? 'text-rose-700' : 'text-emerald-700'">{{ failCount }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-slate-100 flex items-center justify-center shrink-0">
            <RefreshCw class="h-4 w-4 text-slate-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Compliance Score</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ scorePercent }}<span class="text-xs font-normal text-slate-400">%</span></p>
          </div>
        </div>
      </div>

      <div class="flex-1 min-h-0 rounded-xl border border-slate-200 bg-white shadow-sm flex flex-col overflow-hidden">
        <div class="flex items-center justify-between px-4 py-3 border-b border-slate-100 shrink-0 print:hidden">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.2em] text-primary/70">Foundation Compliance</p>
            <p class="text-xs text-slate-500 mt-0.5">{{ project?.name ?? 'Project' }} · SANS 10400-H:2010</p>
          </div>

          <div class="flex items-center gap-2">
            <Button variant="outline" size="sm" @click="print" class="gap-1.5 text-xs h-8 bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-slate-100 dark:hover:bg-slate-700 text-slate-900 border-slate-200">
              <Printer class="h-3.5 w-3.5" /> Print
            </Button>
            <Button
              @click="runCheck"
              :disabled="isSaving || isLoading || !projectId"
              size="sm"
              class="gap-1.5 text-xs h-8 bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Loader2 v-if="isSaving || isLoading" class="h-3.5 w-3.5 animate-spin" />
              <RefreshCw v-else class="h-3.5 w-3.5" />
              Run Check
            </Button>
          </div>
        </div>

        <div class="flex-1 min-h-0 overflow-auto p-4">
          <div class="grid grid-cols-1 lg:grid-cols-[380px_1fr] gap-4">
            <section class="space-y-4 print:hidden">
              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Project Parameters</h2>

                <div class="space-y-4">
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Number of Storeys</Label>
                      <Select v-model="form.numberOfStoreys">
                        <SelectTrigger class="mt-1 h-9 text-sm">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem v-for="s in STOREY_OPTIONS" :key="s" :value="s">{{ s }}</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Foundation Type</Label>
                      <Select v-model="form.foundationType">
                        <SelectTrigger class="mt-1 h-9 text-sm">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem v-for="t in FOUNDATION_TYPES" :key="t" :value="t">{{ t }}</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div v-if="engineerRequired" class="rounded-lg bg-amber-50 border border-amber-200 p-3 text-xs text-amber-800">
                    <p class="font-semibold">Structural Engineer Required</p>
                    <p class="mt-0.5 text-amber-700">3+ storeys or non-strip foundation type requires a structural engineer. Minimum dimension checks below are for reference only - engineer's design governs.</p>
                    <div class="mt-2 flex items-center gap-2">
                      <Switch v-model="form.engineerAppointed" id="eng-apptd" />
                      <Label for="eng-apptd" class="text-xs text-amber-800">Engineer appointed</Label>
                    </div>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm" :class="{'opacity-60': engineerRequired}">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">
                  Strip Dimensions
                  <span class="ml-2 font-normal normal-case tracking-normal text-slate-400">SANS 10400-H Table H1</span>
                </h2>
                <p v-if="!engineerRequired" class="text-xs text-slate-500 mb-3">
                  Minimums for {{ form.numberOfStoreys }}-storey (normal soil, 50-75kPa):
                  External {{ currentMin.extW }}x{{ currentMin.extD }}mm · Internal {{ currentMin.intW }}x{{ currentMin.intD }}mm
                </p>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Ext. Width (mm)</Label>
                    <Input v-model.number="form.externalWidthMm" type="number" class="mt-1 h-9" :placeholder="currentMin.extW" />
                    <p class="text-xs mt-0.5" :class="form.externalWidthMm >= currentMin.extW ? 'text-emerald-600' : 'text-rose-600'">
                      Min {{ currentMin.extW }}mm
                    </p>
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Ext. Depth (mm)</Label>
                    <Input v-model.number="form.externalDepthMm" type="number" class="mt-1 h-9" :placeholder="currentMin.extD" />
                    <p class="text-xs mt-0.5" :class="form.externalDepthMm >= currentMin.extD ? 'text-emerald-600' : 'text-rose-600'">
                      Min {{ currentMin.extD }}mm
                    </p>
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Int. Width (mm)</Label>
                    <Input v-model.number="form.internalWidthMm" type="number" class="mt-1 h-9" :placeholder="currentMin.intW" />
                    <p class="text-xs mt-0.5" :class="form.internalWidthMm >= currentMin.intW ? 'text-emerald-600' : 'text-rose-600'">
                      Min {{ currentMin.intW }}mm
                    </p>
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Int. Depth (mm)</Label>
                    <Input v-model.number="form.internalDepthMm" type="number" class="mt-1 h-9" :placeholder="currentMin.intD" />
                    <p class="text-xs mt-0.5" :class="form.internalDepthMm >= currentMin.intD ? 'text-emerald-600' : 'text-rose-600'">
                      Min {{ currentMin.intD }}mm
                    </p>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Concrete & Waterproofing</h2>
                <div class="space-y-4">
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Concrete Strength (MPa)</Label>
                      <Input v-model.number="form.concreteMpa" type="number" class="mt-1 h-9" />
                      <p class="text-xs mt-0.5" :class="form.concreteMpa >= 15 ? 'text-emerald-600' : 'text-rose-600'">
                        Min 15MPa · 25MPa recommended
                      </p>
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Depth Below NGL (mm)</Label>
                      <Input v-model.number="form.belowNglMm" type="number" class="mt-1 h-9" />
                      <p class="text-xs mt-0.5" :class="form.belowNglMm >= 300 ? 'text-emerald-600' : 'text-rose-600'">
                        Min 300mm
                      </p>
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Finished Floor Above NGL (mm)</Label>
                      <Input v-model.number="form.finishedFloorAboveNglMm" type="number" class="mt-1 h-9" />
                      <p class="text-xs mt-0.5" :class="form.finishedFloorAboveNglMm >= 170 ? 'text-emerald-600' : 'text-rose-600'">
                        Min 170mm
                      </p>
                    </div>
                    <div v-if="form.dpcSpecified">
                      <Label class="text-xs font-medium text-slate-600">DPC Thickness (µm)</Label>
                      <Input v-model.number="form.dpcMicron" type="number" class="mt-1 h-9" />
                      <p class="text-xs mt-0.5" :class="form.dpcMicron >= 375 ? 'text-emerald-600' : 'text-rose-600'">
                        Min 375µm (SANS 952)
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Drawing Callouts</h2>
                <div class="space-y-3">
                  <div v-for="(item, key) in {
                    dpcSpecified: { label: 'DPC specified on drawing', clause: 'SANS 10400-B cl.3.2.1' },
                    brickForceSpecified: { label: 'Brick force specified every 2nd course', clause: 'SANS 10400-K cl.5.2.4' },
                    compactionSpec: { label: 'Backfill compaction spec noted (90% MOD, 150mm layers)', clause: 'SANS 10400-H cl.4.6' },
                    pestTreatmentSpecified: { label: 'Termite treatment with 10yr guarantee noted', clause: 'SANS 10400-H cl.4.7' },
                  }" :key="key" class="flex items-start gap-3">
                    <Switch :id="key" v-model="form[key]" class="mt-0.5" />
                    <div>
                      <Label :for="key" class="text-sm text-slate-700 cursor-pointer">{{ item.label }}</Label>
                      <p class="text-xs text-slate-400">{{ item.clause }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </section>

            <section class="space-y-4 min-w-0">
              <div v-if="(isLoading || isLoadingProject) && !result" class="rounded-xl border border-slate-200 bg-white p-12 text-center">
                <Loader2 class="h-8 w-8 mx-auto text-blue-500 animate-spin" />
              </div>

              <div v-else-if="!result" class="rounded-xl border border-dashed border-slate-300 bg-white p-12 text-center">
                <HardHat class="h-10 w-10 mx-auto text-slate-300 mb-3" />
                <p class="text-sm font-medium text-slate-600 mb-1">No check run yet</p>
                <p class="text-xs text-slate-400 mb-5">Fill in the foundation parameters and click <strong>Run Check</strong></p>
                <Button @click="runCheck" :disabled="isSaving || isLoading" size="sm" class="bg-blue-600 hover:bg-blue-700 text-white gap-1.5">
                  <Loader2 v-if="isSaving || isLoading" class="h-3.5 w-3.5 animate-spin" />
                  <RefreshCw v-else class="h-3.5 w-3.5" />
                  Run Foundation Check
                </Button>
              </div>

              <template v-else>
                <div class="rounded-xl border p-5 shadow-sm" :class="result.overallPass ? 'bg-emerald-50 border-emerald-200' : 'bg-rose-50 border-rose-200'">
                  <div class="flex items-center gap-4">
                    <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-full"
                      :class="result.overallPass ? 'bg-emerald-100' : 'bg-rose-100'">
                      <CheckCircle2 v-if="result.overallPass" class="h-7 w-7 text-emerald-600" />
                      <XCircle v-else class="h-7 w-7 text-rose-600" />
                    </div>
                    <div class="flex-1">
                      <p class="text-base font-bold" :class="result.overallPass ? 'text-emerald-800' : 'text-rose-800'">
                        {{ result.overallPass ? 'Compliant - SANS 10400-H' : 'Non-Compliant - Action Required' }}
                      </p>
                      <p class="text-sm mt-0.5" :class="result.overallPass ? 'text-emerald-600' : 'text-rose-600'">
                        {{ passCount }} of {{ totalCount }} checks passed
                        <span v-if="failCount > 0"> · {{ failCount }} require attention</span>
                      </p>
                    </div>
                    <div class="text-right">
                      <div class="text-2xl font-bold tabular-nums" :class="result.overallPass ? 'text-emerald-700' : 'text-rose-700'">
                        {{ scorePercent }}<span class="text-base">%</span>
                      </div>
                      <p class="text-xs text-slate-500">Pass rate</p>
                    </div>
                  </div>
                  <div class="mt-4 h-2 rounded-full bg-white/60">
                    <div class="h-2 rounded-full transition-all duration-700"
                      :class="result.overallPass ? 'bg-emerald-500' : 'bg-rose-500'"
                      :style="{ width: totalCount ? passCount / totalCount * 100 + '%' : '0%' }" />
                  </div>
                  <p class="mt-2 text-xs text-slate-500">
                    {{ result.results?.sansReference }} · Checked {{ new Date(result.checkedAt).toLocaleString('en-ZA') }}
                  </p>
                </div>

                <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
                  <div class="px-5 py-4 border-b border-slate-100">
                    <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500">Check Results</h2>
                  </div>
                  <div class="divide-y divide-slate-50">
                    <div v-for="(check, i) in result.results?.checks ?? []" :key="i"
                      class="px-5 py-4 flex items-start gap-4 transition-colors hover:bg-slate-50">
                      <div class="mt-0.5 shrink-0">
                        <CheckCircle2 v-if="check.passed" class="h-5 w-5 text-emerald-500" />
                        <XCircle v-else class="h-5 w-5 text-rose-500" />
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="flex items-center gap-2 flex-wrap">
                          <p class="text-sm font-semibold text-slate-900">{{ check.rule }}</p>
                          <span class="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium"
                            :class="checkTypeBadge(check.checkType)">
                            {{ check.checkType }}
                          </span>
                          <span v-if="!check.isMandatory" class="text-[10px] font-medium text-slate-400">Optional</span>
                        </div>
                        <p class="text-xs text-slate-500 mt-0.5">{{ check.description }}</p>
                        <div v-if="check.checkType === 'Numeric'" class="mt-2 flex items-center gap-4 text-xs">
                          <span class="font-mono font-semibold" :class="check.passed ? 'text-emerald-700' : 'text-rose-700'">
                            Provided: {{ check.providedValue }} {{ check.unit }}
                          </span>
                          <span class="text-slate-400">·</span>
                          <span class="text-slate-500 font-mono">Required: {{ check.requiredValue }} {{ check.unit }}</span>
                        </div>
                        <p class="text-[11px] text-slate-400 mt-1">{{ check.clauseReference }}</p>
                        <p v-if="check.note" class="text-[11px] text-amber-600 mt-1 italic">{{ check.note }}</p>
                      </div>
                    </div>
                  </div>
                </div>
              </template>
            </section>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
