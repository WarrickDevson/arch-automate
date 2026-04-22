import apiClient from './apiClient'

export const roofCheckService = {
  async getCheck(projectId) {
    try {
      const { data } = await apiClient.get(`/roofcheck/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  async evaluate(projectId, request) {
    const { data } = await apiClient.post(`/roofcheck/${projectId}`, request)
    return data
  },
}
