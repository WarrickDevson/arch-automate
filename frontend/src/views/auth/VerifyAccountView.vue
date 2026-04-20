<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { Mail, RefreshCcw } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const authStore = useAuthStore()

const email = typeof route.query.username === 'string' ? route.query.username : ''
const resent = ref(false)

const onResend = async () => {
  resent.value = false
  authStore.errorMessage = null
  try {
    await authStore.resendRegistrationCode({ username: email })
    resent.value = true
  } catch {
    // error shown from store
  }
}
</script>

<template>
  <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
    <div class="border-l-4 border-primary pl-6 mb-10">
      <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">Arch Automate</p>
      <h1
        class="mt-2 font-display text-3xl font-extrabold tracking-tighter text-foreground uppercase"
      >
        Check Your Email
      </h1>
      <p class="mt-2 max-w-md text-[13px] font-medium text-slate-500">
        We've sent a confirmation link to
        <span class="font-bold text-foreground">{{ email }}</span
        >. Click the link to activate your account.
      </p>
    </div>

    <!-- Sent confirmation -->
    <div class="space-y-5">
      <div class="rounded-lg border border-slate-200 bg-slate-100/60 p-5">
        <p class="text-[11px] font-mono font-medium text-slate-500 leading-relaxed">
          <span class="font-bold text-slate-700">Didn't get it?</span>
          Check your spam folder. The link expires in 24 hours.
        </p>
      </div>

      <p
        v-if="resent"
        class="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-xs font-bold uppercase tracking-wider text-emerald-700"
      >
        Confirmation email resent.
      </p>
      <p
        v-if="authStore.errorMessage"
        class="text-xs font-bold uppercase tracking-wider text-rose-600"
      >
        {{ authStore.errorMessage }}
      </p>

      <Button
        type="button"
        variant="outline"
        class="h-12 w-full gap-2 uppercase text-xs font-bold tracking-widest transition-all active:scale-95"
        :disabled="authStore.loading"
        @click="onResend"
      >
        <RefreshCcw class="h-4 w-4" :class="authStore.loading ? 'animate-spin' : ''" />
        {{ authStore.loading ? 'Resending...' : 'Resend confirmation email' }}
      </Button>
    </div>

    <p class="mt-8 text-center text-sm text-slate-500">
      Wrong address?
      <RouterLink to="/auth/register" class="font-bold text-primary/70 hover:text-primary">
        Sign up again
      </RouterLink>
      &nbsp;or&nbsp;
      <RouterLink to="/auth/login" class="font-bold text-primary/70 hover:text-primary">
        Sign in
      </RouterLink>
    </p>
  </div>
</template>

<style scoped>
:deep(label) {
  @apply text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5;
}
</style>
