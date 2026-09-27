import { Show, type JSX } from 'solid-js'
import { Button } from '~/components/ui/Button'
import { Icon, type IconName } from '~/components/ui/Icon'
import { Spinner } from '~/components/ui/Primitives'
import { getErrorMessage } from '~/services/api'

export function EmptyState(props: { icon?: IconName; title: string; description?: string; action?: JSX.Element }) {
  return (
    <div class="flex flex-col items-center justify-center px-6 py-14 text-center">
      <div class="mb-3 flex size-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <Icon name={props.icon ?? 'file'} class="size-6" />
      </div>
      <h3 class="text-sm font-semibold text-slate-900">{props.title}</h3>
      <Show when={props.description}>
        <p class="mt-1 max-w-sm text-sm text-slate-500">{props.description}</p>
      </Show>
      <Show when={props.action}>
        <div class="mt-4">{props.action}</div>
      </Show>
    </div>
  )
}

export function ErrorState(props: { error: unknown; onRetry?: () => void; title?: string }) {
  return (
    <div role="alert" class="flex flex-col items-center justify-center px-6 py-12 text-center">
      <div class="mb-3 flex size-12 items-center justify-center rounded-full bg-red-50 text-red-500">
        <Icon name="alert" class="size-6" />
      </div>
      <h3 class="text-sm font-semibold text-slate-900">{props.title ?? 'Could not load data'}</h3>
      <p class="mt-1 max-w-md text-sm text-slate-500">{getErrorMessage(props.error)}</p>
      <Show when={props.onRetry}>
        <Button class="mt-4" variant="secondary" size="sm" icon="refresh" onClick={() => props.onRetry?.()}>
          Try again
        </Button>
      </Show>
    </div>
  )
}

export function LoadingState(props: { label?: string }) {
  return (
    <div class="flex items-center justify-center gap-3 py-16 text-sm text-slate-500" aria-busy="true">
      <Spinner class="size-5 text-brand-600" />
      {props.label ?? 'Loading…'}
    </div>
  )
}

export function InlineAlert(props: { tone?: 'info' | 'warning' | 'error' | 'success'; title?: string; children: JSX.Element }) {
  const tones = {
    info: 'border-brand-200 bg-brand-50 text-brand-900',
    warning: 'border-amber-200 bg-amber-50 text-amber-900',
    error: 'border-red-200 bg-red-50 text-red-900',
    success: 'border-emerald-200 bg-emerald-50 text-emerald-900',
  }
  return (
    <div class={`flex gap-3 rounded-lg border p-3 text-sm ${tones[props.tone ?? 'info']}`} role="status">
      <Icon name={props.tone === 'success' ? 'check' : props.tone === 'info' ? 'info' : 'alert'} class="mt-0.5 size-4 shrink-0" />
      <div>
        <Show when={props.title}>
          <p class="font-semibold">{props.title}</p>
        </Show>
        <div>{props.children}</div>
      </div>
    </div>
  )
}
