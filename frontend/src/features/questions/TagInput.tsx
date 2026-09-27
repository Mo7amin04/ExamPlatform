import { createSignal, For, Show } from 'solid-js'

/** Chip-style tag editor: Enter or comma adds a tag, Backspace on empty input removes the last one. */
export function TagInput(props: { tags: string[]; onChange: (tags: string[]) => void; error?: string; max?: number }) {
  const [value, setValue] = createSignal('')
  const max = () => props.max ?? 10

  const add = (raw: string) => {
    const tag = raw.trim().toLowerCase().replace(/\s+/g, ' ').slice(0, 50)
    if (!tag || props.tags.includes(tag) || props.tags.length >= max()) return
    props.onChange([...props.tags, tag])
  }

  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      add(value())
      setValue('')
    } else if (e.key === 'Backspace' && !value() && props.tags.length) {
      props.onChange(props.tags.slice(0, -1))
    }
  }

  return (
    <div>
      <label for="tag-input" class="mb-1.5 block text-sm font-medium text-slate-700">Tags</label>
      <div class={`flex min-h-10 flex-wrap items-center gap-1.5 rounded-lg border bg-white px-2 py-1.5 shadow-sm focus-within:ring-2 ${props.error ? 'border-red-400 focus-within:ring-red-200' : 'border-slate-300 focus-within:border-brand-500 focus-within:ring-brand-100'}`}>
        <For each={props.tags}>
          {(tag) => (
            <span class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 text-xs text-slate-700">
              {tag}
              <button type="button" class="text-slate-400 hover:text-slate-700" aria-label={`Remove tag ${tag}`} onClick={() => props.onChange(props.tags.filter((t) => t !== tag))}>
                ×
              </button>
            </span>
          )}
        </For>
        <input
          id="tag-input"
          class="min-w-[6rem] flex-1 border-0 bg-transparent p-0.5 text-sm focus:outline-none"
          placeholder={props.tags.length ? '' : 'Type and press Enter'}
          value={value()}
          onInput={(e) => setValue(e.currentTarget.value)}
          onKeyDown={onKeyDown}
          onBlur={() => {
            add(value())
            setValue('')
          }}
        />
      </div>
      <Show when={props.error} fallback={<p class="mt-1 text-xs text-slate-500">Up to {max()} tags.</p>}>
        <p class="mt-1 text-xs font-medium text-red-600">{props.error}</p>
      </Show>
    </div>
  )
}
