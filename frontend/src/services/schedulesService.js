import apiClient from './apiClient'

export const schedulesService = {
  /**
   * Returns the saved schedule for a project, or null if none exists yet.
   * @param {string} projectId
   */
  async getSchedule(projectId) {
    try {
      const { data } = await apiClient.get(`/schedules/${projectId}`)
      return data
    } catch (err) {
      if (err.response?.status === 404) return null
      throw err
    }
  },

  /**
   * Upserts the door/window schedule for a project.
   * @param {string} projectId
   * @param {{ doors: ScheduleItem[], windows: ScheduleItem[] }} payload
   */
  async saveSchedule(projectId, payload) {
    const { data } = await apiClient.post(`/schedules/${projectId}`, payload)
    return data
  },
}
