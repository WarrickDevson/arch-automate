<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Building2, ArrowRight, Loader2, CheckCircle2 } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const step = ref('practice') // 'practice' | 'done'
const submitting = ref(false)
const errorMsg = ref(null)

const form = reactive({
  practiceName: '',
})

const errors = reactive({})

function validate() {
  Object.keys(errors).forEach((k) => delete errors[k])
  if (!form.practiceName.trim()) errors.practiceName = 'Practice name is required'
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  submitting.value = true
  errorMsg.value = null
  try {
    await authStore.setupTenant({ practiceName: form.practiceName.trim() })
    step.value = 'done'
  } catch (err) {
    errorMsg.value = err.message ?? 'Something went wrong. Please try again.'
  } finally {
    submitting.value = false
  }
}

function proceed() {
  router.replace({ name: 'dashboard' })
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center py-16">
    <div class="w-full max-w-md animate-in fade-in slide-in-from-bottom-4 duration-500">

      <!-- Step: Practice Setup -->
      <template v-if="step === 'practice'">
        <div class="border-l-4 border-primary pl-6 mb-10">
          <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">Welcome</p>
          <h1 class="mt-2 font-display text-3xl font-extrabold tracking-tighter text-foreground uppercase">
            Set Up Your Workspace
          </h1>
          <p class="mt-2 max-w-md text-[13px] font-medium text-slate-500">
            You're almost ready. Tell us the name of your architectural practice to create your workspace.
          </p>
        </div>

        <form class="space-y-6" @submit.prevent="submit">
          <div class="space-y-2">
            <Label for="practice-name" class="text-xs font-bold uppercase tracking-wide">
              Practice / Organisation Name <span class="text-rose-500">*</span>
            </Label>
            <div class="relative">
              <Building2 class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <Input
                id="practice-name"
                v-model="form.practiceName"
                class="pl-9"
                :class="errors.practiceName ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
                placeholder="e.g. Smith & Associates Architects"
                autocomplete="organization"
              />
            </div>
            <p v-if="errors.practiceName" class="text-[11px] text-rose-500">{{ errors.practiceName }}</p>
          </div>

          <div
            v-if="errorMsg"
            class="flex items-start gap-2 p-3 bg-rose-50 border border-rose-200 rounded-lg text-sm text-rose-700"
          >
            {{ errorMsg }}
          </div>

          <Button
            type="submit"
            class="w-full bg-primary hover:bg-primary/90 gap-2 uppercase text-xs font-bold h-11"
            :disabled="submitting"
          >
            <Loader2 v-if="submitting" class="h-4 w-4 animate-spin" />
            <template v-else>
              Create Workspace <ArrowRight class="h-4 w-4" />
            </template>
          </Button>
        </form>
      </template>

      <!-- Step: Done -->
      <template v-else-if="step === 'done'">
        <div class="text-center space-y-6">
          <div class="flex justify-center">
            <div class="bg-emerald-100 rounded-full p-5">
              <CheckCircle2 class="h-12 w-12 text-emerald-600" />
            </div>
          </div>
          <div>
            <h1 class="text-2xl font-extrabold tracking-tighter text-foreground uppercase">
              Workspace Ready
            </h1>
            <p class="mt-2 text-sm text-slate-500">
              Your practice workspace has been created. You can now start adding projects.
            </p>
          </div>
          <Button
            class="w-full bg-primary hover:bg-primary/90 gap-2 uppercase text-xs font-bold h-11"
            @click="proceed"
          >
            Go to Dashboard <ArrowRight class="h-4 w-4" />
          </Button>
        </div>
      </template>

    </div>
  </div>
</template>
