import apiClient from './apiClient'

export const municipalitiesService = {
  async getMunicipalities() {
    const { data } = await apiClient.get('/municipalities')
    return data.map((m) => ({
      id: m.Id ?? m.id,
      name: m.Name ?? m.name,
      shortName: m.ShortName ?? m.shortName,
      category: m.Category ?? m.category,
      zoningScheme: m.ZoningScheme ?? m.zoningScheme,
      provinceId: m.ProvinceId ?? m.provinceId,
      provinceName: m.ProvinceName ?? m.provinceName ?? '',
    }))
  },
}
