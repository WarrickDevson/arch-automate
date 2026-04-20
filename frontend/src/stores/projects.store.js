import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { projectsService } from '@/services/projectsService'

export const useProjectsStore = defineStore('projects', () => {
  const projects = ref([])
  const loading = ref(false)
  const error = ref(null)

  // ── Computed ────────────────────────────────────────────
  const byMunicipality = computed(() => {
    const munis = projects.value.map((p) => p.municipality).filter(Boolean)
    return [...new Set(munis)].sort()
  })

  const stats = computed(() => {
    const all = projects.value
    return {
      total: all.length,
      inCouncilCount: all.filter((p) => p.status === 'SubmittedToCouncil').length,
      rejectedCount: all.filter((p) => p.status === 'Rejected').length,
      inProgressCount: all.filter((p) => p.status === 'InProgress').length,
    }
  })

  // ── Actions ─────────────────────────────────────────────
  async function fetchProjects() {
    loading.value = true
    error.value = null
    try {
      projects.value = await projectsService.getProjects()
    } catch (err) {
      error.value = err.message ?? 'Failed to load projects'
    } finally {
      loading.value = false
    }
  }

  async function addProject(payload) {
    const created = await projectsService.createProject(payload)
    projects.value.unshift(created)
    return created
  }

  async function removeProject(id) {
    await projectsService.deleteProject(id)
    projects.value = projects.value.filter((p) => p.id !== id)
  }

  async function fetchProject(id) {
    const project = await projectsService.getProject(id)
    const idx = projects.value.findIndex((p) => p.id === id)
    if (idx !== -1) {
      projects.value[idx] = project
    }
    return project
  }

  async function updateIfcPath(id, ifcPath) {
    await projectsService.setIfcPath(id, ifcPath)
    const idx = projects.value.findIndex((p) => p.id === id)
    if (idx !== -1) projects.value[idx].ifcPath = ifcPath
  }

  return {
    projects,
    loading,
    error,
    byMunicipality,
    stats,
    fetchProjects,
    fetchProject,
    addProject,
    removeProject,
    updateIfcPath,
  }
})
