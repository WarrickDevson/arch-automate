import apiClient from './apiClient'

export const foundationService = {
  async getCheck(projectId) {
    try {
      const { data } = await apiClient.get(`/foundation/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  async evaluate(projectId, request) {
    const { data } = await apiClient.post(`/foundation/${projectId}`, request)
    return data
  },
}
