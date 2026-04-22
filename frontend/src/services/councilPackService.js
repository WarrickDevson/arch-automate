import apiClient from './apiClient'

export const councilPackService = {
  async generateTables(payload, filename = 'CouncilTables.dxf') {
    const response = await apiClient.post('/councilpack/generate-tables', payload, {
      responseType: 'blob',
    })
    const url = URL.createObjectURL(new Blob([response.data], { type: 'application/dxf' }))
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  },

  async generateSansForms(projectId, filename = 'SANS_Forms.pdf') {
    const response = await apiClient.post(`/councilpack/generate-sans-forms?projectId=${projectId}`, {}, {
      responseType: 'blob',
    })
    const url = URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }))
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  },
}
