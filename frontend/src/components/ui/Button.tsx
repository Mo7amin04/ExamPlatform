import { splitProps, Show, type JSX } from 'solid-js'
import { Spinner } from './Spinner'
import { Icon, type IconName } from './Icon'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'ai'
type Size = 'sm' | 'md'

const variants: Record<Variant, string> = {
  primary: 'action-button bg-brand-600 text-white enabled:hover:bg-brand-700',
  secondary: 'action-button bg-white text-ink enabled:hover:bg-brand-50',
  ghost: 'icon-button font-semibold text-ink enabled:hover:bg-brand-50 disabled:opacity-50',
  danger: 'action-button bg-red-700 text-white enabled:hover:bg-red-800',
  ai: 'action-button bg-butter text-ink enabled:hover:bg-amber-300',
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
      class={`inline-flex shrink-0 items-center justify-center disabled:cursor-not-allowed ${
        variants[local.variant ?? 'primary']
      } ${sizes[local.size ?? 'md']} ${local.class ?? ''}`}
      {...rest}
    >
      <Show when={local.loading} fallback={local.icon && <Icon name={local.icon} class="size-[18px]" />}>
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
      class={`icon-button inline-flex size-8 items-center justify-center disabled:cursor-not-allowed disabled:opacity-40 ${
        local.tone === 'danger' ? 'text-red-700 enabled:hover:bg-peach' : 'text-ink enabled:hover:bg-brand-100'
      } ${local.class ?? ''}`}
      {...rest}
    >
      <Icon name={local.icon} class="size-4" />
    </button>
  )
}
