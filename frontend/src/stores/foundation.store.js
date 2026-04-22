import { defineStore } from 'pinia'
import { ref } from 'vue'
import { foundationService } from '@/services/foundationService'

export const useFoundationStore = defineStore('foundation', () => {
  const checksByProject = ref({})
  const loadingByProject = ref({})

  async function fetchCheck(projectId, forceRefresh = false) {
    if (!forceRefresh && checksByProject.value[projectId] !== undefined) return checksByProject.value[projectId]
    loadingByProject.value[projectId] = true
    try {
      const data = await foundationService.getCheck(projectId)
      checksByProject.value[projectId] = data ?? null
      return checksByProject.value[projectId]
    } catch { return null }
    finally { loadingByProject.value[projectId] = false }
  }

  async function evaluate(projectId, request) {
    loadingByProject.value[projectId] = true
    try {
      const data = await foundationService.evaluate(projectId, request)
      checksByProject.value[projectId] = data
      return data
    } finally { loadingByProject.value[projectId] = false }
  }

  function getCheck(projectId) { return checksByProject.value[projectId] ?? null }
  function isLoading(projectId) { return loadingByProject.value[projectId] ?? false }

  return { fetchCheck, evaluate, getCheck, isLoading }
})
