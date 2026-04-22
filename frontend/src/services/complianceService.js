import apiClient from './apiClient'

export const complianceService = {
  async evaluate(params) {
    const { data } = await apiClient.post('/compliance/evaluate', params)
    return data
  },

  /**
   * Runs SANS 10400-XA (Energy Usage in Buildings) compliance analysis.
   * @param {Object} params - XaEnergyParameters payload
   * @returns {Promise<Object>} XaEnergyResult with checks, violations, and energy rating
   */
  async evaluateEnergy(params) {
    const { data } = await apiClient.post('/compliance/xa', params)
    return data
  },
}
