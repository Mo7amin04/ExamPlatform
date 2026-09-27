import { For, Index, Match, Show, Switch, createUniqueId } from 'solid-js'
import { SelectInput, TextArea, TextInput } from '~/components/forms/Fields'
import { Button, IconButton } from '~/components/ui/Button'
import type { QuestionContent, QuestionOptionInput, QuestionType } from '~/types/models'
import { bloomOptions, difficultyOptions, questionTypeOptions, usesOptions } from './questionMeta'

export type ContentUpdater = <K extends keyof QuestionContent>(key: K, value: QuestionContent[K]) => void

const MAX_OPTIONS = 10
const MAX_PAIRS = 20
const letter = (i: number) => String.fromCharCode(65 + i)

/** Default option scaffolding for a newly selected question type. */
export function defaultOptionsFor(type: QuestionType): QuestionOptionInput[] {
  switch (type) {
    case 'MultipleChoice':
    case 'MultipleSelect':
      return Array.from({ length: 4 }, () => ({ text: '', isCorrect: false }))
    case 'TrueFalse':
      return [
        { text: 'True', isCorrect: true },
        { text: 'False', isCorrect: false },
      ]
    case 'Matching':
      return Array.from({ length: 3 }, () => ({ text: '', isCorrect: false, matchText: '' }))
    case 'Ordering':
      return Array.from({ length: 4 }, () => ({ text: '', isCorrect: false }))
    default:
      return []
  }
}

/** Client-side mirror of the server's content rules (the server remains authoritative). */
export function validateContent(q: QuestionContent): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!q.text.trim()) errors.text = 'Question text is required.'
  if (!(q.points > 0)) errors.points = 'Points must be greater than zero.'
  if ((q.type === 'ShortAnswer' || q.type === 'FillBlank') && !q.expectedAnswer?.trim())
    errors.expectedAnswer = 'An expected answer is required for this question type.'
  if (usesOptions(q.type)) {
    if (q.options.some((o) => !o.text.trim())) errors.options = 'Every option needs text.'
    const texts = q.options.map((o) => o.text.trim().toLowerCase()).filter(Boolean)
    if (new Set(texts).size !== texts.length) errors.options = 'Options must not contain duplicates.'
    const correct = q.options.filter((o) => o.isCorrect).length
    if ((q.type === 'MultipleChoice' || q.type === 'TrueFalse') && correct !== 1) errors.options = 'Mark exactly one correct answer.'
    if (q.type === 'MultipleSelect' && correct < 1) errors.options = 'Mark at least one correct answer.'
    if (q.type === 'Matching' && q.options.some((o) => !o.matchText?.trim())) errors.options = 'Every pair needs a matching item.'
    if (q.options.length < 2) errors.options = 'Add at least two options.'
  }
  return errors
}

/** Type-aware editor for the answerable part of a question. Used by the question form and the AI review dialog. */
export function QuestionContentEditor(props: {
  value: QuestionContent
  update: ContentUpdater
  errors: Record<string, string>
  lockType?: boolean
}) {
  const groupName = createUniqueId()

  const setOptions = (options: QuestionOptionInput[]) => props.update('options', options)
  const patchOption = (index: number, patch: Partial<QuestionOptionInput>) =>
    setOptions(props.value.options.map((o, i) => (i === index ? { ...o, ...patch } : o)))
  const setSingleCorrect = (index: number) => setOptions(props.value.options.map((o, i) => ({ ...o, isCorrect: i === index })))
  const removeOption = (index: number) => setOptions(props.value.options.filter((_, i) => i !== index))
  const addOption = () =>
    setOptions([...props.value.options, { text: '', isCorrect: false, matchText: props.value.type === 'Matching' ? '' : null }])
  const move = (index: number, delta: number) => {
    const next = [...props.value.options]
    const target = index + delta
    if (target < 0 || target >= next.length) return
    ;[next[index], next[target]] = [next[target], next[index]]
    setOptions(next)
  }

  const changeType = (type: QuestionType) => {
    props.update('type', type)
    props.update('options', defaultOptionsFor(type))
  }

  const maxOptions = () => (props.value.type === 'Matching' || props.value.type === 'Ordering' ? MAX_PAIRS : MAX_OPTIONS)

  return (
    <div class="space-y-5">
      <div class="grid gap-4 sm:grid-cols-4">
        <SelectInput
          class="sm:col-span-2"
          label="Question type"
          required
          options={questionTypeOptions}
          value={props.value.type}
          onChange={(v) => changeType(v as QuestionType)}
          disabled={props.lockType}
        />
        <SelectInput label="Difficulty" options={difficultyOptions} value={props.value.difficulty} onChange={(v) => props.update('difficulty', v as QuestionContent['difficulty'])} />
        <SelectInput label="Bloom level" options={bloomOptions} value={props.value.bloomLevel} onChange={(v) => props.update('bloomLevel', v as QuestionContent['bloomLevel'])} />
      </div>

      <TextArea
        label="Question"
        required
        rows={4}
        value={props.value.text}
        onInput={(e) => props.update('text', e.currentTarget.value)}
        error={props.errors.text}
        hint={props.value.type === 'FillBlank' ? 'Mark each blank with _____ (five underscores).' : undefined}
      />

      <Switch>
        <Match when={props.value.type === 'MultipleChoice' || props.value.type === 'TrueFalse' || props.value.type === 'MultipleSelect'}>
          <fieldset>
            <legend class="mb-2 text-sm font-medium text-slate-700">
              {props.value.type === 'MultipleSelect' ? 'Options — tick every correct answer' : 'Options — select the correct answer'}
            </legend>
            <div class="space-y-2">
              <Index each={props.value.options}>
                {(option, i) => (
                  <div class={`flex items-center gap-2 rounded-lg border p-2 ${option().isCorrect ? 'border-emerald-300 bg-emerald-50/60' : 'border-slate-200'}`}>
                    <input
                      type={props.value.type === 'MultipleSelect' ? 'checkbox' : 'radio'}
                      name={groupName}
                      class="size-4 accent-emerald-600"
                      checked={option().isCorrect}
                      aria-label={`Mark option ${letter(i)} as correct`}
                      onChange={(e) => (props.value.type === 'MultipleSelect' ? patchOption(i, { isCorrect: e.currentTarget.checked }) : setSingleCorrect(i))}
                    />
                    <span class="w-5 text-sm font-semibold text-slate-500">{letter(i)}.</span>
                    <input
                      class="h-9 min-w-0 flex-1 rounded-md border border-slate-300 px-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100 disabled:bg-slate-50"
                      value={option().text}
                      placeholder={`Option ${letter(i)}`}
                      aria-label={`Option ${letter(i)} text`}
                      disabled={props.value.type === 'TrueFalse'}
                      onInput={(e) => patchOption(i, { text: e.currentTarget.value })}
                    />
                    <Show when={props.value.type !== 'TrueFalse'}>
                      <IconButton icon="trash" label={`Remove option ${letter(i)}`} tone="danger" disabled={props.value.options.length <= 2} onClick={() => removeOption(i)} />
                    </Show>
                  </div>
                )}
              </Index>
            </div>
            <Show when={props.value.type !== 'TrueFalse' && props.value.options.length < maxOptions()}>
              <Button class="mt-2" size="sm" variant="ghost" icon="plus" onClick={addOption}>Add option</Button>
            </Show>
          </fieldset>
        </Match>

        <Match when={props.value.type === 'Matching'}>
          <fieldset>
            <legend class="mb-2 text-sm font-medium text-slate-700">Pairs — each item and its correct match (the right column is shuffled when printed)</legend>
            <div class="space-y-2">
              <Index each={props.value.options}>
                {(pair, i) => (
                  <div class="flex items-center gap-2">
                    <span class="w-5 text-sm font-semibold text-slate-500">{i + 1}.</span>
                    <input class="h-9 min-w-0 flex-1 rounded-md border border-slate-300 px-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
                      value={pair().text} placeholder="Item" aria-label={`Item ${i + 1}`} onInput={(e) => patchOption(i, { text: e.currentTarget.value })} />
                    <span class="text-slate-400 rtl:rotate-180">→</span>
                    <input class="h-9 min-w-0 flex-1 rounded-md border border-slate-300 px-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
                      value={pair().matchText ?? ''} placeholder="Matches" aria-label={`Match for item ${i + 1}`} onInput={(e) => patchOption(i, { matchText: e.currentTarget.value })} />
                    <IconButton icon="trash" label={`Remove pair ${i + 1}`} tone="danger" disabled={props.value.options.length <= 2} onClick={() => removeOption(i)} />
                  </div>
                )}
              </Index>
            </div>
            <Show when={props.value.options.length < maxOptions()}>
              <Button class="mt-2" size="sm" variant="ghost" icon="plus" onClick={addOption}>Add pair</Button>
            </Show>
          </fieldset>
        </Match>

        <Match when={props.value.type === 'Ordering'}>
          <fieldset>
            <legend class="mb-2 text-sm font-medium text-slate-700">Items in the correct order (scrambled when printed)</legend>
            <div class="space-y-2">
              <Index each={props.value.options}>
                {(item, i) => (
                  <div class="flex items-center gap-2">
                    <span class="w-5 text-sm font-semibold text-slate-500">{i + 1}.</span>
                    <input class="h-9 min-w-0 flex-1 rounded-md border border-slate-300 px-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
                      value={item().text} placeholder={`Step ${i + 1}`} aria-label={`Item ${i + 1}`} onInput={(e) => patchOption(i, { text: e.currentTarget.value })} />
                    <IconButton icon="up" label="Move up" disabled={i === 0} onClick={() => move(i, -1)} />
                    <IconButton icon="down" label="Move down" disabled={i === props.value.options.length - 1} onClick={() => move(i, 1)} />
                    <IconButton icon="trash" label={`Remove item ${i + 1}`} tone="danger" disabled={props.value.options.length <= 2} onClick={() => removeOption(i)} />
                  </div>
                )}
              </Index>
            </div>
            <Show when={props.value.options.length < maxOptions()}>
              <Button class="mt-2" size="sm" variant="ghost" icon="plus" onClick={addOption}>Add item</Button>
            </Show>
          </fieldset>
        </Match>

        <Match when={props.value.type === 'ShortAnswer' || props.value.type === 'FillBlank'}>
          <TextInput
            label={props.value.type === 'FillBlank' ? 'Correct answer(s) for the blank(s)' : 'Expected answer'}
            required
            value={props.value.expectedAnswer ?? ''}
            onInput={(e) => props.update('expectedAnswer', e.currentTarget.value)}
            error={props.errors.expectedAnswer}
          />
        </Match>

        <Match when={props.value.type === 'Essay'}>
          <TextArea
            label="Expected answer / marking rubric"
            rows={4}
            value={props.value.expectedAnswer ?? ''}
            onInput={(e) => props.update('expectedAnswer', e.currentTarget.value)}
            error={props.errors.expectedAnswer}
            hint="Key points and how marks are allocated. Printed only on the answer key."
          />
        </Match>
      </Switch>

      <Show when={props.errors.options}>
        <p class="text-xs font-medium text-red-600" role="alert">{props.errors.options}</p>
      </Show>

      <div class="grid gap-4 sm:grid-cols-4">
        <TextInput
          label="Points"
          type="number"
          min={0.25}
          step={0.25}
          required
          value={props.value.points}
          onInput={(e) => props.update('points', Number(e.currentTarget.value))}
          error={props.errors.points}
        />
        <TextArea
          class="sm:col-span-3"
          label="Explanation"
          rows={2}
          value={props.value.explanation ?? ''}
          onInput={(e) => props.update('explanation', e.currentTarget.value)}
          hint="Why the answer is correct. Shown on the answer key only."
          error={props.errors.explanation}
        />
      </div>
    </div>
  )
}

/** Read-only rendering of question content (used in previews and AI review). */
export function QuestionContentView(props: { value: QuestionContent; showAnswers?: boolean }) {
  return (
    <div class="space-y-2 text-sm">
      <p class="whitespace-pre-wrap text-slate-900">{props.value.text}</p>
      <Switch>
        <Match when={props.value.type === 'Matching'}>
          <ul class="space-y-1">
            <For each={props.value.options}>
              {(o, i) => (
                <li class="text-slate-700">
                  {i() + 1}. {o.text} <span class="text-slate-400">→</span> <span class={props.showAnswers ? 'font-medium text-emerald-700' : ''}>{o.matchText}</span>
                </li>
              )}
            </For>
          </ul>
        </Match>
        <Match when={props.value.type === 'Ordering'}>
          <ol class="list-inside list-decimal space-y-1 text-slate-700">
            <For each={props.value.options}>{(o) => <li>{o.text}</li>}</For>
          </ol>
        </Match>
        <Match when={usesOptions(props.value.type)}>
          <ul class="space-y-1">
            <For each={props.value.options}>
              {(o, i) => (
                <li class={props.showAnswers && o.isCorrect ? 'font-medium text-emerald-700' : 'text-slate-700'}>
                  {letter(i())}. {o.text} {props.showAnswers && o.isCorrect ? '✓' : ''}
                </li>
              )}
            </For>
          </ul>
        </Match>
      </Switch>
      <Show when={props.showAnswers && props.value.expectedAnswer}>
        <p class="rounded-md bg-emerald-50 px-2 py-1 text-emerald-800">
          <span class="font-medium">{props.value.type === 'Essay' ? 'Rubric: ' : 'Answer: '}</span>
          {props.value.expectedAnswer}
        </p>
      </Show>
      <Show when={props.showAnswers && props.value.explanation}>
        <p class="text-xs italic text-slate-500">{props.value.explanation}</p>
      </Show>
    </div>
  )
}
