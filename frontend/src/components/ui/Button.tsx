import { splitProps, Show, type JSX } from 'solid-js'
import { Spinner } from './Spinner'
import { Icon, type IconName } from './Icon'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'ai'
type Size = 'sm' | 'md'

const variants: Record<Variant, string> = {
  primary: 'bg-brand-600 text-white hover:bg-brand-700 shadow-sm disabled:bg-brand-300',
  secondary: 'bg-white text-slate-700 border border-slate-300 hover:bg-slate-50 shadow-sm disabled:text-slate-400',
  ghost: 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 disabled:text-slate-300',
  danger: 'bg-red-600 text-white hover:bg-red-700 shadow-sm disabled:bg-red-300',
  ai: 'bg-gradient-to-r from-violet-600 to-brand-600 text-white hover:from-violet-700 hover:to-brand-700 shadow-sm disabled:opacity-60',
}

const sizes: Record<Size, string> = {
  sm: 'h-8 px-3 text-sm gap-1.5',
  md: 'h-10 px-4 text-sm gap-2',
}

export interface ButtonProps extends JSX.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  icon?: IconName
}

export function Button(props: ButtonProps) {
  const [local, rest] = splitProps(props, ['variant', 'size', 'loading', 'icon', 'class', 'children', 'disabled', 'type'])
  return (
    <button
      type={local.type ?? 'button'}
      disabled={local.disabled || local.loading}
      aria-busy={local.loading}
      class={`inline-flex shrink-0 items-center justify-center rounded-lg font-medium transition-colors disabled:cursor-not-allowed ${
        variants[local.variant ?? 'primary']
      } ${sizes[local.size ?? 'md']} ${local.class ?? ''}`}
      {...rest}
    >
      <Show when={local.loading} fallback={local.icon && <Icon name={local.icon} class="size-4" />}>
        <Spinner class="size-4" />
      </Show>
      {local.children}
    </button>
  )
}

export function IconButton(props: JSX.ButtonHTMLAttributes<HTMLButtonElement> & { icon: IconName; label: string; tone?: 'default' | 'danger' }) {
  const [local, rest] = splitProps(props, ['icon', 'label', 'tone', 'class'])
  return (
    <button
      type="button"
      aria-label={local.label}
      title={local.label}
      class={`inline-flex size-8 items-center justify-center rounded-md transition-colors disabled:opacity-40 ${
        local.tone === 'danger' ? 'text-slate-500 hover:bg-red-50 hover:text-red-600' : 'text-slate-500 hover:bg-slate-100 hover:text-slate-900'
      } ${local.class ?? ''}`}
      {...rest}
    >
      <Icon name={local.icon} class="size-4" />
    </button>
  )
}
