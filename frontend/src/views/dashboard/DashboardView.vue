<script setup>
import { computed, reactive, ref, onMounted } from 'vue'
import {
  Building2,
  TrendingUp,
  AlertOctagon,
  Clock,
  BarChart3,
  Download,
  Plus,
  SlidersHorizontal,
  ChevronDown,
  CheckCircle2,
  XCircle,
  Trash2,
  Search,
  Loader2,
  ExternalLink,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'
import { useRouter } from 'vue-router'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import AppDatePicker from '@/components/ui/AppDatePicker.vue'
import WelcomeSection from './components/WelcomeSection.vue'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'

// --- STORES ---
const projectsStore = useProjectsStore()
const uiStore = useUiStore()
const router = useRouter()

// --- STATE ---
const searchQuery = ref('')
const deletingId = ref(null)

// Filter State
const startDate = ref('')
const endDate = ref('')

const filters = reactive({
  municipalities: [],
  status: [],
})

const expanded = reactive({
  date: true,
  municipalities: true,
  status: true,
  map: true,
})

// Status enum values matching the C# ProjectStatus enum
const STATUS_OPTIONS = ['Draft', 'InProgress', 'SubmittedToCouncil', 'Approved', 'Rejected', 'Revised']

const STATUS_LABELS = {
  Draft: 'Draft',
  InProgress: 'In Progress',
  SubmittedToCouncil: 'Submitted to Council',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Revised: 'Revised',
}

// --- LIFECYCLE ---
onMounted(() => projectsStore.fetchProjects())

// --- COMPUTED ---
const activeFilterCount = computed(
  () => filters.municipalities.length + filters.status.length,
)

const filteredProjects = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  return projectsStore.projects.filter((p) => {
    if (filters.municipalities.length && !filters.municipalities.includes(p.municipality)) return false
    if (filters.status.length && !filters.status.includes(p.status)) return false
    if (q && !p.name.toLowerCase().includes(q) && !p.municipality.toLowerCase().includes(q)) return false
    return true
  })
})

const stats = computed(() => ({
  total: projectsStore.stats.total,
  inCouncilCount: projectsStore.stats.inCouncilCount,
  rejectedCount: projectsStore.stats.rejectedCount,
  approvedCount: projectsStore.projects.filter((p) => p.status === 'Approved').length,
}))

const approvalRate = computed(() => {
  const decided = stats.value.approvedCount + stats.value.rejectedCount
  if (!decided) return null
  return Math.round((stats.value.approvedCount / decided) * 100)
})

const PIPELINE_STAGES = [
  { key: 'Draft',              label: 'Draft',      color: 'bg-slate-400',   textColor: 'text-slate-500',   bgColor: 'bg-slate-50',   borderColor: 'border-slate-200' },
  { key: 'InProgress',         label: 'In Progress', color: 'bg-blue-500',   textColor: 'text-blue-600',    bgColor: 'bg-blue-50',    borderColor: 'border-blue-100' },
  { key: 'SubmittedToCouncil', label: 'In Council',  color: 'bg-amber-400',  textColor: 'text-amber-600',   bgColor: 'bg-amber-50',   borderColor: 'border-amber-100' },
  { key: 'Revised',            label: 'Revised',     color: 'bg-violet-500', textColor: 'text-violet-600',  bgColor: 'bg-violet-50',  borderColor: 'border-violet-100' },
  { key: 'Approved',           label: 'Approved',    color: 'bg-emerald-500',textColor: 'text-emerald-600', bgColor: 'bg-emerald-50', borderColor: 'border-emerald-100' },
  { key: 'Rejected',           label: 'Rejected',    color: 'bg-rose-500',   textColor: 'text-rose-600',    bgColor: 'bg-rose-50',    borderColor: 'border-rose-100' },
]

const pipelineCounts = computed(() => {
  const counts = Object.fromEntries(PIPELINE_STAGES.map((s) => [s.key, 0]))
  for (const p of projectsStore.projects) {
    if (p.status in counts) counts[p.status]++
  }
  return counts
})

function stageWidth(key) {
  if (!stats.value.total) return 0
  return (pipelineCounts.value[key] / stats.value.total) * 100
}

// --- METHODS ---
const toggleFilterValue = (group, val) => {
  if (filters[group].includes(val)) {
    filters[group] = filters[group].filter((i) => i !== val)
  } else {
    filters[group].push(val)
  }
}

const clearFilters = () => {
  filters.municipalities = []
  filters.status = []
  startDate.value = ''
  endDate.value = ''
  searchQuery.value = ''
}

async function handleDelete(project) {
  if (!confirm(`Delete "${project.name}"? This cannot be undone.`)) return
  deletingId.value = project.id
  try {
    await projectsStore.removeProject(project.id)
    toast.success('Project deleted', { description: project.name })
  } catch (err) {
    toast.error('Failed to delete project', { description: err.message })
  } finally {
    deletingId.value = null
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 uppercase">Command Center</h1>
        <p class="text-sm text-slate-500">Portfolio Operations & Municipal Compliance Dashboard</p>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <Button variant="outline" class="gap-2 uppercase text-xs font-bold" disabled>
          <Download class="h-3.5 w-3.5" /> Export Report
        </Button>
        <Button
          class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold"
          @click="uiStore.openCreateProjectSheet()"
        >
          <Plus class="h-3.5 w-3.5" /> New Project
        </Button>
      </div>
    </header>

    <!-- Content Area Placeholder -->
    <div class="grid grid-cols-1 gap-6">
      <!-- Getting Started -->
      <WelcomeSection @create-project="uiStore.openCreateProjectSheet()" />

      <!-- Quick Metrics -->
      <section class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex justify-between items-start text-slate-500 mb-2">
            <Building2 class="h-5 w-5" />
            <span class="text-[10px] font-bold uppercase tracking-wider">Total Projects</span>
          </div>
          <div class="text-2xl font-bold text-slate-900">{{ stats.total }}</div>
          <div class="text-[11px] text-slate-500 mt-1">Across all municipalities</div>
        </div>

        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex justify-between items-start text-slate-500 mb-2">
            <Clock class="h-5 w-5 text-blue-500" />
            <span class="text-[10px] font-bold uppercase tracking-wider">In Council</span>
          </div>
          <div class="text-2xl font-bold text-slate-900">{{ stats.inCouncilCount }}</div>
          <div class="text-[11px] text-slate-500 mt-1">Submitted, awaiting decision</div>
        </div>

        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex justify-between items-start text-slate-500 mb-2">
            <AlertOctagon class="h-5 w-5 text-amber-500" />
            <span class="text-[10px] font-bold uppercase tracking-wider">Rejected</span>
          </div>
          <div class="text-2xl font-bold text-slate-900">{{ stats.rejectedCount }}</div>
          <div class="text-[11px] text-slate-500 mt-1">Requiring response</div>
        </div>

        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
          <div class="flex justify-between items-start text-slate-500 mb-2">
            <TrendingUp class="h-5 w-5 text-emerald-500" />
            <span class="text-[10px] font-bold uppercase tracking-wider">Approval Rate</span>
          </div>
          <div
            class="text-2xl font-bold"
            :class="approvalRate === null ? 'text-slate-300' : approvalRate >= 70 ? 'text-emerald-600' : approvalRate >= 40 ? 'text-amber-500' : 'text-rose-600'"
          >
            {{ approvalRate === null ? '—' : approvalRate + '%' }}
          </div>
          <div class="text-[11px] text-slate-500 mt-1">
            {{ approvalRate === null
              ? 'No decided applications yet'
              : `${stats.approvedCount} approved of ${stats.approvedCount + stats.rejectedCount} decided` }}
          </div>
        </div>
      </section>

      <!-- Dashboard Main Content -->
      <div class="grid grid-cols-1 xl:grid-cols-[300px_1fr] gap-6">
        <!-- Sidebar Filters -->
        <aside class="space-y-4">
          <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
            <div class="flex items-center justify-between mb-4">
              <h3
                class="text-[10px] font-bold uppercase tracking-widest text-slate-500 flex items-center gap-2"
              >
                <SlidersHorizontal class="h-3.5 w-3.5" /> Filters
              </h3>
              <button
                v-if="activeFilterCount > 0"
                class="text-[10px] font-bold text-blue-600 uppercase tracking-wider hover:underline"
                @click="clearFilters"
              >
                Clear
              </button>
            </div>

            <div class="space-y-6">
              <!-- Date Filter -->
              <div>
                <button
                  class="flex items-center justify-between w-full mb-2 group"
                  @click="expanded.date = !expanded.date"
                >
                  <span class="text-[13px] font-bold text-slate-700 uppercase tracking-tight"
                    >Submission Date</span
                  >
                  <ChevronDown
                    class="h-4 w-4 text-slate-400 transition-transform duration-200"
                    :class="!expanded.date && '-rotate-90'"
                  />
                </button>
                <div v-if="expanded.date" class="space-y-2 animate-in fade-in slide-in-from-top-1">
                  <AppDatePicker v-model="startDate" label="From" />
                  <AppDatePicker v-model="endDate" label="To" />
                </div>
              </div>

              <!-- Municipality Filter -->
              <div>
                <button
                  class="flex items-center justify-between w-full mb-2 group"
                  @click="expanded.municipalities = !expanded.municipalities"
                >
                  <span class="text-[13px] font-bold text-slate-700 uppercase tracking-tight"
                    >Municipality</span
                  >
                  <ChevronDown
                    class="h-4 w-4 text-slate-400 transition-transform duration-200"
                    :class="!expanded.municipalities && '-rotate-90'"
                  />
                </button>
                <div
                  v-if="expanded.municipalities"
                  class="space-y-2 max-h-40 overflow-y-auto pr-2 animate-in fade-in slide-in-from-top-1"
                >
                  <p v-if="projectsStore.byMunicipality.length === 0" class="text-[11px] text-slate-400 italic">No projects loaded</p>
                  <label
                    v-for="m in projectsStore.byMunicipality"
                    :key="m"
                    class="flex items-center gap-2 text-[13px] text-slate-600 cursor-pointer hover:text-slate-900 transition-colors"
                  >
                    <Checkbox
                      :checked="filters.municipalities.includes(m)"
                      @update:checked="toggleFilterValue('municipalities', m)"
                    />
                    {{ m }}
                  </label>
                </div>
              </div>

              <!-- Status Filter -->
              <div>
                <button
                  class="flex items-center justify-between w-full mb-2 group"
                  @click="expanded.status = !expanded.status"
                >
                  <span class="text-[13px] font-bold text-slate-700 uppercase tracking-tight"
                    >Approval Status</span
                  >
                  <ChevronDown
                    class="h-4 w-4 text-slate-400 transition-transform duration-200"
                    :class="!expanded.status && '-rotate-90'"
                  />
                </button>
                <div
                  v-if="expanded.status"
                  class="space-y-2 animate-in fade-in slide-in-from-top-1"
                >
                  <label
                    v-for="s in STATUS_OPTIONS"
                    :key="s"
                    class="flex items-center gap-2 text-[13px] text-slate-600 cursor-pointer hover:text-slate-900 transition-colors"
                  >
                    <Checkbox
                      :checked="filters.status.includes(s)"
                      @update:checked="toggleFilterValue('status', s)"
                    />
                    {{ STATUS_LABELS[s] }}
                  </label>
                </div>
              </div>
            </div>
          </div>
        </aside>

        <!-- Main Dashboard Panels -->
        <div class="space-y-6">
          <!-- Project Pipeline -->
          <section class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center justify-between mb-5">
              <h3 class="text-sm font-bold uppercase tracking-wider text-slate-800 flex items-center gap-2">
                <TrendingUp class="h-4 w-4 text-blue-600" /> Project Pipeline
              </h3>
              <span class="text-[11px] font-medium text-slate-400">
                {{ stats.total }} project{{ stats.total === 1 ? '' : 's' }} total
              </span>
            </div>

            <!-- Segmented progress bar -->
            <div class="flex h-2 rounded-full overflow-hidden gap-px bg-slate-100 mb-6">
              <template v-if="stats.total > 0">
                <div
                  v-for="stage in PIPELINE_STAGES"
                  :key="stage.key"
                  class="transition-all duration-700"
                  :class="stage.color"
                  :style="`width: ${stageWidth(stage.key)}%`"
                />
              </template>
            </div>

            <!-- Stage cards -->
            <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
              <div
                v-for="stage in PIPELINE_STAGES"
                :key="stage.key"
                class="rounded-lg border p-3"
                :class="[stage.bgColor, stage.borderColor]"
              >
                <div class="flex items-center gap-1.5 mb-2">
                  <span class="h-2 w-2 rounded-full flex-shrink-0" :class="stage.color" />
                  <span class="text-[10px] font-bold uppercase tracking-wider truncate" :class="stage.textColor">{{ stage.label }}</span>
                </div>
                <div class="text-2xl font-extrabold text-slate-800">
                  {{ pipelineCounts[stage.key] }}
                </div>
                <div class="text-[10px] text-slate-400 mt-0.5">
                  {{ stats.total ? Math.round(stageWidth(stage.key)) + '% of total' : '—' }}
                </div>
              </div>
            </div>
          </section>

          <!-- Data/Projects Table Panel -->
          <section class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div
              class="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50"
            >
              <h3
                class="text-sm font-bold uppercase tracking-wider text-slate-800 flex items-center gap-2"
              >
                <BarChart3 class="h-4 w-4 text-blue-600" /> Project Portfolio
              </h3>
              <div class="flex gap-2">
                <div class="relative">
                  <Search
                    class="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400"
                  />
                  <Input
                    v-model="searchQuery"
                    class="h-8 text-[12px] pl-7 w-48"
                    placeholder="Search projects..."
                  />
                </div>
              </div>
            </div>

            <!-- API error state -->
            <div
              v-if="projectsStore.error"
              class="flex items-center gap-3 p-4 m-4 bg-rose-50 border border-rose-200 rounded-lg text-sm text-rose-700"
            >
              <AlertOctagon class="h-4 w-4 flex-shrink-0" />
              {{ projectsStore.error }}
              <Button
                variant="ghost"
                size="sm"
                class="ml-auto text-rose-700 text-[11px] uppercase font-bold"
                @click="projectsStore.fetchProjects()"
              >Retry</Button>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left">
                <thead>
                  <tr
                    class="text-[10px] font-bold uppercase text-slate-400 border-b border-slate-100"
                  >
                    <th class="px-6 py-3">Project Name</th>
                    <th class="px-6 py-3">Municipality</th>
                    <th class="px-6 py-3">Zoning</th>
                    <th class="px-6 py-3">ERF</th>
                    <th class="px-6 py-3">Site Area</th>
                    <th class="px-6 py-3">Status</th>
                    <th class="px-6 py-3"></th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-50">
                  <!-- Loading skeleton -->
                  <template v-if="projectsStore.loading">
                    <tr v-for="n in 4" :key="'sk-' + n" class="animate-pulse">
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-40"></div></td>
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-28"></div></td>
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-24"></div></td>
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-16"></div></td>
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-16"></div></td>
                      <td class="px-6 py-4"><div class="h-3 bg-slate-100 rounded w-20"></div></td>
                      <td class="px-6 py-4"></td>
                    </tr>
                  </template>

                  <!-- Empty state -->
                  <tr v-else-if="filteredProjects.length === 0">
                    <td colspan="7" class="px-6 py-16 text-center">
                      <Building2 class="h-8 w-8 text-slate-200 mx-auto mb-3" />
                      <p class="text-sm font-medium text-slate-400">
                        {{ projectsStore.projects.length === 0 ? 'No projects yet — create your first one.' : 'No projects match the current filters.' }}
                      </p>
                      <Button
                        v-if="projectsStore.projects.length === 0"
                        class="mt-4 bg-blue-600 hover:bg-blue-700 uppercase text-xs font-bold gap-2"
                        @click="uiStore.openCreateProjectSheet()"
                      >
                        <Plus class="h-3.5 w-3.5" /> New Project
                      </Button>
                    </td>
                  </tr>

                  <!-- Live rows -->
                  <tr
                    v-for="p in filteredProjects"
                    :key="p.id"
                    class="hover:bg-slate-50/50 transition-colors"
                  >
                    <td class="px-6 py-4 text-sm font-bold text-slate-800">{{ p.name }}</td>
                    <td class="px-6 py-4 text-xs text-slate-500 font-medium">{{ p.municipality }}</td>
                    <td class="px-6 py-4 font-mono text-xs text-slate-600">{{ p.zoningScheme }}</td>
                    <td class="px-6 py-4 text-xs text-slate-500">{{ p.erf }}</td>
                    <td class="px-6 py-4 text-xs text-slate-500">{{ p.siteAreaM2?.toLocaleString() }} m²</td>
                    <td class="px-6 py-4">
                      <span
                        v-if="p.status === 'Approved'"
                        class="inline-flex items-center gap-1 text-[10px] font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full uppercase"
                      ><CheckCircle2 class="h-3 w-3" /> Approved</span>
                      <span
                        v-else-if="p.status === 'Rejected'"
                        class="inline-flex items-center gap-1 text-[10px] font-bold text-rose-600 bg-rose-50 px-2 py-0.5 rounded-full uppercase"
                      ><XCircle class="h-3 w-3" /> Rejected</span>
                      <span
                        v-else-if="p.status === 'SubmittedToCouncil'"
                        class="inline-flex items-center gap-1 text-[10px] font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full uppercase"
                      ><Clock class="h-3 w-3" /> In Council</span>
                      <span
                        v-else
                        class="inline-flex items-center gap-1 text-[10px] font-bold text-slate-600 bg-slate-100 px-2 py-0.5 rounded-full uppercase"
                      >{{ STATUS_LABELS[p.status] ?? p.status }}</span>
                    </td>
                    <td class="px-6 py-4 text-right">
                      <div class="flex items-center justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          class="text-slate-400 hover:text-blue-600 hover:bg-blue-50 h-7 w-7 p-0"
                          title="Open in Workbench"
                          @click="router.push({ name: 'workbench', params: { projectId: p.id } })"
                        >
                          <ExternalLink class="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          class="text-rose-500 hover:text-rose-700 hover:bg-rose-50 h-7 w-7 p-0"
                          :disabled="deletingId === p.id"
                          @click="handleDelete(p)"
                        >
                          <Loader2 v-if="deletingId === p.id" class="h-3.5 w-3.5 animate-spin" />
                          <Trash2 v-else class="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </div>
    </div>

    <!-- Create Project Drawer is rendered globally in AppLayout -->
  </div>
</template>
