<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  Loader2, HomeIcon, CheckCircle2, XCircle,
  RefreshCw, Printer,
  FolderOpen, Search, Building2, ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { useProjectsStore } from '@/stores/projects.store'
import { useRoofCheckStore } from '@/stores/roofCheck.store'
import { useUiStore } from '@/stores/ui.store'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })
const router = useRouter()
const projectsStore = useProjectsStore()
const roofCheckStore = useRoofCheckStore()
const uiStore = useUiStore()

const project = ref(null)
const isLoadingProject = ref(false)
const isSaving = ref(false)

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

// ── SANS minimum pitches per type
const ROOF_TYPES = [
  'Chromadek', 'IBR Sheeting', 'Corrugated', 'Clay Tiles',
  'Concrete Tiles', 'Slate', 'Thatch', 'Flat (waterproofed)',
]
const MIN_PITCH = {
  'Chromadek': 5, 'IBR Sheeting': 5, 'Corrugated': 8,
  'Clay Tiles': 17.5, 'Concrete Tiles': 17.5, 'Slate': 25,
  'Thatch': 45, 'Flat (waterproofed)': 1,
}

const form = ref({
  roofType: 'Chromadek',
  pitchDegrees: 30,
  trussType: 'Pre-fabricated SA Pine roof trusses',
  trussDesigner: 'Specialist roof engineer',
  purlinSpacingMm: 1000,
  overhangMm: 600,
  roofColour: 'Charcoal Grey',
  insulationSpecified: true,
  insulationRValue: 3.7,
  ridgePlateSpecified: true,
  fasciaSpecified: true,
  timberTreatmentSpecified: true,
  guttersSpecified: true,
  engineerCertRequired: true,
  manufacturerSpecReferenced: true,
})

const minPitch = computed(() => MIN_PITCH[form.value.roofType] ?? 5)
const pitchOk = computed(() => form.value.pitchDegrees >= minPitch.value)

const result = computed(() => roofCheckStore.getCheck(props.projectId))
const isLoading = computed(() => roofCheckStore.isLoading(props.projectId))

const CATEGORY_COLORS = {
  General: 'bg-blue-50 border-blue-200 text-blue-700',
  Structure: 'bg-amber-50 border-amber-200 text-amber-700',
  Materials: 'bg-emerald-50 border-emerald-200 text-emerald-700',
  Certification: 'bg-purple-50 border-purple-200 text-purple-700',
}

const CALLOUT_TOGGLES = {
  ridgePlateSpecified: { label: 'Ridge cap / plate specified', clause: 'SANS 10400-L cl.5.1' },
  fasciaSpecified: { label: 'Fascia board specified and painted', clause: 'SANS 10400-L cl.4.3' },
  timberTreatmentSpecified: { label: 'Timber treatment specified', clause: 'SANS 10005' },
  guttersSpecified: { label: 'Gutters and downpipes shown', clause: 'SANS 10400-P cl.5.1.1' },
  engineerCertRequired: { label: 'Engineer certificate required (noted)', clause: 'SANS 10400-K cl.6.2.2' },
  manufacturerSpecReferenced: { label: 'Manufacturer spec referenced', clause: 'SANS 10400-L cl.3.2' },
}

const grouped = computed(() => {
  if (!result.value?.results?.items) return {}
  return result.value.results.items.reduce((acc, item) => {
    (acc[item.category] = acc[item.category] ?? []).push(item)
    return acc
  }, {})
})

const passCount = computed(() => result.value?.results?.items?.filter(i => i.passed).length ?? 0)
const totalCount = computed(() => result.value?.results?.items?.length ?? 0)
const mandatoryFailCount = computed(
  () => result.value?.results?.items?.filter(i => i.isMandatory && !i.passed).length ?? 0
)
const scorePercent = computed(() => {
  if (!totalCount.value) return 0
  return Math.round((passCount.value / totalCount.value) * 100)
})

async function runCheck() {
  if (!props.projectId) return toast.error('No project selected')
  isSaving.value = true
  try {
    await roofCheckStore.evaluate(props.projectId, form.value)
    toast.success('Roof checklist complete')
  } catch {
    toast.error('Failed to run roof check')
  } finally {
    isSaving.value = false
  }
}

function print() { window.print() }

function selectProject(projectItem) {
  uiStore.setLastProject(projectItem.id, projectItem.name)
  router.push({ name: 'roofcheck', params: { projectId: projectItem.id } })
}

async function loadProject(id) {
  isLoadingProject.value = true
  try {
    project.value = await projectsStore.fetchProject(id)
    if (project.value) uiStore.setLastProject(project.value.id, project.value.name)
    const saved = await roofCheckStore.fetchCheck(id)
    if (saved?.input) Object.assign(form.value, saved.input)
  } catch {
    toast.error('Failed to load roof check context')
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
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Roof Checklist</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to run SANS 10400 roof checklist checks.</p>
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

    <main v-else class="relative flex-1 min-h-0 flex flex-col gap-3">
      <div class="hidden print:block mb-6">
        <h1 class="text-xl font-bold">Roof Callout Checklist - SANS 10400-L/K</h1>
        <p class="text-sm text-slate-600">{{ project?.name }} · Checked: {{ result?.checkedAt ? new Date(result.checkedAt).toLocaleDateString('en-ZA') : '—' }}</p>
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
          <div class="h-8 w-8 rounded-md bg-blue-50 flex items-center justify-center shrink-0">
            <HomeIcon class="h-4 w-4 text-blue-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Roof Type</p>
            <p class="text-sm font-bold text-slate-900 leading-tight">{{ form.roofType }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <CheckCircle2 class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Items Passed</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ passCount }}<span class="text-xs font-normal text-slate-400"> / {{ totalCount }}</span></p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-rose-50 flex items-center justify-center shrink-0">
            <XCircle class="h-4 w-4 text-rose-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Mandatory Missing</p>
            <p class="text-xl font-bold leading-tight" :class="mandatoryFailCount ? 'text-rose-700' : 'text-emerald-700'">{{ mandatoryFailCount }}</p>
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
            <p class="text-[10px] font-bold uppercase tracking-[0.2em] text-primary/70">Roof Checklist</p>
            <p class="text-xs text-slate-500 mt-0.5">{{ project?.name ?? 'Project' }} · SANS 10400-L:2011 / SANS 10400-K:2011</p>
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
          <div class="grid grid-cols-1 lg:grid-cols-[360px_1fr] gap-4">
            <section class="space-y-4 print:hidden">
              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Roof Properties</h2>
                <div class="space-y-4">
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Covering Type</Label>
                    <Select v-model="form.roofType">
                      <SelectTrigger class="mt-1 h-9 text-sm">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem v-for="t in ROOF_TYPES" :key="t" :value="t">{{ t }}</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Pitch (°)</Label>
                      <Input v-model.number="form.pitchDegrees" type="number" step="0.5" class="mt-1 h-9" />
                      <p class="text-xs mt-0.5" :class="pitchOk ? 'text-emerald-600' : 'text-rose-600'">
                        Min {{ minPitch }}° for {{ form.roofType }}
                      </p>
                    </div>
                    <div>
                      <Label class="text-xs font-medium text-slate-600">Overhang (mm)</Label>
                      <Input v-model.number="form.overhangMm" type="number" class="mt-1 h-9" />
                    </div>
                  </div>

                  <div>
                    <Label class="text-xs font-medium text-slate-600">Roof Colour</Label>
                    <Input v-model="form.roofColour" class="mt-1 h-9" placeholder="e.g. Charcoal Grey" />
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Structure</h2>
                <div class="space-y-3">
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Truss Type</Label>
                    <Input v-model="form.trussType" class="mt-1 h-9" placeholder="e.g. Pre-fabricated SA Pine" />
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Truss Designer / Engineer</Label>
                    <Input v-model="form.trussDesigner" class="mt-1 h-9" placeholder="e.g. Roof specialist" />
                  </div>
                  <div>
                    <Label class="text-xs font-medium text-slate-600">Purlin Spacing (mm CTC)</Label>
                    <Input v-model.number="form.purlinSpacingMm" type="number" class="mt-1 h-9" />
                    <p class="text-xs mt-0.5" :class="form.purlinSpacingMm <= 1500 ? 'text-emerald-600' : 'text-rose-600'">
                      Typically ≤1000mm for sheeting
                    </p>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Insulation & Energy</h2>
                <div class="space-y-4">
                  <div class="flex items-center gap-3">
                    <Switch id="insul" v-model="form.insulationSpecified" />
                    <Label for="insul" class="text-sm">Insulation specified on drawing</Label>
                  </div>
                  <div v-if="form.insulationSpecified">
                    <Label class="text-xs font-medium text-slate-600">Insulation R-Value</Label>
                    <Input v-model.number="form.insulationRValue" type="number" step="0.1" class="mt-1 h-9" />
                    <p class="text-xs mt-0.5" :class="form.insulationRValue >= 3.5 ? 'text-emerald-600' : 'text-amber-600'">
                      Typically R3.5-R5.5 for roofs (SANS 10400-XA Table XA2)
                    </p>
                  </div>
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 class="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">Drawing Callouts</h2>
                <div class="space-y-3">
                  <div v-for="(item, key) in CALLOUT_TOGGLES" :key="key" class="flex items-start gap-3">
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
                <HomeIcon class="h-10 w-10 mx-auto text-slate-300 mb-3" />
                <p class="text-sm font-medium text-slate-600 mb-1">No check run yet</p>
                <p class="text-xs text-slate-400 mb-5">Complete the form and click <strong>Run Check</strong></p>
                <Button @click="runCheck" :disabled="isSaving || isLoading" size="sm" class="bg-blue-600 hover:bg-blue-700 text-white gap-1.5">
                  <Loader2 v-if="isSaving || isLoading" class="h-3.5 w-3.5 animate-spin" />
                  <RefreshCw v-else class="h-3.5 w-3.5" />
                  Run Roof Checklist
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
                        {{ result.overallPass ? 'All Mandatory Callouts Present' : 'Missing Mandatory Callouts' }}
                      </p>
                      <p class="text-sm mt-0.5" :class="result.overallPass ? 'text-emerald-600' : 'text-rose-600'">
                        {{ passCount }} of {{ totalCount }} items present
                        <span v-if="mandatoryFailCount > 0"> · {{ mandatoryFailCount }} mandatory item(s) missing</span>
                      </p>
                    </div>
                    <div class="text-right">
                      <div class="text-2xl font-bold tabular-nums" :class="result.overallPass ? 'text-emerald-700' : 'text-rose-700'">
                        {{ scorePercent }}<span class="text-base">%</span>
                      </div>
                    </div>
                  </div>
                  <div class="mt-4 h-2 rounded-full bg-white/60">
                    <div class="h-2 rounded-full transition-all duration-700"
                      :class="result.overallPass ? 'bg-emerald-500' : 'bg-rose-500'"
                      :style="{ width: totalCount ? passCount / totalCount * 100 + '%' : '0%' }" />
                  </div>
                  <p class="mt-2 text-xs text-slate-500">
                    {{ result.results?.sansReference }} · Roof type: {{ result.roofType }}
                    · {{ new Date(result.checkedAt).toLocaleString('en-ZA') }}
                  </p>
                </div>

                <div v-for="(items, category) in grouped" :key="category"
                  class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
                  <div class="px-5 py-3 border-b border-slate-100 flex items-center gap-2">
                    <span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-[11px] font-bold border"
                      :class="CATEGORY_COLORS[category] ?? 'bg-slate-50 border-slate-200 text-slate-700'">
                      {{ category }}
                    </span>
                    <span class="text-xs text-slate-400">{{ items.filter(i => i.passed).length }} / {{ items.length }} passed</span>
                  </div>
                  <div class="divide-y divide-slate-50">
                    <div v-for="(item, idx) in items" :key="idx"
                      class="px-5 py-3.5 flex items-start gap-4 hover:bg-slate-50 transition-colors">
                      <div class="mt-0.5 shrink-0">
                        <CheckCircle2 v-if="item.passed" class="h-5 w-5 text-emerald-500" />
                        <XCircle v-else class="h-5 w-5 text-rose-500" />
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="flex items-center gap-2 flex-wrap">
                          <p class="text-sm font-semibold text-slate-900">{{ item.item }}</p>
                          <span v-if="!item.isMandatory" class="text-[10px] text-slate-400 font-medium">Optional</span>
                        </div>
                        <p class="text-xs text-slate-500 mt-0.5">{{ item.requirement }}</p>
                        <div class="flex items-center gap-3 mt-1">
                          <span v-if="item.providedValue" class="text-xs font-mono font-semibold text-emerald-700">
                            ✓ {{ item.providedValue }}
                          </span>
                          <span class="text-[11px] text-slate-400">{{ item.clauseReference }}</span>
                        </div>
                        <p v-if="item.note" class="text-[11px] text-amber-600 mt-1 italic">{{ item.note }}</p>
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
