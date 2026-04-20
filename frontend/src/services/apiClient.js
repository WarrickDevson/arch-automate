import axios from 'axios'
import { supabase } from '@/utils/supabase'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Attach the Supabase JWT to every outbound request.
// getSession() auto-refreshes an expired token before returning it.
apiClient.interceptors.request.use(
  async (config) => {
    const {
      data: { session },
    } = await supabase.auth.getSession()
    if (session?.access_token) {
      config.headers.Authorization = `Bearer ${session.access_token}`
    } else {
      console.warn('[apiClient] No active session — request will be sent without a Bearer token:', config.url)
    }
    return config
  },
  (error) => Promise.reject(error),
)

// Normalise API error responses so components get a plain `error.message` string.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const serverMessage = error.response?.data?.error
    if (serverMessage) {
      error.message = serverMessage
    }
    return Promise.reject(error)
  },
)

export function setupRefreshInterceptor(callbacks) {
  apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error.response?.status === 401) {
        const {
          data: { session },
        } = await supabase.auth.getSession()
        if (!session && callbacks.onSessionExpired) {
          callbacks.onSessionExpired()
        }
      }
      return Promise.reject(error)
    },
  )
}

export default apiClient
