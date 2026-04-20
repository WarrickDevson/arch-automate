import apiClient from './apiClient'

export const complianceService = {
  async evaluate(params) {
    const { data } = await apiClient.post('/compliance/evaluate', params)
    return data
  },
}
