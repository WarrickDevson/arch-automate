<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useUiStore } from '@/stores/ui.store.js'
import { useProjectsStore } from '@/stores/projects.store'
import { useTheme } from '@/composables/useTheme'
import { Button } from '@/components/ui/button'
import CreateProjectSheet from '@/views/dashboard/components/CreateProjectSheet.vue'
import {
  LayoutDashboard,
  Box,
  Users,
  Library,
  FileDown,
  History,
  Settings,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  CheckCircle2,
  Menu,
  X,
  Moon,
  Sun,
  UserRound,
  ShieldCheck,
  Plus,
  TableProperties,
  Map,
  Zap,
  FileText,
  Download,
  HardHat,
  Flame,
  Home,
  Lock,
} from 'lucide-vue-next'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()
const projectsStore = useProjectsStore()
const { isDark, toggleTheme } = useTheme()
const pageToolbarActionsCount = ref(0)

const hasPageToolbarActions = computed(() => pageToolbarActionsCount.value > 0)

const onPageToolbarActionsRegister = () => {
  pageToolbarActionsCount.value += 1
}

const onPageToolbarActionsUnregister = () => {
  pageToolbarActionsCount.value = Math.max(0, pageToolbarActionsCount.value - 1)
}

const hasActiveProject = computed(() => Boolean(uiStore.lastProjectId))

// Workspace-level navigation that does not require a selected project.
const workspaceNavItems = computed(() => [
  {
    label: 'Command Center',
    to: '/dashboard',
    icon: LayoutDashboard,
    isActive: () => route.path === '/dashboard',
  },
  {
    label: 'Stakeholders',
    to: '/stakeholders',
    icon: Users,
    isActive: () => route.path.startsWith('/stakeholders'),
  },
  {
    label: 'Knowledge Base',
    to: '/knowledge',
    icon: Library,
    isActive: () => route.path.startsWith('/knowledge'),
  },
  {
    label: 'Studio Settings',
    to: '/settings',
    icon: Settings,
    isActive: () => route.path.startsWith('/settings'),
  },
])

// Project workspace navigation that requires an active project context.
const projectNavItems = computed(() => [
  {
    label: 'Workbench',
    to: uiStore.lastProjectId ? `/workbench/${uiStore.lastProjectId}` : '/workbench',
    icon: Box,
    isActive: () => route.path.startsWith('/workbench'),
    requiresProject: true,
  },
  {
    label: 'Zoning Calculator',
    to: uiStore.lastProjectId ? `/zoning/${uiStore.lastProjectId}` : '/zoning',
    icon: Map,
    isActive: () => route.path.startsWith('/zoning'),
    requiresProject: true,
  },
  {
    label: 'Foundation Check',
    to: uiStore.lastProjectId ? `/foundation/${uiStore.lastProjectId}` : '/foundation',
    icon: HardHat,
    isActive: () => route.path.startsWith('/foundation'),
    requiresProject: true,
  },
  {
    label: 'Roof Checklist',
    to: uiStore.lastProjectId ? `/roofcheck/${uiStore.lastProjectId}` : '/roofcheck',
    icon: Home,
    isActive: () => route.path.startsWith('/roofcheck'),
    requiresProject: true,
  },
  {
    label: 'Gas Installation',
    to: uiStore.lastProjectId ? `/gascheck/${uiStore.lastProjectId}` : '/gascheck',
    icon: Flame,
    isActive: () => route.path.startsWith('/gascheck'),
    requiresProject: true,
  },
  {
    label: 'Schedules',
    to: uiStore.lastProjectId ? `/schedules/${uiStore.lastProjectId}` : '/schedules',
    icon: TableProperties,
    isActive: () => route.path.startsWith('/schedules'),
    requiresProject: true,
  },
  {
    label: 'Fixture Tally',
    to: uiStore.lastProjectId ? `/tally/${uiStore.lastProjectId}` : '/tally',
    icon: Zap,
    isActive: () => route.path.startsWith('/tally'),
    requiresProject: true,
  },
  {
    label: 'Specifications',
    to: uiStore.lastProjectId ? `/specs/${uiStore.lastProjectId}` : '/specs',
    icon: FileText,
    isActive: () => route.path.startsWith('/specs'),
    requiresProject: true,
  },
  {
    label: 'Export Documents',
    to: uiStore.lastProjectId ? `/exports/${uiStore.lastProjectId}` : '/exports',
    icon: Download,
    isActive: () => route.path.startsWith('/exports'),
    requiresProject: true,
  },
  {
    label: 'Council Pack',
    to: '/council-pack',
    icon: FileDown,
    isActive: () => route.path.startsWith('/council-pack'),
    requiresProject: true,
  },
  {
    label: 'Rejection Tracker',
    to: '/rejections',
    icon: History,
    isActive: () => route.path.startsWith('/rejections'),
    requiresProject: true,
  },
])

const navSections = computed(() => [
  { label: 'Workspace', items: workspaceNavItems.value },
  { label: 'Project Workspace', items: projectNavItems.value },
])

const sidebarLabelClass = computed(() =>
  uiStore.isSidebarCollapsed
    ? 'md:ml-0 md:w-0 md:opacity-0 md:pointer-events-none'
    : 'md:ml-3 md:w-auto md:opacity-100'
)

const contentOffsetClass = computed(() =>
  uiStore.isSidebarCollapsed ? 'md:pl-20' : 'md:pl-[240px]'
)

const onSignOut = async () => {
  await authStore.logout()
  await router.replace('/login')
}

const onDisabledProjectNavClick = async () => {
  if (route.path !== '/dashboard') {
    await router.push('/dashboard')
  }
  uiStore.openCreateProjectSheet()
}

watch(
  () => route.fullPath,
  () => {
    uiStore.closeMobileDrawer()
  }
)

onMounted(() => {
  window.addEventListener('page-toolbar-actions:register', onPageToolbarActionsRegister)
  window.addEventListener('page-toolbar-actions:unregister', onPageToolbarActionsUnregister)

  if (projectsStore.projects.length === 0) {
    projectsStore.fetchProjects()
  }
})

const onSwitchProject = (proj) => {
  const oldId = uiStore.lastProjectId
  uiStore.setLastProject(proj.id, proj.name)
  if (oldId && route.path.includes(oldId)) {
    router.push(route.path.replace(oldId, proj.id))
  }
}


onUnmounted(() => {
  window.removeEventListener('page-toolbar-actions:register', onPageToolbarActionsRegister)
  window.removeEventListener('page-toolbar-actions:unregister', onPageToolbarActionsUnregister)
})
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
    <!-- Mobile Overlay -->
    <div
      class="fixed inset-0 z-40 bg-slate-950/60 backdrop-blur-sm transition md:hidden"
      :class="uiStore.isMobileDrawerOpen ? 'opacity-100' : 'pointer-events-none opacity-0'"
      @click="uiStore.closeMobileDrawer"
    />

    <!-- Sidebar: Now Navy/Slate Engineering Theme -->
    <aside
      class="fixed inset-y-0 left-0 z-50 flex flex-col border-r border-slate-800 bg-slate-950 p-4 text-white transition-[width,transform] duration-300 ease-in-out md:z-30 md:translate-x-0"
      :class="[
        uiStore.isMobileDrawerOpen ? 'translate-x-0' : '-translate-x-full',
        uiStore.isSidebarCollapsed ? 'w-20' : 'w-[240px]',
      ]"
    >
      <!-- Logo Section -->
      <div class="flex items-center justify-between border-b border-slate-800 pb-6 mb-6">
        <div class="flex items-center gap-3 overflow-hidden">
            <img
            src="/app-logo.png"
            alt="ArchAutomate Logo"
            class="h-10 w-10 shrink-0 rounded-lg object-contain"
            />
          <div
            v-if="!uiStore.isSidebarCollapsed"
            class="flex flex-col animate-in fade-in slide-in-from-left-2"
          >
            <span class="text-[11px] font-bold uppercase tracking-[0.2em] text-blue-400"
              >ArchAutomate</span
            >
            <span class="text-xs font-bold text-slate-400 uppercase tracking-tighter"
              >Studio Suite</span
            >
          </div>
        </div>
        <Button
          variant="ghost"
          size="icon"
          class="text-slate-400 hover:bg-slate-800 md:hidden"
          @click="uiStore.closeMobileDrawer"
        >
          <X class="h-4 w-4" />
        </Button>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 overflow-y-auto pr-1">
        <div v-for="section in navSections" :key="section.label" class="mb-4">
          <p
            v-if="!uiStore.isSidebarCollapsed"
            class="mb-1 px-3 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500"
          >
            {{ section.label }}
          </p>

          <div class="space-y-0.5">
            <template v-for="item in section.items">
              <RouterLink
                v-if="!item.requiresProject || hasActiveProject"
                :key="`enabled-${item.label}`"
                :to="item.to"
                class="group flex items-start rounded-lg px-3 py-2.5 text-xs font-bold uppercase tracking-widest transition-all mb-1"
                :class="
                  item.isActive()
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-900/40'
                    : 'text-slate-400 hover:bg-slate-900 hover:text-slate-100'
                "
              >
                <component :is="item.icon" class="h-4 w-4 shrink-0" />
                <span class="whitespace-nowrap transition-all duration-300" :class="sidebarLabelClass">
                  <span class="block">{{ item.label }}</span>
                  <span
                    v-if="item.context && !uiStore.isSidebarCollapsed"
                    class="block text-[9px] font-medium normal-case tracking-normal truncate max-w-[130px]"
                    :class="item.isActive() ? 'text-blue-200' : 'text-slate-500'"
                  >{{ item.context }}</span>
                </span>
              </RouterLink>

              <button
                v-else
                :key="`disabled-${item.label}`"
                type="button"
                class="group flex w-full items-start rounded-lg border border-slate-800/80 bg-slate-900/40 px-3 py-2.5 text-xs font-bold uppercase tracking-widest text-slate-600/90"
                title="Select or create a project first"
                @click="onDisabledProjectNavClick"
              >
                <component :is="item.icon" class="h-4 w-4 shrink-0 mt-0.5" />
                <span class="whitespace-nowrap transition-all duration-300" :class="sidebarLabelClass">
                  <span class="block">{{ item.label }}</span>
                  <span v-if="!uiStore.isSidebarCollapsed" class="mt-1 inline-flex items-center gap-1 text-[9px] font-medium normal-case tracking-normal text-slate-500">
                    <Lock class="h-3 w-3" />
                    Requires active project
                  </span>
                </span>
              </button>
            </template>
          </div>
        </div>
      </nav>

      <!-- Sidebar Toggle -->
      <Button
        type="button"
        variant="ghost"
        class="hidden items-center justify-center gap-2 rounded-lg border border-slate-800 bg-slate-900/50 py-6 text-[10px] font-bold uppercase tracking-widest text-slate-500 hover:bg-slate-800 hover:text-slate-200 md:flex"
        @click="uiStore.toggleSidebar"
      >
        <ChevronLeft v-if="!uiStore.isSidebarCollapsed" class="h-4 w-4" />
        <ChevronRight v-else class="h-4 w-4" />
        <span v-if="!uiStore.isSidebarCollapsed">Collapse View</span>
      </Button>
    </aside>

    <!-- Main Content Area -->
    <div class="transition-[padding-left] duration-300 ease-in-out" :class="contentOffsetClass">
      <header
        class="sticky top-0 z-20 border-b border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-950/80 px-4 py-3 backdrop-blur-md md:px-8"
      >
        <div class="flex items-center justify-between gap-4">
          <div class="flex items-center gap-4">
            <Button
              variant="outline"
              size="icon"
              class="rounded-lg md:hidden"
              @click="uiStore.openMobileDrawer"
            >
              <Menu class="h-4 w-4" />
            </Button>

            <!-- Workspace & Project Selector -->
            <div class="hidden items-center gap-6 md:flex">
              <h1 class="text-sm font-bold tracking-tight text-slate-800 dark:text-slate-200 uppercase mr-2">
                {{ route.meta.title || 'Studio Suite' }}
              </h1>
              <!-- Active Project Indicator -->
              <DropdownMenu v-if="projectsStore.projects.length > 1">
                <DropdownMenuTrigger as-child>
                  <div
                    class="flex items-center gap-3 px-4 py-1.5 rounded-full border transition-colors cursor-pointer border-emerald-200 dark:border-emerald-900 bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-100 dark:hover:bg-emerald-900/50"
                  >
                    <div class="flex flex-col">
                      <span class="text-[9px] font-bold uppercase tracking-wider leading-none mb-0.5 text-emerald-600/70 dark:text-emerald-400/70">
                        Active Project
                      </span>
                      <span class="text-[12px] font-bold truncate max-w-[200px] leading-none">
                        {{ uiStore.lastProjectName || 'Project selected' }}
                      </span>
                    </div>
                    <div
                      class="h-5 w-5 rounded-full flex items-center justify-center bg-white dark:bg-emerald-950 border shadow-sm border-emerald-200 dark:border-emerald-800 text-emerald-600 dark:text-emerald-400"
                    >
                      <ChevronDown class="h-3 w-3" />
                    </div>
                  </div>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="start" class="w-64 rounded-xl border-slate-200 dark:border-slate-800 mt-2 bg-white dark:bg-slate-900">
                  <DropdownMenuLabel class="text-[10px] font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400 py-2">Switch Project</DropdownMenuLabel>
                  <DropdownMenuSeparator class="bg-slate-200 dark:bg-slate-800" />
                  <DropdownMenuItem
                    v-for="proj in projectsStore.projects"
                    :key="proj.id"
                    class="py-2.5 text-xs font-bold cursor-pointer flex items-center justify-between hover:bg-slate-50 dark:hover:bg-slate-800 focus:bg-slate-50 dark:focus:bg-slate-800 text-slate-900 dark:text-slate-100"
                    @click="onSwitchProject(proj)"
                  >
                    <div class="flex items-center gap-2 truncate">
                      <Box class="h-3.5 w-3.5 text-slate-400 dark:text-slate-500 shrink-0" />
                      <span class="truncate">{{ proj.name }}</span>
                    </div>
                    <CheckCircle2 v-if="proj.id === uiStore.lastProjectId" class="h-4 w-4 text-emerald-500 dark:text-emerald-400 shrink-0 ml-3" />
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              <div
                v-else
                class="flex items-center gap-3 px-4 py-1.5 rounded-full border transition-colors"
                :class="hasActiveProject ? 'border-emerald-200 dark:border-emerald-900 bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400' : 'border-amber-200 dark:border-amber-900 bg-amber-50 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 cursor-pointer hover:bg-amber-100 dark:hover:bg-amber-900/50'"
                @click="!hasActiveProject && uiStore.openCreateProjectSheet()"
              >
                <div class="flex flex-col">
                  <span class="text-[9px] font-bold uppercase tracking-wider leading-none mb-0.5" :class="hasActiveProject ? 'text-emerald-600/70 dark:text-emerald-400/70' : 'text-amber-600/70 dark:text-amber-400/70'">
                    Active Project
                  </span>
                  <span class="text-[12px] font-bold truncate max-w-[200px] leading-none">
                    {{ hasActiveProject ? (uiStore.lastProjectName || 'Project selected') : 'None selected' }}
                  </span>
                </div>
                <div
                  class="h-5 w-5 rounded-full flex items-center justify-center bg-white dark:bg-slate-950 border shadow-sm"
                  :class="hasActiveProject ? 'border-emerald-200 dark:border-emerald-800 text-emerald-600 dark:text-emerald-400' : 'border-amber-200 dark:border-amber-800 text-amber-600 dark:text-amber-400'"
                >
                  <Plus v-if="!hasActiveProject" class="h-3 w-3" />
                  <Box v-else class="h-3 w-3" />
                </div>
              </div>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <div id="app-page-toolbar-actions" class="hidden items-center gap-2 md:flex"></div>

            <!-- <Button
              v-if="!hasPageToolbarActions"
              variant="outline"
              class="hidden h-9 gap-2 rounded-lg border-slate-200 bg-white px-3 text-[11px] font-bold uppercase tracking-wider md:flex"
              disabled
            >
              <Download class="h-4 w-4 text-slate-500" />
              <span>Export</span>
            </Button> -->

            <Button
              v-if="!hasPageToolbarActions"
              class="h-9 gap-2 rounded-lg bg-blue-600 px-3 text-[11px] font-bold uppercase tracking-wider text-white hover:bg-blue-700 shadow-md shadow-blue-900/10"
              @click="uiStore.openCreateProjectSheet()"
            >
              <Plus class="h-4 w-4" />
              <span class="hidden sm:inline">New Project</span>
            </Button>

            <div class="h-6 w-px bg-slate-200 dark:bg-slate-800 mx-1 hidden md:block"></div>

            <Button
              variant="outline"
              size="icon"
              class="rounded-lg border-slate-200 dark:border-slate-800 dark:bg-slate-950 dark:hover:bg-slate-900"
              @click="toggleTheme"
            >
              <Moon v-if="!isDark" class="h-4 w-4 text-slate-500" />
              <Sun v-else class="h-4 w-4 text-slate-400" />
            </Button>

            <DropdownMenu>
              <DropdownMenuTrigger as-child>
                <Button
                  variant="outline"
                  class="h-9 gap-2 rounded-lg border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 px-3 text-[13px] font-bold dark:hover:bg-slate-900"
                >
                  <UserRound class="h-4 w-4 text-slate-400" />
                  <!-- <span class="hidden md:inline">{{ authStore.displayName }}</span> -->
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="w-56 rounded-lg border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
                <DropdownMenuLabel class="flex flex-col gap-0.5 py-3">
                  <span class="text-xs font-bold text-slate-900 dark:text-slate-100">{{ authStore.displayName }}</span>
                  <span class="text-[11px] font-medium text-slate-500 dark:text-slate-400">{{
                    authStore.profileEmail
                  }}</span>
                </DropdownMenuLabel>
                <DropdownMenuSeparator class="bg-slate-200 dark:bg-slate-800" />
                <!-- <DropdownMenuItem
                  class="py-2.5 text-xs font-bold uppercase tracking-widest text-slate-600 dark:text-slate-300 cursor-pointer dark:hover:bg-slate-800 dark:focus:bg-slate-800"
                >
                  Account Settings
                </DropdownMenuItem> -->
                <DropdownMenuItem
                  class="py-2.5 text-xs font-bold uppercase tracking-widest text-rose-600 dark:text-rose-400 cursor-pointer dark:hover:bg-slate-800 dark:focus:bg-slate-800"
                  @click="onSignOut"
                >
                  Sign Out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </header>

      <!-- Page Content Slot -->
      <main class="w-full max-w-[1600px] mx-auto px-3 py-4 md:px-6 md:py-5">
        <RouterView :key="$route.fullPath" />
      </main>
    </div>

    <!-- Global Create Project Sheet — accessible from sidebar or any page -->
    <CreateProjectSheet
      :open="uiStore.showCreateProjectSheet"
      @update:open="uiStore.showCreateProjectSheet = $event"
    />
  </div>
</template>

<style scoped>
/* Technical Styling for active navigation states */
.router-link-active {
  background-color: rgb(37 99 235);
  color: rgb(255 255 255);
  box-shadow: 0 10px 15px -3px rgb(30 58 138 / 0.4), 0 4px 6px -4px rgb(30 58 138 / 0.4);
}

:deep(.view-page) {
  color: rgb(51 65 85);
}

:deep(.view-page > * + *) {
  margin-top: 1.5rem;
}

:deep(.view-toolbar) {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  border-radius: 0.75rem;
  border: 1px solid rgb(226 232 240);
  background-color: rgb(255 255 255);
  padding: 0.75rem 1rem;
  box-shadow: 0 1px 2px 0 rgb(15 23 42 / 0.05);
}

[data-theme='dark'] :deep(.view-page) {
  color: rgb(226 232 240);
}
[data-theme='dark'] :deep(.view-toolbar) {
  border-color: rgb(30 41 59);
  background-color: rgb(2 6 23);
  box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.2);
}

:deep(.view-toolbar-actions) {
  margin-left: auto;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

:deep(.view-toolbar-title) {
  display: none;
}
</style>
