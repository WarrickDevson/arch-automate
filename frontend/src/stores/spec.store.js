import { defineStore } from 'pinia'
import { ref } from 'vue'
import { specService } from '@/services/specService'

export const useSpecStore = defineStore('spec', () => {
  // Keyed by projectId
  const specsByProject = ref({})
  const loadingByProject = ref({})
  const errorByProject = ref({})

  /**
   * Fetches the saved spec for a project from the API.
   * @param {string} projectId
   * @param {boolean} forceRefresh
   */
  async function fetchSpec(projectId, forceRefresh = false) {
    if (!forceRefresh && specsByProject.value[projectId] !== undefined) {
      return specsByProject.value[projectId]
    }

    loadingByProject.value[projectId] = true
    errorByProject.value[projectId] = null

    try {
      const data = await specService.getSpec(projectId)
      specsByProject.value[projectId] = data ?? null
      return specsByProject.value[projectId]
    } catch (err) {
      errorByProject.value[projectId] = err.message ?? 'Failed to load specification'
      return null
    } finally {
      loadingByProject.value[projectId] = false
    }
  }

  /**
   * Compiles and saves a spec from IFC-extracted material items.
   * @param {string} projectId
   * @param {Array<{ rawValue: string, elementCategory: string }>} materialItems
   */
  async function compile(projectId, materialItems) {
    loadingByProject.value[projectId] = true
    errorByProject.value[projectId] = null

    try {
      const compiled = await specService.compile(projectId, materialItems)
      // The API returns the CompiledSpec directly; wrap with metadata shape for consistency
      specsByProject.value[projectId] = { spec: compiled, clauseCount: compiled.clauseCount }
      return specsByProject.value[projectId]
    } catch (err) {
      errorByProject.value[projectId] = err.message ?? 'Specification compile failed'
      return null
    } finally {
      loadingByProject.value[projectId] = false
    }
  }

  function getSpec(projectId) {
    return specsByProject.value[projectId] ?? null
  }

  function isLoading(projectId) {
    return loadingByProject.value[projectId] ?? false
  }

  return {
    specsByProject,
    fetchSpec,
    compile,
    getSpec,
    isLoading,
  }
})
