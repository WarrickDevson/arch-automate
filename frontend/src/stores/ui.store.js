import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useUiStore = defineStore(
  'ui',
  () => {
    const isSidebarCollapsed = ref(false)
    const isMobileDrawerOpen = ref(false)

    // ── Getting Started checklist ────────────────────────────────────────
    const gettingStartedDismissed = ref(false)
    const hasOpenedWorkbench = ref(false)
    const hasGeneratedCouncilPack = ref(false)

    // ── Last opened project (persisted so sidebar link resumes context) ──
    const lastProjectId = ref(null)
    const lastProjectName = ref(null)

    // ── Global Create Project sheet (accessible from sidebar) ────────────
    const showCreateProjectSheet = ref(false)

    const sidebarWidthClass = computed(() => (isSidebarCollapsed.value ? 'w-20' : 'w-[190px]'))

    function toggleSidebar() {
      isSidebarCollapsed.value = !isSidebarCollapsed.value
    }

    function setSidebarCollapsed(value) {
      isSidebarCollapsed.value = value
    }

    function toggleMobileDrawer() {
      isMobileDrawerOpen.value = !isMobileDrawerOpen.value
    }

    function openMobileDrawer() {
      isMobileDrawerOpen.value = true
    }

    function closeMobileDrawer() {
      isMobileDrawerOpen.value = false
    }

    function dismissGettingStarted() {
      gettingStartedDismissed.value = true
    }

    function markWorkbenchOpened() {
      hasOpenedWorkbench.value = true
    }

    function markCouncilPackGenerated() {
      hasGeneratedCouncilPack.value = true
    }

    function setLastProject(id, name) {
      lastProjectId.value = id
      lastProjectName.value = name
    }

    function openCreateProjectSheet() {
      showCreateProjectSheet.value = true
    }

    return {
      isSidebarCollapsed,
      isMobileDrawerOpen,
      gettingStartedDismissed,
      hasOpenedWorkbench,
      hasGeneratedCouncilPack,
      lastProjectId,
      lastProjectName,
      showCreateProjectSheet,
      sidebarWidthClass,
      toggleSidebar,
      setSidebarCollapsed,
      toggleMobileDrawer,
      openMobileDrawer,
      closeMobileDrawer,
      dismissGettingStarted,
      markWorkbenchOpened,
      markCouncilPackGenerated,
      setLastProject,
      openCreateProjectSheet,
    }
  },
  {
    persist: true,
  }
)
