import { t } from '~/utils/i18n'
import { For, Show, splitProps, type JSX, type ParentProps } from 'solid-js'
import { Icon, type IconName } from './Icon'

export function Spinner(props: { class?: string }) {
  return (
    <svg class={`animate-spin ${props.class ?? 'size-5'}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" class="opacity-25" />
      <path d="M22 12a10 10 0 0 0-10-10" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
    </svg>
  )
}

export function Card(props: ParentProps<{ class?: string }>) {
  return <div class={`panel ${props.class ?? ''}`}>{props.children}</div>
}

export function CardHeader(props: ParentProps<{ title: JSX.Element; description?: JSX.Element; actions?: JSX.Element }>) {
  return (
    <div class="flex flex-wrap items-start justify-between gap-3 rounded-t-[14px] border-b border-brand-100 bg-brand-50 px-5 py-4">
      <div class="min-w-0">
        <h2 class="font-display text-lg font-bold text-ink">{props.title}</h2>
        <Show when={props.description}>
          <p class="mt-0.5 text-sm text-slate-500">{props.description}</p>
        </Show>
      </div>
      <Show when={props.actions}>
        <div class="flex flex-wrap items-center gap-2">{props.actions}</div>
      </Show>
    </div>
  )
}

export type BadgeTone = 'gray' | 'blue' | 'green' | 'amber' | 'red' | 'violet'

const badgeTones: Record<BadgeTone, string> = {
  gray: 'bg-slate-100 text-slate-700 ring-slate-200',
  blue: 'bg-brand-50 text-brand-700 ring-brand-200',
  green: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  amber: 'bg-amber-50 text-amber-800 ring-amber-200',
  red: 'bg-red-50 text-red-700 ring-red-200',
  violet: 'bg-violet-50 text-violet-700 ring-violet-200',
}

export function Badge(props: ParentProps<{ tone?: BadgeTone; class?: string }>) {
  return (
    <span
      class={`inline-flex items-center gap-1 whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset ${
        badgeTones[props.tone ?? 'gray']
      } ${props.class ?? ''}`}
    >
      {props.children}
    </span>
  )
}

export function Skeleton(props: { class?: string }) {
  return <div class={`animate-pulse rounded-md bg-slate-200/70 ${props.class ?? 'h-4 w-full'}`} />
}

export function SkeletonRows(props: { rows?: number }) {
  return (
    <div class="space-y-3 p-5" aria-busy="true" aria-label={t('Loading')}>
      <For each={Array.from({ length: props.rows ?? 5 })}>
        {() => (
          <div class="flex items-center gap-4">
            <Skeleton class="h-4 w-2/5" />
            <Skeleton class="h-4 w-1/5" />
            <Skeleton class="h-4 w-1/6" />
            <Skeleton class="ms-auto h-4 w-16" />
          </div>
        )}
      </For>
    </div>
  )
}

export function PageHeader(props: { title: JSX.Element; description?: JSX.Element; actions?: JSX.Element; breadcrumb?: JSX.Element; class?: string }) {
  return (
    <div class={props.class ?? 'mb-6'}>
      <Show when={props.breadcrumb}>
        <div class="mb-2 text-sm text-slate-500">{props.breadcrumb}</div>
      </Show>
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div class="min-w-0">
          <h1 class="font-display text-3xl font-extrabold tracking-tight text-ink [overflow-wrap:anywhere] sm:text-4xl">{props.title}</h1>
          <Show when={props.description}>
            <p class="mt-1 text-sm text-slate-500">{props.description}</p>
          </Show>
        </div>
        <Show when={props.actions}>
          <div class="flex flex-wrap items-center gap-3">{props.actions}</div>
        </Show>
      </div>
    </div>
  )
}

const statTones = {
  lavender: 'bg-brand-100',
  butter: 'bg-butter',
  mint: 'bg-mint',
  peach: 'bg-peach',
}

export function StatCard(props: { label: string; value: JSX.Element; hint?: string; icon?: IconName; tone?: keyof typeof statTones }) {
  return (
    <Card class="p-5">
      <div class="flex items-center justify-between gap-3">
        <div class="min-w-0">
          <p class="text-sm font-semibold text-slate-600">{props.label}</p>
          <p class="mt-1 font-display text-3xl font-extrabold text-ink">{props.value}</p>
        </div>
        <Show when={props.icon}>
          {(icon) => (
            <span class={`sticker size-10 ${statTones[props.tone ?? 'lavender']}`}>
              <Icon name={icon()} class="size-5" />
            </span>
          )}
        </Show>
      </div>
      <Show when={props.hint}>
        <p class="mt-1 text-xs text-slate-500">{props.hint}</p>
      </Show>
    </Card>
  )
}

export function Divider(props: JSX.HTMLAttributes<HTMLHRElement>) {
  const [local, rest] = splitProps(props, ['class'])
  return <hr class={`border-slate-200 ${local.class ?? ''}`} {...rest} />
}
