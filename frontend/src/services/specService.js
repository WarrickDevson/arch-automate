import apiClient from './apiClient'

export const specService = {
  /**
   * Returns the saved compiled spec for a project, or null if none exists yet.
   * @param {string} projectId
   */
  async getSpec(projectId) {
    try {
      const { data } = await apiClient.get(`/specs/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  /**
   * Compiles a new spec from the given material strings and saves it.
   * @param {string} projectId
   * @param {Array<{ rawValue: string, elementCategory: string }>} materialItems
   */
  async compile(projectId, materialItems) {
    const materialStrings = materialItems.map((m) => m.rawValue)
    const elementCategories = materialItems.map((m) => m.elementCategory)
    const { data } = await apiClient.post(`/specs/${projectId}/compile`, {
      materialStrings,
      elementCategories,
    })
    return data
  },
}
