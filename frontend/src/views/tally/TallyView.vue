<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  Loader2,
  Zap,
  AlertTriangle,
  Download,
  RefreshCw,
  Lightbulb,
  PlugZap,
  Droplets,
  Wind,
  ShieldAlert,
  Package,
  ArrowUpDown,
  FolderOpen,
  Search,
  Building2,
  ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { useTallyStore } from '@/stores/tally.store'
import { toast } from 'vue-sonner'

const props = defineProps({
  projectId: { type: String, default: null },
})

const router = useRouter()
const projectsStore = useProjectsStore()
const uiStore = useUiStore()
const tallyStore = useTallyStore()

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

const filterText = ref('')
const filterCategory = ref('All')
const sortKey = ref('category')
const sortAsc = ref(true)

// ── Category config ──────────────────────────────────────────────────────────
const CATEGORIES = [
  { key: 'Lighting',   label: 'Lighting',    icon: Lightbulb,  color: 'text-amber-500',   bg: 'bg-amber-50 border-amber-200'   },
  { key: 'Electrical', label: 'Electrical',  icon: PlugZap,    color: 'text-blue-500',    bg: 'bg-blue-50 border-blue-200'     },
  { key: 'Sanitary',   label: 'Sanitary',    icon: Droplets,   color: 'text-cyan-500',    bg: 'bg-cyan-50 border-cyan-200'     },
  { key: 'HVAC',       label: 'HVAC',        icon: Wind,       color: 'text-emerald-500', bg: 'bg-emerald-50 border-emerald-200'},
  { key: 'Fire',       label: 'Fire',        icon: ShieldAlert,color: 'text-rose-500',    bg: 'bg-rose-50 border-rose-200'     },
  { key: 'Other',      label: 'Other',       icon: Package,    color: 'text-slate-500',   bg: 'bg-slate-50 border-slate-200'   },
]

// ── Data ─────────────────────────────────────────────────────────────────────
const tally = computed(() => tallyStore.getTally(props.projectId))
const isLoading = computed(() => tallyStore.isLoading(props.projectId))
const allItems = computed(() => tally.value?.items ?? [])

// ── Summary counts ────────────────────────────────────────────────────────────
const summaryByCategory = computed(() => {
  const counts = {}
  for (const cat of CATEGORIES) counts[cat.key] = 0
  for (const item of allItems.value) {
    if (counts[item.category] !== undefined) counts[item.category]++
    else counts['Other'] = (counts['Other'] ?? 0) + 1
  }
  return counts
})

// ── Filtered + sorted items ───────────────────────────────────────────────────
const filteredItems = computed(() => {
  let items = allItems.value

  if (filterCategory.value !== 'All') {
    items = items.filter((i) => i.category === filterCategory.value)
  }

  const q = filterText.value.trim().toLowerCase()
  if (q) {
    items = items.filter(
      (i) =>
        i.name?.toLowerCase().includes(q) ||
        i.type?.toLowerCase().includes(q) ||
        i.mark?.toLowerCase().includes(q) ||
        i.ifcType?.toLowerCase().includes(q) ||
        i.level?.toLowerCase().includes(q),
    )
  }

  const key = sortKey.value
  return [...items].sort((a, b) => {
    const av = (a[key] ?? '').toString().toLowerCase()
    const bv = (b[key] ?? '').toString().toLowerCase()
    const cmp = av.localeCompare(bv)
    return sortAsc.value ? cmp : -cmp
  })
})

// ── Grouped view — category → type → items ────────────────────────────────────
const groupedItems = computed(() => {
  const groups = {}
  for (const item of filteredItems.value) {
    const cat = item.category ?? 'Other'
    const type = item.type || item.ifcType || item.name || 'Unknown'
    if (!groups[cat]) groups[cat] = {}
    if (!groups[cat][type]) groups[cat][type] = []
    groups[cat][type].push(item)
  }
  return groups
})

// Level breakdown per group
function levelBreakdown(items) {
  const counts = {}
  for (const item of items) {
    const level = item.level || 'Ground Floor'
    counts[level] = (counts[level] ?? 0) + 1
  }
  return Object.entries(counts).sort(([a], [b]) => a.localeCompare(b))
}

function catConfig(key) {
  return CATEGORIES.find((c) => c.key === key) ?? CATEGORIES[5]
}

function toggleSort(key) {
  if (sortKey.value === key) {
    sortAsc.value = !sortAsc.value
  } else {
    sortKey.value = key
    sortAsc.value = true
  }
}

// ── Data loading ─────────────────────────────────────────────────────────────
onMounted(async () => {
  if (props.projectId) {
    await loadProject(props.projectId)
  } else {
    projectsStore.fetchProjects()
  }
})

watch(() => props.projectId, (id) => { if (id) loadProject(id) })

async function loadProject(id) {
  isLoadingProject.value = true
  try {
    project.value = await projectsStore.fetchProject(id)
    await tallyStore.fetchTally(id)
  } catch (err) {
    toast.error('Could not load project', { description: err.message })
  } finally {
    isLoadingProject.value = false
  }
}

async function refresh() {
  if (!props.projectId) return
  await tallyStore.fetchTally(props.projectId, true)
}

// ── CSV export ───────────────────────────────────────────────────────────────
function exportCsv() {
  const headers = ['Category', 'IFC Type', 'Mark', 'Name', 'Type', 'Level']
  const rows = filteredItems.value.map((i) => [
    i.category, i.ifcType, i.mark, i.name, i.type, i.level,
  ])
  const BOM = '\uFEFF'
  const csvContent =
    BOM +
    [headers, ...rows]
      .map((r) => r.map((v) => `"${(v ?? '').replace(/"/g, '""')}"`).join(','))
      .join('\r\n')

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `FixtureTally_${project.value?.name?.replace(/\s+/g, '_') ?? 'export'}.csv`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">

    <!-- No project: inline project picker -->
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Fixture Tally</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to view its fixture and equipment tally.</p>
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
              @click="router.push({ name: 'tally', params: { projectId: p.id } })"
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
          <Loader2 class="h-3.5 w-3.5 animate-spin text-amber-400" />
          <span class="text-xs text-slate-500">Loading project...</span>
        </div>
      </div>

      <div v-if="project" class="space-y-4 pb-6">

        <!-- Print header -->
        <div class="hidden print:block mb-6">
          <h2 class="text-2xl font-bold text-slate-900">Fixture Tally (Mini-BOQ)</h2>
          <p class="text-sm text-slate-600 mt-1">Project: <strong>{{ project.name }}</strong> · Generated {{ new Date().toLocaleDateString('en-ZA') }}</p>
          <hr class="mt-4" />
        </div>

        <!-- ── No tally yet ─────────────────────────────────────────────────── -->
        <div
          v-if="!isLoading && !allItems.length"
          class="bg-white border border-dashed border-slate-200 rounded-xl p-10 flex flex-col items-center gap-4 text-center print:hidden"
        >
          <Zap class="h-12 w-12 text-slate-200" />
          <p class="text-base font-bold text-slate-500">No fixture tally extracted yet</p>
          <p class="text-sm text-slate-400 max-w-sm leading-relaxed">
            Open the Workbench, load this project's IFC model, and the fixture tally will be
            automatically extracted and saved. Supported types include light fixtures, outlets,
            sanitary terminals, HVAC terminals, and fire suppression devices.
          </p>
          <Button size="sm" class="mt-1 gap-1.5" variant="outline" @click="router.push({ name: 'workbench', params: { projectId } })">
            Go to Workbench
          </Button>
        </div>

        <template v-else-if="allItems.length">
          <!-- ── Summary cards ─────────────────────────────────────────────────── -->
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          <div
            v-for="cat in CATEGORIES"
            :key="cat.key"
            class="border rounded-xl p-4 flex flex-col items-center gap-1 cursor-pointer transition-all"
            :class="[cat.bg, filterCategory === cat.key ? 'ring-2 ring-offset-1 ring-blue-400' : '']"
            @click="filterCategory = filterCategory === cat.key ? 'All' : cat.key"
          >
            <component :is="cat.icon" class="h-5 w-5" :class="cat.color" />
            <p class="text-2xl font-mono font-black text-slate-800 mt-1">{{ summaryByCategory[cat.key] ?? 0 }}</p>
            <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">{{ cat.label }}</p>
          </div>
          </div>

          <!-- ── Toolbar (total + filters + actions) ──────────────────────────── -->
          <!-- print: show compact total only -->
          <div class="hidden print:flex items-center gap-3 mb-2">
            <span class="text-xs font-bold text-slate-500 uppercase tracking-widest">Total Fixtures</span>
            <span class="text-lg font-mono font-black text-slate-900">{{ tally?.totalCount ?? allItems.length }}</span>
          </div>
          <div class="flex flex-wrap items-center gap-2 bg-white border border-slate-200 rounded-xl px-4 py-2 print:hidden">
            <!-- Total -->
            <div class="flex items-center gap-2 shrink-0">
              <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Total</span>
              <span class="text-base font-mono font-black text-slate-900">{{ tally?.totalCount ?? allItems.length }}</span>
              <span class="text-[10px] text-slate-400 italic hidden sm:inline">
                · {{ tally?.extractedAt ? new Date(tally.extractedAt).toLocaleString('en-ZA') : '—' }}
              </span>
            </div>
            <div class="h-5 w-px bg-slate-200 shrink-0 mx-1" />
            <!-- Search -->
            <Input
              v-model="filterText"
              placeholder="Search name, type, mark, level…"
              class="h-8 text-sm w-56"
            />
            <!-- Category filter -->
            <Select v-model="filterCategory">
              <SelectTrigger class="h-8 w-40 text-sm"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="All">All categories</SelectItem>
                <SelectItem v-for="cat in CATEGORIES" :key="cat.key" :value="cat.key">{{ cat.label }}</SelectItem>
              </SelectContent>
            </Select>
            <span class="text-xs text-slate-400">{{ filteredItems.length }} items</span>
            <!-- Actions -->
            <div class="ml-auto flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                class="h-8 gap-1.5 text-[10px] font-bold uppercase px-3 bg-white dark:bg-slate-800 dark:border-slate-700 dark:text-slate-100 dark:hover:bg-slate-700 text-slate-900 border-slate-200"
                :disabled="isLoading"
                @click="refresh"
              >
                <RefreshCw class="h-3.5 w-3.5" :class="{ 'animate-spin': isLoading }" />
                Refresh
              </Button>
              <Button
                size="sm"
                class="h-8 gap-1.5 bg-slate-900 dark:bg-slate-800 hover:bg-slate-800 dark:hover:bg-slate-700 text-white dark:text-slate-100 text-[10px] font-bold uppercase px-3"
                :disabled="!allItems.length"
                @click="exportCsv"
              >
                <Download class="h-3.5 w-3.5" />
                Export CSV
              </Button>
            </div>
          </div>

          <!-- ── Grouped BOQ table ──────────────────────────────────────────────── -->
          <div
            v-for="(types, catKey) in groupedItems"
            :key="catKey"
            class="bg-white border border-slate-200 rounded-xl overflow-hidden"
          >
            <!-- Category header -->
            <div
              class="flex items-center gap-3 px-5 py-3 border-b border-slate-100"
              :class="catConfig(catKey).bg"
            >
              <component :is="catConfig(catKey).icon" class="h-4 w-4 shrink-0" :class="catConfig(catKey).color" />
              <span class="text-sm font-bold text-slate-800">{{ catKey }}</span>
              <span class="ml-auto text-xs font-mono font-bold text-slate-600">
                {{ Object.values(types).reduce((s, arr) => s + arr.length, 0) }} items
              </span>
            </div>

            <!-- Types within this category -->
            <div
              v-for="(items, typeName) in types"
              :key="typeName"
              class="border-b border-slate-50 last:border-0"
            >
              <!-- Type row -->
              <div class="flex items-center justify-between px-5 py-2.5 bg-slate-50/50">
                <div class="flex items-center gap-3">
                  <span class="text-xs font-bold text-slate-700">{{ typeName }}</span>
                  <span class="text-[10px] text-slate-400 font-mono">{{ items[0]?.ifcType }}</span>
                </div>
                <div class="flex items-center gap-4">
                  <!-- Level breakdown pills -->
                  <div class="flex items-center gap-1.5 flex-wrap">
                    <span
                      v-for="[level, count] in levelBreakdown(items)"
                      :key="level"
                      class="text-[9px] font-bold bg-white border border-slate-200 text-slate-600 rounded-full px-2 py-0.5"
                    >
                      {{ level }}: {{ count }}
                    </span>
                  </div>
                  <span class="text-sm font-mono font-black text-slate-900 w-8 text-right">{{ items.length }}</span>
                </div>
              </div>

              <!-- Detail rows (collapsible by default — shown on print) -->
              <table class="w-full text-xs hidden print:table">
                <thead>
                  <tr class="border-b border-slate-100 text-[10px] font-bold text-slate-400 uppercase">
                    <th class="py-1.5 px-5 text-left">Mark</th>
                    <th class="py-1.5 text-left">Name</th>
                    <th class="py-1.5 text-left">Level</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in items" :key="item.localId" class="border-b border-slate-50">
                    <td class="py-1.5 px-5 font-mono text-slate-700">{{ item.mark }}</td>
                    <td class="py-1.5 text-slate-600">{{ item.name || '—' }}</td>
                    <td class="py-1.5 text-slate-500">{{ item.level }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- ── Flat detail table (screen, all rows sortable) ──────────────────── -->
          <div class="bg-white border border-slate-200 rounded-xl overflow-hidden print:hidden">
            <div class="flex items-center justify-between px-5 py-3 border-b border-slate-100">
              <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">Full Item List</p>
              <p class="text-xs text-slate-400">Click column headers to sort</p>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-xs">
                <thead>
                  <tr class="border-b border-slate-100 text-[10px] font-bold text-slate-400 uppercase">
                    <th
                      v-for="col in [
                        { key: 'category', label: 'Category' },
                        { key: 'ifcType',  label: 'IFC Type'  },
                        { key: 'mark',     label: 'Mark'      },
                        { key: 'name',     label: 'Name'      },
                        { key: 'type',     label: 'Type'      },
                        { key: 'level',    label: 'Level'     },
                      ]"
                      :key="col.key"
                      class="py-2.5 px-4 text-left cursor-pointer hover:text-slate-700 select-none"
                      @click="toggleSort(col.key)"
                    >
                      <span class="flex items-center gap-1">
                        {{ col.label }}
                        <ArrowUpDown
                          class="h-3 w-3 opacity-40"
                          :class="{ 'opacity-100 text-blue-500': sortKey === col.key }"
                        />
                      </span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="item in filteredItems"
                    :key="item.localId"
                    class="border-b border-slate-50 hover:bg-slate-50 transition-colors"
                  >
                    <td class="py-2 px-4">
                      <span
                        class="inline-flex items-center gap-1 text-[10px] font-bold rounded-full px-2 py-0.5 border"
                        :class="catConfig(item.category).bg"
                      >
                        <component :is="catConfig(item.category).icon" class="h-2.5 w-2.5" :class="catConfig(item.category).color" />
                        <span :class="catConfig(item.category).color">{{ item.category }}</span>
                      </span>
                    </td>
                    <td class="py-2 px-4 font-mono text-slate-500">{{ item.ifcType }}</td>
                    <td class="py-2 px-4 font-mono font-bold text-slate-700">{{ item.mark }}</td>
                    <td class="py-2 px-4 text-slate-700">{{ item.name || '—' }}</td>
                    <td class="py-2 px-4 text-slate-500">{{ item.type || '—' }}</td>
                    <td class="py-2 px-4 text-slate-500">{{ item.level }}</td>
                  </tr>
                  <tr v-if="!filteredItems.length">
                    <td colspan="6" class="py-8 text-center text-slate-400">No items match the current filter</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>

      </div>
    </main>
  </div>
</template>

<style>
@media print {
  header, .print\:hidden { display: none !important; }
  .print\:block { display: block !important; }
  .print\:table { display: table !important; }
  body { background: white; }
}
</style>
