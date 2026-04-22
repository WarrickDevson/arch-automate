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

  async function updateProject(id, payload) {
    const updated = await projectsService.updateProject(id, payload)
    const idx = projects.value.findIndex((p) => p.id === id)
    if (idx !== -1) projects.value[idx] = updated
    return updated
  }

  async function saveIfcData(id, payload) {
    await projectsService.saveIfcData(id, payload)
    const idx = projects.value.findIndex((p) => p.id === id)
    if (idx !== -1) {
      const p = projects.value[idx]
      if (payload.proposedGfaM2 != null) p.proposedGfaM2 = payload.proposedGfaM2
      if (payload.footprintM2 != null) p.footprintM2 = payload.footprintM2
      if (payload.buildingHeightM != null) p.buildingHeightM = payload.buildingHeightM
      if (payload.numberOfStoreys != null) p.numberOfStoreys = payload.numberOfStoreys
    }
  }

  async function saveParams(id, payload) {
    await projectsService.saveParams(id, payload)
    const idx = projects.value.findIndex((p) => p.id === id)
    if (idx !== -1) Object.assign(projects.value[idx], payload)
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
    updateProject,
    saveIfcData,
    saveParams,
  }
})
