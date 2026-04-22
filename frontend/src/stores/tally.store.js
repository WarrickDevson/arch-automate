import { defineStore } from 'pinia'
import { ref } from 'vue'
import { tallyService } from '@/services/tallyService'

export const useTallyStore = defineStore('tally', () => {
  // Keyed by projectId for multi-project support
  const talliesByProject = ref({})
  const loadingByProject = ref({})
  const errorByProject = ref({})

  /**
   * Returns the tally for a project (from cache or API).
   * @param {string} projectId
   * @param {boolean} forceRefresh
   */
  async function fetchTally(projectId, forceRefresh = false) {
    if (!forceRefresh && talliesByProject.value[projectId] !== undefined) {
      return talliesByProject.value[projectId]
    }

    loadingByProject.value[projectId] = true
    errorByProject.value[projectId] = null

    try {
      const data = await tallyService.getTally(projectId)
      talliesByProject.value[projectId] = data ?? null
      return talliesByProject.value[projectId]
    } catch (err) {
      errorByProject.value[projectId] = err.message ?? 'Failed to load tally'
      return null
    } finally {
      loadingByProject.value[projectId] = false
    }
  }

  /**
   * Saves the extracted tally to the backend and updates local cache.
   * @param {string} projectId
   * @param {Array} items - TallyItem[]
   */
  async function saveTally(projectId, items) {
    const saved = await tallyService.saveTally(projectId, items)
    talliesByProject.value[projectId] = saved
    return saved
  }

  function getTally(projectId) {
    return talliesByProject.value[projectId] ?? null
  }

  function isLoading(projectId) {
    return loadingByProject.value[projectId] ?? false
  }

  return {
    talliesByProject,
    fetchTally,
    saveTally,
    getTally,
    isLoading,
  }
})
