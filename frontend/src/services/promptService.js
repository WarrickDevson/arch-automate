import { reactive } from 'vue'

export const promptState = reactive({
  isOpen: false,
  title: '',
  description: '',
  confirmLabel: 'Confirm',
  cancelLabel: 'Cancel',
  variant: 'default'
})

let resolveFn = null

export function showPrompt(options) {
  Object.assign(promptState, {
    isOpen: true,
    title: options.title || '',
    description: options.description || '',
    confirmLabel: options.confirmLabel || 'Confirm',
    cancelLabel: options.cancelLabel || 'Cancel',
    variant: options.variant || 'default'
  })

  return new Promise((resolve) => {
    resolveFn = resolve
  })
}

export function resolvePrompt(result) {
  promptState.isOpen = false
  if (resolveFn) {
    resolveFn(result)
    resolveFn = null
  }
}
