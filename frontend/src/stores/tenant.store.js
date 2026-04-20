import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useTenantStore = defineStore(
  'tenant',
  () => {
    const tenants = ref([])
    const currentTenantId = ref(null)
    const isLoading = ref(false)

    const currentTenant = computed(
      () => tenants.value.find((t) => t.id === currentTenantId.value) || null
    )

    function setTenants(newTenants) {
      tenants.value = newTenants
      if (newTenants.length > 0 && !currentTenantId.value) {
        currentTenantId.value = newTenants[0].id
      }
    }

    function setTenant(id) {
      currentTenantId.value = id
    }

    function clearTenants() {
      tenants.value = []
      currentTenantId.value = null
    }

    return {
      tenants,
      currentTenantId,
      currentTenant,
      isLoading,
      setTenants,
      setTenant,
      clearTenants,
    }
  },
  {
    persist: true,
  }
)
