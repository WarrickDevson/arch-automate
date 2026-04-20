<script setup>
import { computed, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useUiStore } from '@/stores/ui.store.js'
import { useTenantStore } from '@/stores/tenant.store.js'
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
  Menu,
  X,
  Search,
  Moon,
  Sun,
  UserRound,
  ShieldCheck,
  Plus,
} from 'lucide-vue-next'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()
const tenantStore = useTenantStore()
const { isDark, toggleTheme } = useTheme()

// Nav items with smart active matching.
// Workbench destination is dynamic — resumes last open project if one exists.
const navItems = computed(() => [
  {
    label: 'Command Center',
    to: '/dashboard',
    icon: LayoutDashboard,
    isActive: () => route.path === '/dashboard',
  },
  {
    label: 'Workbench',
    to: uiStore.lastProjectId ? `/workbench/${uiStore.lastProjectId}` : '/workbench',
    icon: Box,
    isActive: () => route.path.startsWith('/workbench'),
    // Show the last project name as context below the label
    context: uiStore.lastProjectName || null,
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
    label: 'Council Pack',
    to: '/council-pack',
    icon: FileDown,
    isActive: () => route.path.startsWith('/council-pack'),
  },
  {
    label: 'Rejection Tracker',
    to: '/rejections',
    icon: History,
    isActive: () => route.path.startsWith('/rejections'),
  },
  {
    label: 'Studio Settings',
    to: '/settings',
    icon: Settings,
    isActive: () => route.path.startsWith('/settings'),
  },
])

const sidebarLabelClass = computed(() =>
  uiStore.isSidebarCollapsed
    ? 'md:ml-0 md:w-0 md:opacity-0 md:pointer-events-none'
    : 'md:ml-3 md:w-auto md:opacity-100'
)

const contentOffsetClass = computed(() =>
  uiStore.isSidebarCollapsed ? 'md:pl-20' : 'md:pl-[240px]'
)

const selectedTenantId = computed({
  get: () => tenantStore.currentTenantId,
  set: (value) => tenantStore.setTenant(value),
})

const onSignOut = async () => {
  await authStore.logout()
  await router.replace('/login')
}

watch(
  () => route.fullPath,
  () => {
    uiStore.closeMobileDrawer()
  }
)
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-900">
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
          <div
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-blue-600 shadow-lg shadow-blue-900/20"
          >
            <ShieldCheck class="h-6 w-6 text-white" />
          </div>
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
      <nav class="flex-1 space-y-0.5">
        <!-- New Project CTA — accessible from anywhere in the app -->
        <button
          class="group flex w-full items-center rounded-lg px-3 py-2.5 mb-3 text-xs font-bold uppercase tracking-widest transition-all bg-blue-600/10 hover:bg-blue-600/20 text-blue-400 hover:text-blue-300 border border-blue-600/20 hover:border-blue-500/40"
          @click="uiStore.openCreateProjectSheet()"
        >
          <Plus class="h-4 w-4 shrink-0" />
          <span class="whitespace-nowrap transition-all duration-300" :class="sidebarLabelClass">
            New Project
          </span>
        </button>

        <RouterLink
          v-for="item in navItems"
          :key="item.label"
          :to="item.to"
          class="group flex items-start rounded-lg px-3 py-2.5 text-xs font-bold uppercase tracking-widest transition-all"
          :class="
            item.isActive()
              ? 'bg-blue-600 text-white shadow-lg shadow-blue-900/40'
              : 'text-slate-400 hover:bg-slate-900 hover:text-slate-100'
          "
        >
          <component :is="item.icon" class="h-4 w-4 shrink-0 mt-0.5" />
          <span class="whitespace-nowrap transition-all duration-300" :class="sidebarLabelClass">
            <span class="block">{{ item.label }}</span>
            <span
              v-if="item.context && !uiStore.isSidebarCollapsed"
              class="block text-[9px] font-medium normal-case tracking-normal truncate max-w-[130px]"
              :class="item.isActive() ? 'text-blue-200' : 'text-slate-500'"
            >{{ item.context }}</span>
          </span>
        </RouterLink>
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
        class="sticky top-0 z-20 border-b border-slate-200 bg-white/80 px-4 py-3 backdrop-blur-md md:px-8"
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

            <!-- Workspace Selector -->
            <div class="hidden items-center gap-3 md:flex">
              <template v-if="tenantStore.tenants.length > 1">
                <Select v-model="selectedTenantId">
                  <SelectTrigger
                    class="h-9 w-64 rounded-lg border-slate-200 bg-slate-50 text-[13px] font-medium"
                  >
                    <SelectValue placeholder="Select workspace…" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem v-for="t in tenantStore.tenants" :key="t.id" :value="t.id">
                      {{ t.name }}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </template>
              <template v-else>
                <div class="flex items-center gap-2 px-1">
                  <div class="h-2 w-2 rounded-full bg-emerald-500" />
                  <span class="text-[13px] font-bold text-slate-700 uppercase tracking-tight">
                    {{ tenantStore.currentTenant?.name || 'Standard Workspace' }}
                  </span>
                </div>
              </template>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <!-- Global Search -->
            <div class="relative hidden lg:block">
              <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
              <input
                type="search"
                placeholder="Search projects, erf numbers, clients..."
                class="h-9 w-80 rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-4 text-[13px] outline-none transition focus:border-blue-600 focus:ring-4 focus:ring-blue-600/5"
              />
            </div>

            <Button
              variant="outline"
              size="icon"
              class="rounded-lg border-slate-200"
              @click="toggleTheme"
            >
              <Moon v-if="!isDark" class="h-4 w-4 text-slate-500" />
              <Sun v-else class="h-4 w-4 text-slate-500" />
            </Button>

            <DropdownMenu>
              <DropdownMenuTrigger as-child>
                <Button
                  variant="outline"
                  class="h-9 gap-2 rounded-lg border-slate-200 bg-white px-3 text-[13px] font-bold"
                >
                  <UserRound class="h-4 w-4 text-slate-400" />
                  <span class="hidden md:inline">{{ authStore.displayName }}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="w-56 rounded-lg border-slate-200">
                <DropdownMenuLabel class="flex flex-col gap-0.5 py-3">
                  <span class="text-xs font-bold text-slate-900">{{ authStore.displayName }}</span>
                  <span class="text-[11px] font-medium text-slate-500">{{
                    authStore.profileEmail
                  }}</span>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  class="py-2.5 text-xs font-bold uppercase tracking-widest text-slate-600 cursor-pointer"
                >
                  Account Settings
                </DropdownMenuItem>
                <DropdownMenuItem
                  class="py-2.5 text-xs font-bold uppercase tracking-widest text-rose-600 cursor-pointer"
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
      <main class="p-3 md:p-6 max-w-[1600px] mx-auto">
        <RouterView />
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
  @apply bg-blue-600 text-white shadow-lg shadow-blue-900/40;
}
</style>
