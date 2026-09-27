import { Dialog as KDialog } from '@kobalte/core/dialog'
import { AlertDialog } from '@kobalte/core/alert-dialog'
import { createSignal, Show, type JSX, type ParentProps } from 'solid-js'
import { Button } from './Button'
import { Icon } from './Icon'

type DialogSize = 'sm' | 'md' | 'lg' | 'xl'

const sizes: Record<DialogSize, string> = {
  sm: 'max-w-md',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-5xl',
}

/** Accessible modal (focus trap, Esc to close, labelled title) built on Kobalte. */
export function Modal(
  props: ParentProps<{
    open: boolean
    onOpenChange: (open: boolean) => void
    title: string
    description?: string
    size?: DialogSize
    footer?: JSX.Element
  }>,
) {
  return (
    <KDialog open={props.open} onOpenChange={props.onOpenChange}>
      <KDialog.Portal>
        <KDialog.Overlay class="dialog-overlay fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-[1px]" />
        <div class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto p-4 sm:items-center">
          <KDialog.Content
            class={`dialog-content flex max-h-[calc(100vh-2rem)] w-full flex-col rounded-xl bg-white shadow-xl ${sizes[props.size ?? 'md']}`}
          >
            <div class="flex items-start justify-between gap-4 border-b border-slate-100 px-5 py-4">
              <div>
                <KDialog.Title class="text-lg font-semibold text-slate-900">{props.title}</KDialog.Title>
                <Show when={props.description}>
                  <KDialog.Description class="mt-0.5 text-sm text-slate-500">{props.description}</KDialog.Description>
                </Show>
              </div>
              <KDialog.CloseButton class="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700" aria-label="Close">
                <Icon name="x" class="size-5" />
              </KDialog.CloseButton>
            </div>
            <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4">{props.children}</div>
            <Show when={props.footer}>
              <div class="flex flex-wrap justify-end gap-2 border-t border-slate-100 bg-slate-50/60 px-5 py-3">{props.footer}</div>
            </Show>
          </KDialog.Content>
        </div>
      </KDialog.Portal>
    </KDialog>
  )
}

interface ConfirmOptions {
  title: string
  description: string
  confirmLabel?: string
  tone?: 'danger' | 'primary'
  onConfirm: () => Promise<unknown> | unknown
}

/**
 * Confirmation dialog hook. Usage:
 *   const confirm = useConfirm();  confirm.ask({...});  <confirm.Dialog />
 */
export function useConfirm() {
  const [options, setOptions] = createSignal<ConfirmOptions | null>(null)
  const [busy, setBusy] = createSignal(false)

  const close = () => {
    if (!busy()) setOptions(null)
  }

  const run = async () => {
    const current = options()
    if (!current) return
    setBusy(true)
    try {
      await current.onConfirm()
      setOptions(null)
    } finally {
      setBusy(false)
    }
  }

  const Dialog = () => (
    <AlertDialog open={!!options()} onOpenChange={(open) => !open && close()}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay class="dialog-overlay fixed inset-0 z-50 bg-slate-900/40" />
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4">
          <AlertDialog.Content class="dialog-content w-full max-w-md rounded-xl bg-white p-5 shadow-xl">
            <AlertDialog.Title class="text-lg font-semibold text-slate-900">{options()?.title}</AlertDialog.Title>
            <AlertDialog.Description class="mt-2 text-sm text-slate-600">{options()?.description}</AlertDialog.Description>
            <div class="mt-5 flex justify-end gap-2">
              <Button variant="secondary" onClick={close} disabled={busy()}>
                Cancel
              </Button>
              <Button variant={options()?.tone === 'primary' ? 'primary' : 'danger'} loading={busy()} onClick={run}>
                {options()?.confirmLabel ?? 'Confirm'}
              </Button>
            </div>
          </AlertDialog.Content>
        </div>
      </AlertDialog.Portal>
    </AlertDialog>
  )

  return { ask: (o: ConfirmOptions) => setOptions(o), Dialog }
}
