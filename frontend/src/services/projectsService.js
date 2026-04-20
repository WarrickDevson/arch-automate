import apiClient from './apiClient'

// The C# API returns PascalCase properties from the anonymous object projection.
// We normalise to camelCase here so the rest of the app is consistent.
function normalise(p) {
  return {
    id: p.Id ?? p.id,
    name: p.Name ?? p.name,
    description: p.Description ?? p.description,
    status: p.Status ?? p.status,
    municipality: p.Municipality ?? p.municipality,
    erf: p.Erf ?? p.erf,
    siteAreaM2: p.SiteAreaM2 ?? p.siteAreaM2,
    zoningScheme: p.ZoningScheme ?? p.zoningScheme,
    ifcPath: p.IfcPath ?? p.ifcPath ?? null,
    createdAt: p.CreatedAt ?? p.createdAt,
    updatedAt: p.UpdatedAt ?? p.updatedAt,
  }
}

export const projectsService = {
  async getProjects() {
    const { data } = await apiClient.get('/projects')
    return data.map(normalise)
  },

  async getProject(id) {
    const { data } = await apiClient.get(`/projects/${id}`)
    return normalise(data)
  },

  async createProject(payload) {
    const { data } = await apiClient.post('/projects', payload)
    return normalise(data)
  },

  async updateProject(id, payload) {
    const { data } = await apiClient.put(`/projects/${id}`, payload)
    return normalise(data)
  },

  async deleteProject(id) {
    await apiClient.delete(`/projects/${id}`)
  },

  async setIfcPath(id, ifcPath) {
    const { data } = await apiClient.patch(`/projects/${id}/ifc-path`, { ifcPath })
    return data
  },
}
