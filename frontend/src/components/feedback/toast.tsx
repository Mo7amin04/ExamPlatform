import { Toast, toaster } from '@kobalte/core/toast'
import { Portal } from 'solid-js/web'
import { Show } from 'solid-js'
import { Icon } from '~/components/ui/Icon'

export type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface ToastOptions {
  title: string
  description?: string
  variant?: ToastVariant
}

const variantClasses: Record<ToastVariant, string> = {
  success: 'border-emerald-200 bg-emerald-50 text-emerald-900',
  error: 'border-red-200 bg-red-50 text-red-900',
  warning: 'border-amber-200 bg-amber-50 text-amber-900',
  info: 'border-slate-200 bg-white text-slate-900',
}

const variantIcon: Record<ToastVariant, 'check' | 'alert' | 'info'> = {
  success: 'check',
  error: 'alert',
  warning: 'alert',
  info: 'info',
}

export function showToast({ title, description, variant = 'info' }: ToastOptions) {
  return toaster.show((props) => (
    <Toast
      toastId={props.toastId}
      duration={variant === 'error' ? 7000 : 4000}
      class={`toast pointer-events-auto flex w-full items-start gap-3 rounded-lg border p-3 shadow-lg ${variantClasses[variant]}`}
    >
      <Icon name={variantIcon[variant]} class="mt-0.5 size-5 shrink-0" />
      <div class="min-w-0 flex-1">
        <Toast.Title class="text-sm font-semibold">{title}</Toast.Title>
        <Show when={description}>
          <Toast.Description class="mt-0.5 text-sm opacity-90">{description}</Toast.Description>
        </Show>
      </div>
      <Toast.CloseButton class="rounded p-0.5 opacity-60 hover:opacity-100" aria-label="Dismiss">
        <Icon name="x" class="size-4" />
      </Toast.CloseButton>
    </Toast>
  ))
}

export const toast = {
  success: (title: string, description?: string) => showToast({ title, description, variant: 'success' }),
  error: (title: string, description?: string) => showToast({ title, description, variant: 'error' }),
  info: (title: string, description?: string) => showToast({ title, description, variant: 'info' }),
}

export function ToastRegion() {
  return (
    <Portal>
      <Toast.Region swipeDirection="right" limit={4}>
        <Toast.List class="fixed bottom-4 end-4 z-[100] flex w-[22rem] max-w-[calc(100vw-2rem)] flex-col gap-2 outline-none" />
      </Toast.Region>
    </Portal>
  )
}
