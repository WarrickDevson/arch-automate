import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { supabase } from '@/utils/supabase'
import apiClient from '@/services/apiClient'

export const useAuthStore = defineStore(
  'auth',
  () => {
    const user = ref(null)
    // session is intentionally NOT persisted — Supabase manages token storage itself.
    // We hold it in memory only for reactive access in components.
    const session = ref(null)
    const initialized = ref(false)
    const profileLoaded = ref(false)
    const loading = ref(false)
    const errorMessage = ref(null)

    // Profile data from public.profiles (tenant_id, role, display_name, avatar_url)
    const profile = ref(null)

    const activeChallenge = ref(null)
    const resetState = ref({ step: 'request', destination: null, deliveryMedium: null })
    const registrationState = ref({ destination: null, deliveryMedium: null })
    const needsOnboarding = ref(false)

    // ── Computed helpers ──────────────────────────────────────
    const isAuthenticated = computed(() => !!user.value)

    // Extracted from the custom JWT claims hook (public.custom_access_token_hook)
    const tenantId = computed(() => {
      if (profile.value?.tenant_id) return profile.value.tenant_id
      // Fallback: read from JWT claims if profile hasn't loaded yet
      const raw = session.value?.access_token
      if (!raw) return null
      try {
        const payload = JSON.parse(atob(raw.split('.')[1]))
        return payload.tenant_id ?? null
      } catch {
        return null
      }
    })

    const userRole = computed(() => profile.value?.role ?? null)

    const displayName = computed(
      () =>
        profile.value?.display_name ||
        user.value?.user_metadata?.full_name ||
        user.value?.email ||
        null,
    )

    const avatarUrl = computed(() => profile.value?.avatar_url ?? null)

    const accessToken = computed(() => session.value?.access_token ?? null)

    // ── Session init ──────────────────────────────────────────
    async function initializeSession() {
      try {
        const {
          data: { session: currentSession },
          error,
        } = await supabase.auth.getSession()
        if (error) throw error
        session.value = currentSession
        user.value = currentSession?.user || null
      } catch (err) {
        console.error('Error initializing session:', err)
      } finally {
        initialized.value = true
      }

      supabase.auth.onAuthStateChange((_event, newSession) => {
        session.value = newSession
        user.value = newSession?.user || null
        if (!newSession) {
          profileLoaded.value = false
          profile.value = null
        }
      })
    }

    // Called by the router guard on every protected route.
    // Uses the backend /api/onboarding/status endpoint (bypasses Supabase RLS)
    // to reliably detect whether the user has a tenant+profile row.
    async function bootstrapProfile() {
      if (profileLoaded.value || !user.value) return

      try {
        // Always fetch the profile from Supabase for display data
        const { data: profileData } = await supabase
          .from('profiles')
          .select('id, tenant_id, display_name, email, role, avatar_url')
          .eq('id', user.value.id)
          .single()

        if (profileData) {
          profile.value = profileData
          needsOnboarding.value = false
        } else {
          // No profile row — verify via backend (bypasses RLS) before deciding
          const { data: status } = await apiClient.get('/onboarding/status')
          needsOnboarding.value = status.needsOnboarding === true
          profile.value = null
        }
      } catch (err) {
        // If Supabase returns PGRST116 (no row), check backend authoritatively
        try {
          const { data: status } = await apiClient.get('/onboarding/status')
          needsOnboarding.value = status.needsOnboarding === true
          profile.value = null
        } catch {
          // Backend also unreachable — safe default: require onboarding
          needsOnboarding.value = true
          profile.value = null
          console.error('bootstrapProfile: both Supabase and backend unreachable', err)
        }
      } finally {
        profileLoaded.value = true
      }
    }

    // Creates a new tenant + profile for the current user via the backend API.
    // The backend runs with service-role DB access so it bypasses RLS on public.tenants.
    async function setupTenant({ practiceName }) {
      if (!user.value) throw new Error('Not authenticated')

      const displayName =
        user.value.user_metadata?.full_name ||
        `${user.value.user_metadata?.first_name ?? ''} ${user.value.user_metadata?.last_name ?? ''}`.trim() ||
        undefined

      const { data } = await apiClient.post('/onboarding/setup', {
        practiceName,
        displayName: displayName || undefined,
      })

      // Re-fetch the profile so all fields (display_name, role, etc.) are populated
      const { data: profileData } = await supabase
        .from('profiles')
        .select('id, tenant_id, display_name, email, role, avatar_url')
        .eq('id', user.value.id)
        .single()

      profile.value = profileData
      needsOnboarding.value = false
      profileLoaded.value = true

      return data
    }

    async function loginWithPassword({ username, password }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { data, error } = await supabase.auth.signInWithPassword({
          email: username,
          password,
        })
        if (error) throw error
        session.value = data.session
        user.value = data.user
        // Always reset so bootstrapProfile() runs fresh for each login
        profileLoaded.value = false
        profile.value = null
        needsOnboarding.value = false
        await bootstrapProfile()
        return data
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    async function registerWithPassword({ firstName, lastName, username, password }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { data, error } = await supabase.auth.signUp({
          email: username,
          password,
          options: {
            data: { first_name: firstName, last_name: lastName },
            emailRedirectTo: `${window.location.origin}/auth/callback`,
          },
        })
        if (error) throw error

        if (data.user && !data.session) {
          // Email confirmation required — Supabase project must have OTP/email confirm enabled.
          registrationState.value = { destination: username, deliveryMedium: 'Email' }
          return { status: 'pending-verification' }
        }

        session.value = data.session
        user.value = data.user
        // A confirmed auth session does not guarantee onboarding/profile is complete.
        // Always hydrate profile/tenant state before deciding where to route next.
        profileLoaded.value = false
        profile.value = null
        needsOnboarding.value = false
        await bootstrapProfile()

        return {
          status: needsOnboarding.value ? 'onboarding-required' : 'confirmed',
        }
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    // Confirm email address with an OTP. Requires "Enable email confirmations" + OTP in Supabase project settings.
    async function confirmRegistrationCode({ username, code }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { data, error } = await supabase.auth.verifyOtp({
          email: username,
          token: code,
          type: 'signup',
        })
        if (error) throw error
        if (data.session) {
          session.value = data.session
          user.value = data.user
          profileLoaded.value = true
        }
        return data
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    async function resendRegistrationCode({ username }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { error } = await supabase.auth.resend({ type: 'signup', email: username })
        if (error) throw error
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    // Sends a password-reset magic link. User clicks link → lands on /auth/callback?type=recovery.
    async function requestPasswordReset({ username }) {
      loading.value = true
      errorMessage.value = null
      try {
        const redirectTo = `${window.location.origin}/auth/callback`
        const { error } = await supabase.auth.resetPasswordForEmail(username, { redirectTo })
        if (error) throw error
        resetState.value = { step: 'sent', destination: username, deliveryMedium: 'Email' }
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    async function confirmPasswordReset({ username, code, newPassword }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { error: otpError } = await supabase.auth.verifyOtp({
          email: username,
          token: code,
          type: 'recovery',
        })
        if (otpError) throw otpError

        const { error: updateError } = await supabase.auth.updateUser({ password: newPassword })
        if (updateError) throw updateError
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    // Update password for the currently authenticated user. Old password is not required by Supabase client.
    async function changePassword({ newPassword }) {
      loading.value = true
      errorMessage.value = null
      try {
        const { error } = await supabase.auth.updateUser({ password: newPassword })
        if (error) throw error
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    async function logout() {
      loading.value = true
      errorMessage.value = null
      try {
        const { error } = await supabase.auth.signOut()
        if (error) throw error
        clearSession()
      } catch (err) {
        errorMessage.value = err.message
        throw err
      } finally {
        loading.value = false
      }
    }

    function clearSession() {
      session.value = null
      user.value = null
      profile.value = null
      errorMessage.value = null
      profileLoaded.value = false
      needsOnboarding.value = false
    }

    function clearAuthWorkflow() {
      activeChallenge.value = null
      resetState.value = { step: 'request', destination: null, deliveryMedium: null }
      registrationState.value = { destination: null, deliveryMedium: null }
      errorMessage.value = null
    }

    return {
      // State
      user,
      session,
      profile,
      initialized,
      profileLoaded,
      needsOnboarding,
      loading,
      errorMessage,
      activeChallenge,
      resetState,
      registrationState,
      // Computed
      isAuthenticated,
      tenantId,
      userRole,
      displayName,
      avatarUrl,
      accessToken,
      // Actions
      initializeSession,
      bootstrapProfile,
      loginWithPassword,
      registerWithPassword,
      confirmRegistrationCode,
      resendRegistrationCode,
      requestPasswordReset,
      confirmPasswordReset,
      changePassword,
      logout,
      clearAuthWorkflow,
      clearSession,
      setupTenant,
    }
  },
  {
    persist: {
      // Only persist non-sensitive UI state.
      // Supabase manages token storage in its own localStorage keys.
      // session is excluded — it's hydrated from Supabase on every page load.
      paths: ['user', 'profile', 'needsOnboarding', 'registrationState', 'resetState'],
    },
  }
)
