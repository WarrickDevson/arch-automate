import { reactive } from 'vue'

export const promptState = reactive({
  open: false,
  isOpen: false,
  mode: 'confirm',
  title: '',
  message: '',
  description: '',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  confirmLabel: 'Confirm',
  cancelLabel: 'Cancel',
  isDestructive: false,
  variant: 'default',
})

let resolveFn = null

export function showPrompt(options) {
  const mode = options.mode || 'confirm'
  const message = options.message || options.description || ''
  const confirmText = options.confirmText || options.confirmLabel || 'Confirm'
  const cancelText = options.cancelText || options.cancelLabel || 'Cancel'
  const isDestructive =
    options.isDestructive === true || options.variant === 'destructive'

  Object.assign(promptState, {
    open: true,
    isOpen: true,
    mode,
    title: options.title || '',
    message,
    description: message,
    confirmText,
    cancelText,
    confirmLabel: confirmText,
    cancelLabel: cancelText,
    isDestructive,
    variant: options.variant || (isDestructive ? 'destructive' : 'default'),
  })

  return new Promise((resolve) => {
    resolveFn = resolve
  })
}

export function resolvePrompt(result) {
  promptState.open = false
  promptState.isOpen = false
  if (resolveFn) {
    resolveFn(result)
    resolveFn = null
  }
}
