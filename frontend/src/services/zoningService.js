import apiClient from './apiClient'

export const zoningService = {
  /**
   * Returns all known SA zoning designations with their permitted limits.
   * @returns {Promise<Array>} Array of scheme objects with name, maxCoveragePercent, maxFar, etc.
   */
  async getSchemes() {
    const { data } = await apiClient.get('/zoningschemes')
    return data
  },

  /**
   * Returns the limits for a single scheme.
   * @param {string} name - e.g. "Residential 1"
   * @returns {Promise<Object>}
   */
  async getScheme(name) {
    const { data } = await apiClient.get(`/zoningschemes/${encodeURIComponent(name)}`)
    return data
  },

  /**
   * Runs a full zoning compliance evaluation via the backend engine.
   * @param {Object} params - ZoningParameters payload
   * @returns {Promise<Object>} ComplianceResult
   */
  async evaluate(params) {
    const { data } = await apiClient.post('/compliance/zoning', params)
    return data
  },
}
