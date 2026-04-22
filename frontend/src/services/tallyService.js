import apiClient from './apiClient'

export const tallyService = {
  /**
   * Returns the saved fixture tally for a project, or null if none exists yet.
   * @param {string} projectId
   */
  async getTally(projectId) {
    try {
      const { data } = await apiClient.get(`/tally/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  /**
   * Upserts the fixture tally for a project.
   * @param {string} projectId
   * @param {Array} items - Array of TallyItem objects from IFC extraction
   */
  async saveTally(projectId, items) {
    const { data } = await apiClient.post(`/tally/${projectId}`, { items })
    return data
  },
}
