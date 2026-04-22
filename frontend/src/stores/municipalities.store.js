import { defineStore } from 'pinia'
import { ref } from 'vue'
import { municipalitiesService } from '@/services/municipalitiesService'

export const useMunicipalitiesStore = defineStore('municipalities', () => {
  const municipalities = ref([])
  const loading = ref(false)
  const loaded = ref(false)

  async function fetchMunicipalities() {
    if (loaded.value) return
    loading.value = true
    try {
      municipalities.value = await municipalitiesService.getMunicipalities()
      loaded.value = true
    } finally {
      loading.value = false
    }
  }

  /**
   * Returns the province name for a given municipality ID.
   * Returns an empty string if the municipality is not yet loaded or not found.
   */
  function getProvinceByMunicipalityId(municipalityId) {
    if (!municipalityId) return ''
    const found = municipalities.value.find((m) => m.id === municipalityId)
    return found?.provinceName ?? ''
  }

  return { municipalities, loading, loaded, fetchMunicipalities, getProvinceByMunicipalityId }
})
