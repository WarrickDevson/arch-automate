import { defineStore } from 'pinia'
import { ref } from 'vue'
import { roofCheckService } from '@/services/roofCheckService'

export const useRoofCheckStore = defineStore('roofCheck', () => {
  const checksByProject = ref({})
  const loadingByProject = ref({})

  async function fetchCheck(projectId, forceRefresh = false) {
    if (!forceRefresh && checksByProject.value[projectId] !== undefined) return checksByProject.value[projectId]
    loadingByProject.value[projectId] = true
    try {
      const data = await roofCheckService.getCheck(projectId)
      checksByProject.value[projectId] = data ?? null
      return checksByProject.value[projectId]
    } catch { return null }
    finally { loadingByProject.value[projectId] = false }
  }

  async function evaluate(projectId, request) {
    loadingByProject.value[projectId] = true
    try {
      const data = await roofCheckService.evaluate(projectId, request)
      checksByProject.value[projectId] = data
      return data
    } finally { loadingByProject.value[projectId] = false }
  }

  function getCheck(projectId) { return checksByProject.value[projectId] ?? null }
  function isLoading(projectId) { return loadingByProject.value[projectId] ?? false }

  return { fetchCheck, evaluate, getCheck, isLoading }
})
