<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import {
  History, Search, Filter, AlertOctagon, Clock3,
  Download, Loader2, CheckCircle2, FolderOpen,
  Building2, ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'
import { rejectionsService } from '@/services/rejectionsService'
import { toast } from 'vue-sonner'

const projectsStore = useProjectsStore()
const uiStore = useUiStore()

const activeProjectId = ref(uiStore.lastProjectId)
const pickerSearch = ref('')

const query = ref('')
const statusFilter = ref('All')

const items = ref([])
const isLoading = ref(false)
const updatingId = ref(null)

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

const filteredItems = computed(() => {
  const q = query.value.trim().toLowerCase()

  return items.value
    .filter((item) => (statusFilter.value === 'All' ? true : item.status === statusFilter.value))
    .filter((item) => {
      if (!q) return true
      return (
        item.sourceDocument.toLowerCase().includes(q) ||
        item.clauseReference.toLowerCase().includes(q) ||
        item.commentText.toLowerCase().includes(q) ||
        item.parsedAction.toLowerCase().includes(q) ||
        item.category.toLowerCase().includes(q)
      )
    })
    .sort((a, b) => new Date(b.receivedAt ?? 0) - new Date(a.receivedAt ?? 0))
})

const totalCount = computed(() => items.value.length)
const openCount = computed(() => items.value.filter((item) => item.status === 'Open').length)
const inProgressCount = computed(() => items.value.filter((item) => item.status === 'InProgress').length)
const resolvedCount = computed(() => items.value.filter((item) => item.status === 'Resolved').length)

function statusBadgeClass(status) {
  if (status === 'Resolved') return 'bg-emerald-100 text-emerald-700'
  if (status === 'InProgress') return 'bg-blue-100 text-blue-700'
  if (status === 'Disputed') return 'bg-amber-100 text-amber-700'
  return 'bg-rose-100 text-rose-700'
}

function statusDotClass(status) {
  if (status === 'Resolved') return 'bg-emerald-500'
  if (status === 'InProgress') return 'bg-blue-500'
  if (status === 'Disputed') return 'bg-amber-500'
  return 'bg-rose-500'
}

function categoryLabel(raw) {
  const lookup = {
    BuildingLines: 'Building Lines',
    StructuralDocumentation: 'Structural Docs',
    FireCompliance: 'Fire Compliance',
  }
  return lookup[raw] ?? raw
}

function selectProject(projectItem) {
  activeProjectId.value = projectItem.id
  uiStore.setLastProject(projectItem.id, projectItem.name)
}

async function loadRejections(projectId) {
  if (!projectId) return

  isLoading.value = true
  try {
    items.value = await rejectionsService.getProjectRejections(projectId)
  } catch (err) {
    toast.error('Failed to load rejection tracker', { description: err.message })
    items.value = []
  } finally {
    isLoading.value = false
  }
}

async function onStatusChange(item, nextStatus) {
  if (!activeProjectId.value || item.status === nextStatus) return

  const previousStatus = item.status
  const previousResolvedAt = item.resolvedAt

  item.status = nextStatus
  if (nextStatus === 'Resolved' && !item.resolvedAt) {
    item.resolvedAt = new Date().toISOString()
  }

  updatingId.value = item.id
  try {
    const updated = await rejectionsService.updateStatus(
      activeProjectId.value,
      item.id,
      nextStatus,
    )
    Object.assign(item, updated)
    toast.success('Status updated')
  } catch (err) {
    item.status = previousStatus
    item.resolvedAt = previousResolvedAt
    toast.error('Failed to update rejection status', { description: err.message })
  } finally {
    updatingId.value = null
  }
}

function exportCsv() {
  if (!filteredItems.value.length) {
    toast.info('No rows to export with current filters')
    return
  }

  const rows = [
    ['Source Document', 'Clause', 'Category', 'Status', 'Received At', 'Resolved At', 'Comment', 'Suggested Action'],
    ...filteredItems.value.map((item) => [
      item.sourceDocument,
      item.clauseReference,
      item.category,
      item.status,
      item.receivedAt ? new Date(item.receivedAt).toLocaleString('en-ZA') : '',
      item.resolvedAt ? new Date(item.resolvedAt).toLocaleString('en-ZA') : '',
      item.commentText,
      item.parsedAction,
    ]),
  ]

  const csv = rows
    .map((row) => row.map((v) => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','))
    .join('\r\n')

  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${project.value?.name?.replace(/[^a-zA-Z0-9]/g, '_') ?? 'Project'}_RejectionTracker.csv`
  a.click()
  URL.revokeObjectURL(url)
}

onMounted(async () => {
  if (!projectsStore.projects.length) {
    await projectsStore.fetchProjects()
  }

  if (activeProjectId.value) {
    await loadRejections(activeProjectId.value)
  }
})

watch(activeProjectId, async (id) => {
  if (!id) return
  await loadRejections(id)
})
</script>

<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col">
    <div v-if="!activeProjectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <div class="flex items-center justify-between shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">Rejection Tracker</p>
            <h2 class="text-lg font-extrabold tracking-tight text-slate-900 uppercase mt-0.5">Select a Project</h2>
            <p class="text-xs text-slate-500 mt-0.5">Choose a project to track council rejection comments and resolutions.</p>
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
          <div class="h-8 w-8 rounded-md bg-slate-100 flex items-center justify-center shrink-0">
            <History class="h-4 w-4 text-slate-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Total Items</p>
            <p class="text-xl font-bold text-slate-900 leading-tight">{{ totalCount }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-rose-50 flex items-center justify-center shrink-0">
            <AlertOctagon class="h-4 w-4 text-rose-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Open</p>
            <p class="text-xl font-bold text-rose-700 leading-tight">{{ openCount }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-blue-50 flex items-center justify-center shrink-0">
            <Clock3 class="h-4 w-4 text-blue-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">In Progress</p>
            <p class="text-xl font-bold text-blue-700 leading-tight">{{ inProgressCount }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3 rounded-xl bg-white border border-slate-200 shadow-sm px-4 py-3">
          <div class="h-8 w-8 rounded-md bg-emerald-50 flex items-center justify-center shrink-0">
            <CheckCircle2 class="h-4 w-4 text-emerald-600" />
          </div>
          <div>
            <p class="text-xs text-slate-500">Resolved</p>
            <p class="text-xl font-bold text-emerald-700 leading-tight">{{ resolvedCount }}</p>
          </div>
        </div>
      </div>

      <div class="flex-1 min-h-0 rounded-xl border border-slate-200 bg-white shadow-sm flex flex-col overflow-hidden">
        <div class="flex items-center justify-between px-4 py-3 border-b border-slate-100 shrink-0">
          <div>
            <p class="text-[10px] font-bold uppercase tracking-[0.2em] text-primary/70">Rejection Tracker</p>
            <p class="text-xs text-slate-500 mt-0.5">{{ project?.name ?? 'Project' }} · Council comments and resolution workflow</p>
          </div>

          <Button variant="outline" size="sm" class="gap-1.5 text-xs h-8" @click="exportCsv">
            <Download class="h-3.5 w-3.5" /> Export History
          </Button>
        </div>

        <div class="px-4 py-3 border-b border-slate-100 bg-slate-50/50 shrink-0 flex items-center gap-2">
          <div class="relative flex-1">
            <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
            <Input v-model="query" placeholder="Search clause, source, comment, or category..." class="pl-9 h-9 text-sm" />
          </div>

          <Select v-model="statusFilter">
            <SelectTrigger class="h-9 w-[170px] text-xs">
              <Filter class="h-3.5 w-3.5 mr-1.5 text-slate-400" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All statuses</SelectItem>
              <SelectItem value="Open">Open</SelectItem>
              <SelectItem value="InProgress">In Progress</SelectItem>
              <SelectItem value="Resolved">Resolved</SelectItem>
              <SelectItem value="Disputed">Disputed</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div class="flex-1 min-h-0 overflow-auto">
          <div v-if="isLoading" class="h-full flex flex-col items-center justify-center gap-3">
            <Loader2 class="h-6 w-6 animate-spin text-slate-400" />
            <p class="text-sm text-slate-500">Loading rejection comments...</p>
          </div>

          <div v-else-if="filteredItems.length === 0" class="h-full flex flex-col items-center justify-center p-12 text-center">
            <div class="h-16 w-16 bg-blue-50/50 rounded-full flex items-center justify-center mb-6">
              <History class="h-8 w-8 text-blue-400" />
            </div>
            <h3 class="text-sm font-bold text-slate-900 uppercase tracking-widest">No Rejection History Found</h3>
            <p class="text-[13px] text-slate-500 max-w-sm mt-2 leading-relaxed">
              Upload and parse rejection letters in the project workflow to automatically track comments, clauses, and resolution status here.
            </p>
          </div>

          <ul v-else class="divide-y divide-slate-100">
            <li v-for="item in filteredItems" :key="item.id" class="px-4 py-4 hover:bg-slate-50/60 transition-colors">
              <div class="flex items-start gap-3">
                <div :class="['mt-1 h-2.5 w-2.5 rounded-full', statusDotClass(item.status)]" />

                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <p class="text-sm font-bold text-slate-800">{{ item.clauseReference || 'Clause not set' }}</p>
                    <Badge variant="secondary" class="text-[10px]">{{ categoryLabel(item.category) }}</Badge>
                    <Badge :class="statusBadgeClass(item.status)" class="text-[10px]">{{ item.status }}</Badge>
                  </div>

                  <p class="text-xs text-slate-600 mt-1.5 leading-relaxed">{{ item.commentText || 'No comment text available.' }}</p>

                  <div v-if="item.parsedAction" class="mt-2 rounded-md border border-blue-100 bg-blue-50 px-3 py-2">
                    <p class="text-[11px] font-semibold text-blue-700 uppercase tracking-wider">Suggested Action</p>
                    <p class="text-xs text-blue-800 mt-0.5">{{ item.parsedAction }}</p>
                  </div>

                  <div class="mt-2 flex items-center gap-3 text-[11px] text-slate-400 flex-wrap">
                    <span>Source: {{ item.sourceDocument || 'Unknown document' }}</span>
                    <span>&middot;</span>
                    <span>Received: {{ item.receivedAt ? new Date(item.receivedAt).toLocaleDateString('en-ZA') : '-' }}</span>
                    <span v-if="item.resolvedAt">
                      &middot; Resolved: {{ new Date(item.resolvedAt).toLocaleDateString('en-ZA') }}
                    </span>
                  </div>
                </div>

                <div class="w-[160px] shrink-0">
                  <Label class="text-[10px] uppercase text-slate-400 font-bold">Status</Label>
                  <Select :model-value="item.status" @update:model-value="(value) => onStatusChange(item, value)">
                    <SelectTrigger class="mt-1 h-8 text-xs">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Open">Open</SelectItem>
                      <SelectItem value="InProgress">In Progress</SelectItem>
                      <SelectItem value="Resolved">Resolved</SelectItem>
                      <SelectItem value="Disputed">Disputed</SelectItem>
                    </SelectContent>
                  </Select>
                  <p v-if="updatingId === item.id" class="text-[10px] text-slate-400 mt-1">Updating...</p>
                </div>
              </div>
            </li>
          </ul>
        </div>
      </div>
    </main>
  </div>
</template>
