import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulesService } from '@/services/schedulesService'

export const useSchedulesStore = defineStore('schedules', () => {
  // Keyed by projectId for multi-project support
  const schedulesByProject = ref({})
  const loadingByProject = ref({})
  const errorByProject = ref({})

  /**
   * Returns the schedule for a project (from cache or API).
   * @param {string} projectId
   * @param {boolean} forceRefresh
   */
  async function fetchSchedule(projectId, forceRefresh = false) {
    if (!forceRefresh && schedulesByProject.value[projectId]) {
      return schedulesByProject.value[projectId]
    }

    loadingByProject.value[projectId] = true
    errorByProject.value[projectId] = null

    try {
      const data = await schedulesService.getSchedule(projectId)
      schedulesByProject.value[projectId] = data ?? null
      return schedulesByProject.value[projectId]
    } catch (err) {
      errorByProject.value[projectId] = err.message ?? 'Failed to load schedule'
      return null
    } finally {
      loadingByProject.value[projectId] = false
    }
  }

  /**
   * Saves the extracted schedule to the backend and updates local cache.
   * @param {string} projectId
   * @param {{ doors: ScheduleItem[], windows: ScheduleItem[] }} payload
   */
  async function saveSchedule(projectId, payload) {
    const saved = await schedulesService.saveSchedule(projectId, payload)
    schedulesByProject.value[projectId] = saved
    return saved
  }

  function getSchedule(projectId) {
    return schedulesByProject.value[projectId] ?? null
  }

  function isLoading(projectId) {
    return loadingByProject.value[projectId] ?? false
  }

  return {
    schedulesByProject,
    fetchSchedule,
    saveSchedule,
    getSchedule,
    isLoading,
  }
})
