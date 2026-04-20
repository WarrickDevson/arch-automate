<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '@/utils/supabase'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

onMounted(async () => {
  // Supabase appends the token as a URL hash fragment (#access_token=...) or
  // as query params (?code=...) depending on the flow (PKCE vs implicit).
  // supabase-js automatically detects and exchanges both — we just need to
  // call getSession() after the client has processed the URL.
  const {
    data: { session },
    error,
  } = await supabase.auth.getSession()

  if (error || !session) {
    // Token was invalid, expired, or already used.
    await router.replace({
      path: '/auth/login',
      query: { error: 'confirmation_failed' },
    })
    return
  }

  // Hydrate store with the confirmed session
  authStore.session = session
  authStore.user = session.user
  // Force a fresh profile check — this user may have just confirmed for the first time
  authStore.profileLoaded = false
  authStore.profile = null
  authStore.needsOnboarding = false

  const type =
    new URLSearchParams(window.location.search).get('type') ??
    new URLSearchParams(window.location.hash.replace('#', '?')).get('type')

  if (type === 'recovery') {
    await router.replace('/settings/change-password')
    return
  }

  // Email confirmation: check if onboarding is still needed then route directly.
  // No more bouncing through /auth/login.
  await authStore.bootstrapProfile()

  if (authStore.needsOnboarding) {
    await router.replace({ name: 'onboarding' })
  } else {
    await router.replace({ name: 'dashboard' })
  }
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-slate-50">
    <div class="flex flex-col items-center gap-4 text-center">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-slate-200 border-t-primary" />
      <p class="text-xs font-bold uppercase tracking-widest text-slate-400">
        Confirming your account…
      </p>
    </div>
  </div>
</template>
