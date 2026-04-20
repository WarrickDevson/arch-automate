import { createApp } from 'vue'
import { createPinia } from 'pinia'

// THREE.Clock is deprecated in r168+ but still used internally by @thatopen/components.
// Suppress only this specific warning until the upstream library migrates to THREE.Timer.
const _origWarn = console.warn.bind(console)
console.warn = (...args) => {
  if (typeof args[0] === 'string' && args[0].startsWith('THREE.Clock:')) return
  _origWarn(...args)
}
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'

import router from '@/router'
import { useAuthStore } from '@/stores/auth.store'
import { setupRefreshInterceptor } from '@/services/apiClient'
import { registerLocalSyncWorker } from '@/services/localSyncServiceWorker'

import App from './App.vue'
import './style.css'
import 'vue-sonner/style.css'

const app = createApp(App)

// 1. Setup Pinia
const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)
app.use(pinia)

// 2. Setup Router
app.use(router)

// 3. Auth Integration
const authStore = useAuthStore()
authStore.initializeSession()

setupRefreshInterceptor({
  onSessionExpired: () => {
    router.push({ name: 'login', query: { reason: 'session_expired' } })
  },
})

// 4. Background Services
registerLocalSyncWorker().catch(console.error)

app.mount('#app')
