import apiClient from './apiClient'

export const gasCheckService = {
  async getCheck(projectId) {
    try {
      const { data } = await apiClient.get(`/gascheck/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  async evaluate(projectId, request) {
    const { data } = await apiClient.post(`/gascheck/${projectId}`, request)
    return data
  },
}
