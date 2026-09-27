import { For, Show, createUniqueId, splitProps, type JSX, type ParentProps } from 'solid-js'

const controlBase =
  'block w-full rounded-lg border bg-white px-3 text-sm text-slate-900 shadow-sm transition-colors placeholder:text-slate-400 focus:outline-none focus:ring-2 disabled:bg-slate-50 disabled:text-slate-500'
const controlState = (error?: string) =>
  error
    ? 'border-red-400 focus:border-red-500 focus:ring-red-200'
    : 'border-slate-300 focus:border-brand-500 focus:ring-brand-100'

interface FieldProps {
  label?: string
  error?: string
  hint?: string
  required?: boolean
  class?: string
  id?: string
}

/** Label + control + hint/error, wired with aria attributes. */
export function Field(props: ParentProps<FieldProps & { for: string }>) {
  return (
    <div class={props.class}>
      <Show when={props.label}>
        <label for={props.for} class="mb-1.5 block text-sm font-medium text-slate-700">
          {props.label}
          <Show when={props.required}>
            <span class="ms-0.5 text-red-500" aria-hidden="true">*</span>
          </Show>
        </label>
      </Show>
      {props.children}
      <Show when={props.error} fallback={<Show when={props.hint}><p id={`${props.for}-hint`} class="mt-1 text-xs text-slate-500">{props.hint}</p></Show>}>
        <p id={`${props.for}-error`} class="mt-1 text-xs font-medium text-red-600" role="alert">
          {props.error}
        </p>
      </Show>
    </div>
  )
}

export function TextInput(props: FieldProps & JSX.InputHTMLAttributes<HTMLInputElement>) {
  const [local, rest] = splitProps(props, ['label', 'error', 'hint', 'required', 'class', 'id'])
  const id = local.id ?? createUniqueId()
  return (
    <Field for={id} label={local.label} error={local.error} hint={local.hint} required={local.required} class={local.class}>
      <input
        id={id}
        required={local.required}
        aria-invalid={!!local.error}
        aria-describedby={local.error ? `${id}-error` : local.hint ? `${id}-hint` : undefined}
        class={`${controlBase} ${controlState(local.error)} h-10`}
        {...rest}
      />
    </Field>
  )
}

export function TextArea(props: FieldProps & JSX.TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const [local, rest] = splitProps(props, ['label', 'error', 'hint', 'required', 'class', 'id'])
  const id = local.id ?? createUniqueId()
  return (
    <Field for={id} label={local.label} error={local.error} hint={local.hint} required={local.required} class={local.class}>
      <textarea
        id={id}
        required={local.required}
        aria-invalid={!!local.error}
        aria-describedby={local.error ? `${id}-error` : local.hint ? `${id}-hint` : undefined}
        class={`${controlBase} ${controlState(local.error)} py-2`}
        rows={3}
        {...rest}
      />
    </Field>
  )
}

export interface SelectOption {
  value: string
  label: string
}

export function SelectInput(
  props: FieldProps &
    Omit<JSX.SelectHTMLAttributes<HTMLSelectElement>, 'onChange'> & {
      options: SelectOption[]
      placeholder?: string
      onChange?: (value: string) => void
    },
) {
  const [local, rest] = splitProps(props, ['label', 'error', 'hint', 'required', 'class', 'id', 'options', 'placeholder', 'onChange', 'value'])
  const id = local.id ?? createUniqueId()
  return (
    <Field for={id} label={local.label} error={local.error} hint={local.hint} required={local.required} class={local.class}>
      <select
        id={id}
        required={local.required}
        aria-invalid={!!local.error}
        class={`${controlBase} ${controlState(local.error)} h-10 pe-8`}
        value={local.value ?? ''}
        onChange={(e) => local.onChange?.(e.currentTarget.value)}
        {...rest}
      >
        <Show when={local.placeholder !== undefined}>
          <option value="">{local.placeholder}</option>
        </Show>
        <For each={local.options}>
          {(o) => (
            <option value={o.value} selected={o.value === (local.value ?? '')}>
              {o.label}
            </option>
          )}
        </For>
      </select>
    </Field>
  )
}

export function Checkbox(props: { checked: boolean; onChange: (checked: boolean) => void; label: JSX.Element; disabled?: boolean; class?: string }) {
  const id = createUniqueId()
  return (
    <label for={id} class={`inline-flex cursor-pointer items-center gap-2 text-sm text-slate-700 ${props.class ?? ''}`}>
      <input
        id={id}
        type="checkbox"
        class="size-4 rounded border-slate-300 text-brand-600 accent-brand-600 focus:ring-brand-500"
        checked={props.checked}
        disabled={props.disabled}
        onChange={(e) => props.onChange(e.currentTarget.checked)}
      />
      {props.label}
    </label>
  )
}
