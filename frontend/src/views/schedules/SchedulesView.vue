<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProjectsStore } from '@/stores/projects.store'
import { useSchedulesStore } from '@/stores/schedules.store'
import { useUiStore } from '@/stores/ui.store'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DoorOpen,
  Square,
  Download,
  Loader2,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  TableProperties,
  FolderOpen,
  Search,
  Building2,
  ArrowRight,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const props = defineProps({ projectId: { type: String, default: null } })

const router = useRouter()
const projectsStore = useProjectsStore()
const schedulesStore = useSchedulesStore()
const uiStore = useUiStore()

const isLoading = ref(false)
const activeTab = ref('doors')
const doorFilter = ref('')
const windowFilter = ref('')
const doorSort = ref({ key: 'mark', dir: 'asc' })
const windowSort = ref({ key: 'mark', dir: 'asc' })

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

const schedule = computed(() => schedulesStore.getSchedule(props.projectId))
const doors = computed(() => schedule.value?.doors ?? [])
const windows = computed(() => schedule.value?.windows ?? [])

const totalGlazingAreaM2 = computed(() => {
  const winArea = windows.value.reduce((s, w) => s + (w.areaM2 ?? 0), 0)
  const doorGlazing = doors.value
    .filter((d) => d.widthMm > 0 && d.heightMm > 0)
    .reduce((s, d) => s + (d.areaM2 ?? 0), 0)
  return parseFloat((winArea + doorGlazing).toFixed(2))
})

const projectName = computed(() => {
  const p = projectsStore.projects.find((p) => p.id === props.projectId)
  return p?.name ?? uiStore.lastProjectName ?? 'Project'
})

function filterRows(rows, query) {
  if (!query.trim()) return rows
  const q = query.toLowerCase()
  return rows.filter(
    (r) =>
      r.mark?.toLowerCase().includes(q) ||
      r.name?.toLowerCase().includes(q) ||
      r.type?.toLowerCase().includes(q) ||
      r.level?.toLowerCase().includes(q),
  )
}

function sortRows(rows, sortState) {
  const { key, dir } = sortState
  return [...rows].sort((a, b) => {
    const av = a[key] ?? ''
    const bv = b[key] ?? ''
    const cmp = typeof av === 'number'
      ? av - bv
      : String(av).localeCompare(String(bv), 'en-ZA', { numeric: true })
    return dir === 'asc' ? cmp : -cmp
  })
}

function toggleSort(sortState, key) {
  if (sortState.key === key) {
    sortState.dir = sortState.dir === 'asc' ? 'desc' : 'asc'
  } else {
    sortState.key = key
    sortState.dir = 'asc'
  }
}

const filteredDoors = computed(() =>
  sortRows(filterRows(doors.value, doorFilter.value), doorSort.value),
)
const filteredWindows = computed(() =>
  sortRows(filterRows(windows.value, windowFilter.value), windowSort.value),
)

const activeFilter = computed({
  get: () => (activeTab.value === 'doors' ? doorFilter.value : windowFilter.value),
  set: (v) => {
    if (activeTab.value === 'doors') {
      doorFilter.value = v
    } else {
      windowFilter.value = v
    }
  },
})

const activeRows = computed(() =>
  activeTab.value === 'doors' ? filteredDoors.value : filteredWindows.value,
)

function sortIcon(sortState, key) {
  if (sortState.key !== key) return ArrowUpDown
  return sortState.dir === 'asc' ? ArrowUp : ArrowDown
}

async function loadSchedule() {
  if (!props.projectId) return
  isLoading.value = true
  try {
    await schedulesStore.fetchSchedule(props.projectId, false)
  } catch {
    toast.error('Failed to load schedule')
  } finally {
    isLoading.value = false
  }
}

watch(() => props.projectId, (id) => {
  if (id) loadSchedule()
}, { immediate: true })

onMounted(() => {
  if (!projectsStore.projects.length) projectsStore.fetchProjects()
})

function toCsv(rows) {
  const header = ['Mark', 'Name', 'Type', 'Width (mm)', 'Height (mm)', 'Area (m²)', 'Level']
  const lines = rows.map((r) => [
    csvEsc(r.mark),
    csvEsc(r.name),
    csvEsc(r.type),
    r.widthMm ?? '',
    r.heightMm ?? '',
    r.areaM2 ?? '',
    csvEsc(r.level),
  ].join(','))
  return [header.join(','), ...lines].join('\r\n')
}

function csvEsc(v) {
  if (v == null || v === '') return ''
  const s = String(v)
  return s.includes(',') || s.includes('"') || s.includes('\n')
    ? `"${s.replace(/"/g, '""')}"`
    : s
}

function exportCsv(rows, type) {
  const csv = toCsv(rows)
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${projectName.value.replace(/[^a-z0-9]/gi, '_')}_${type}_schedule.csv`
  a.click()
  URL.revokeObjectURL(url)
}

function exportActiveCsv() {
  exportCsv(activeRows.value, activeTab.value === 'doors' ? 'door' : 'window')
}

function goToWorkbench() {
  if (props.projectId) {
    router.push({ name: 'workbench', params: { projectId: props.projectId } })
  } else {
    router.push({ name: 'workbench' })
  }
}

function selectProject(project) {
  uiStore.setLastProject(project.id, project.name)
  router.push({ name: 'schedules', params: { projectId: project.id } })
}
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">
    <div v-if="!projectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Schedules</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to view extracted door and window schedules.</p>
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

    <main v-else class="flex-1 min-h-0 flex flex-col gap-3">
      <!-- Standalone stat cards -->
      <div v-if="schedule" class="grid grid-cols-1 md:grid-cols-3 gap-3 shrink-0">
        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-violet-50 flex items-center justify-center shrink-0">
            <DoorOpen class="h-4 w-4 text-violet-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Doors</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ schedule.doorCount }}</p>
          </div>
        </div>
        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-sky-50 flex items-center justify-center shrink-0">
            <Square class="h-4 w-4 text-sky-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Windows</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ schedule.windowCount }}</p>
          </div>
        </div>
        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <TableProperties class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Total Glazing</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">
              {{ totalGlazingAreaM2 }} <span class="text-xs font-normal text-slate-400">m²</span>
            </p>
          </div>
        </div>
      </div>

      <!-- Table panel -->
      <div class="flex-1 min-h-0 rounded-xl border border-slate-200 bg-white shadow-sm flex flex-col overflow-hidden">
        <div v-if="isLoading" class="h-full flex flex-col items-center justify-center gap-3">
          <Loader2 class="h-6 w-6 animate-spin text-slate-400" />
          <p class="text-sm text-slate-500">Loading schedule data...</p>
        </div>

        <div
          v-else-if="!schedule"
          class="h-full flex flex-col items-center justify-center gap-4 p-8 text-center"
        >
          <TableProperties class="h-12 w-12 text-slate-200" />
          <div>
            <h3 class="text-sm font-semibold text-slate-700">No schedule extracted yet</h3>
            <p class="text-xs text-slate-400 mt-1 max-w-xs">
              Open the workbench and load an IFC model. Door and window schedules are
              extracted automatically and saved here.
            </p>
          </div>
          <Button size="sm" class="gap-1.5" @click="goToWorkbench">
            <Building2 class="h-4 w-4" />
            Go to Workbench
          </Button>
        </div>

        <div v-else class="flex-1 min-h-0 flex flex-col overflow-hidden">
          <!-- Controls row: Tabs only wraps the tab switcher, NOT the content area -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-slate-100 shrink-0">
            <Tabs v-model="activeTab">
              <TabsList class="h-9 shrink-0">
                <TabsTrigger value="doors" class="text-xs gap-1.5 px-3">
                  <DoorOpen class="h-3.5 w-3.5" />
                  Doors
                  <Badge variant="secondary" class="ml-1 h-4 px-1.5 text-[10px] leading-none">
                    {{ filteredDoors.length }}
                  </Badge>
                </TabsTrigger>
                <TabsTrigger value="windows" class="text-xs gap-1.5 px-3">
                  <Square class="h-3.5 w-3.5" />
                  Windows
                  <Badge variant="secondary" class="ml-1 h-4 px-1.5 text-[10px] leading-none">
                    {{ filteredWindows.length }}
                  </Badge>
                </TabsTrigger>
              </TabsList>
            </Tabs>

            <div class="flex items-center gap-2 min-w-0 ml-3">
              <div class="relative w-56 max-w-[40vw]">
                <Search class="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
                <Input
                  v-model="activeFilter"
                  placeholder="Filter by mark, type or level..."
                  class="pl-8 h-8 text-xs"
                />
              </div>
              <Button
                variant="outline"
                size="sm"
                class="gap-1.5 text-xs h-8"
                :disabled="!activeRows.length"
                @click="exportActiveCsv"
              >
                <Download class="h-3.5 w-3.5" />
                Export CSV
              </Button>
            </div>
          </div>

          <!-- Table scroll area: plain div, no Radix components in the height chain -->
          <div class="flex-1 min-h-0 overflow-auto p-4">
            <div class="rounded-lg border border-slate-200">
              <!-- Doors table -->
              <Table v-if="activeTab === 'doors'">
                <TableHeader>
                  <TableRow class="bg-slate-50 hover:bg-slate-50">
                    <TableHead
                      v-for="col in [
                        { key: 'mark', label: 'Mark' },
                        { key: 'name', label: 'Name / Description' },
                        { key: 'type', label: 'Type' },
                        { key: 'widthMm', label: 'W (mm)' },
                        { key: 'heightMm', label: 'H (mm)' },
                        { key: 'areaM2', label: 'Area (m²)' },
                        { key: 'level', label: 'Level' },
                      ]"
                      :key="col.key"
                      class="text-[11px] font-semibold text-slate-600 whitespace-nowrap cursor-pointer select-none"
                      @click="toggleSort(doorSort, col.key)"
                    >
                      <span class="flex items-center gap-1">
                        {{ col.label }}
                        <component :is="sortIcon(doorSort, col.key)" class="h-3 w-3 text-slate-400" />
                      </span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow
                    v-for="(door, i) in filteredDoors"
                    :key="door.localId ?? i"
                    class="hover:bg-violet-50/40"
                  >
                    <TableCell class="font-mono text-xs font-semibold text-slate-800">
                      {{ door.mark || '—' }}
                    </TableCell>
                    <TableCell class="text-xs text-slate-600 max-w-[200px] truncate">
                      {{ door.name || '—' }}
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="outline"
                        class="text-[10px] px-1.5 py-0 capitalize border-violet-200 text-violet-700 bg-violet-50"
                      >
                        {{ door.type || 'N/A' }}
                      </Badge>
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ door.widthMm > 0 ? door.widthMm.toLocaleString() : '—' }}
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ door.heightMm > 0 ? door.heightMm.toLocaleString() : '—' }}
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ door.areaM2 > 0 ? door.areaM2.toFixed(2) : '—' }}
                    </TableCell>
                    <TableCell class="text-xs text-slate-500">
                      {{ door.level || '—' }}
                    </TableCell>
                  </TableRow>
                  <TableRow v-if="!filteredDoors.length">
                    <TableCell colspan="7" class="text-center text-sm text-slate-400 py-12">
                      {{ doors.length ? 'No results match your filter.' : 'No doors extracted from the IFC model.' }}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>

              <!-- Windows table -->
              <Table v-else-if="activeTab === 'windows'">
                <TableHeader>
                  <TableRow class="bg-slate-50 hover:bg-slate-50">
                    <TableHead
                      v-for="col in [
                        { key: 'mark', label: 'Mark' },
                        { key: 'name', label: 'Name / Description' },
                        { key: 'type', label: 'Type' },
                        { key: 'widthMm', label: 'W (mm)' },
                        { key: 'heightMm', label: 'H (mm)' },
                        { key: 'areaM2', label: 'Area (m²)' },
                        { key: 'level', label: 'Level' },
                      ]"
                      :key="col.key"
                      class="text-[11px] font-semibold text-slate-600 whitespace-nowrap cursor-pointer select-none"
                      @click="toggleSort(windowSort, col.key)"
                    >
                      <span class="flex items-center gap-1">
                        {{ col.label }}
                        <component :is="sortIcon(windowSort, col.key)" class="h-3 w-3 text-slate-400" />
                      </span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow
                    v-for="(win, i) in filteredWindows"
                    :key="win.localId ?? i"
                    class="hover:bg-sky-50/40"
                  >
                    <TableCell class="font-mono text-xs font-semibold text-slate-800">
                      {{ win.mark || '—' }}
                    </TableCell>
                    <TableCell class="text-xs text-slate-600 max-w-[200px] truncate">
                      {{ win.name || '—' }}
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="outline"
                        class="text-[10px] px-1.5 py-0 capitalize border-sky-200 text-sky-700 bg-sky-50"
                      >
                        {{ win.type || 'N/A' }}
                      </Badge>
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ win.widthMm > 0 ? win.widthMm.toLocaleString() : '—' }}
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ win.heightMm > 0 ? win.heightMm.toLocaleString() : '—' }}
                    </TableCell>
                    <TableCell class="text-xs tabular-nums text-right">
                      {{ win.areaM2 > 0 ? win.areaM2.toFixed(2) : '—' }}
                    </TableCell>
                    <TableCell class="text-xs text-slate-500">
                      {{ win.level || '—' }}
                    </TableCell>
                  </TableRow>
                  <TableRow v-if="!filteredWindows.length">
                    <TableCell colspan="7" class="text-center text-sm text-slate-400 py-12">
                      {{ windows.length ? 'No results match your filter.' : 'No windows extracted from the IFC model.' }}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>