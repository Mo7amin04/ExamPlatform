import { t, translateMessage } from '~/utils/i18n'
import { For, Show } from 'solid-js'
import { IconButton } from '~/components/ui/Button'
import { Badge } from '~/components/ui/Primitives'
import { QuestionContentView } from '~/features/questions/QuestionContentEditor'
import { DifficultyBadge, QuestionTypeBadge } from '~/features/questions/questionMeta'
import type { GeneratedQuestion } from '~/types/models'
import { formatPoints } from '~/utils/format'

export interface ReviewItem {
  key: string
  question: GeneratedQuestion
  selected: boolean
  edited: boolean
}

/** One AI proposal under teacher review. Invalid proposals cannot be selected until they are fixed. */
export function GeneratedQuestionCard(props: {
  item: ReviewItem
  index: number
  onToggle: (selected: boolean) => void
  onEdit: () => void
  onDiscard: () => void
}) {
  const q = () => props.item.question
  return (
    <li
      class={`panel p-4 ${
        props.item.selected ? 'bg-brand-50' : q().isValid ? '' : 'bg-amber-50'
      }`}
    >
      <div class="flex items-start gap-3">
        <input
          type="checkbox"
          class="mt-1 size-4 accent-brand-600"
          checked={props.item.selected}
          disabled={!q().isValid}
          aria-label={t('Select question {number}', { number: props.index + 1 })}
          onChange={(e) => props.onToggle(e.currentTarget.checked)}
        />
        <div class="min-w-0 flex-1">
          <div class="mb-2 flex flex-wrap items-center gap-1.5">
            <span class="text-sm font-semibold text-slate-900">{t('Question {number}', { number: props.index + 1 })}</span>
            <QuestionTypeBadge value={q().type} />
            <DifficultyBadge value={q().difficulty} />
            <Badge tone="gray">{t(q().bloomLevel)}</Badge>
            <span class="text-xs text-slate-500">{formatPoints(q().points)}</span>
            <Show when={props.item.edited}><Badge tone="blue">{t('Edited')}</Badge></Show>
            <Show when={!q().isValid}><Badge tone="amber">{t('Needs fixing')}</Badge></Show>
          </div>
          <QuestionContentView value={q()} showAnswers />
          <Show when={q().issues.length}>
            <div class="mt-3 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
              <p class="font-semibold">{t('Validation issues')}</p>
              <ul class="list-inside list-disc">
                <For each={q().issues}>{(issue) => <li>{translateMessage(issue)}</li>}</For>
              </ul>
            </div>
          </Show>
        </div>
        <div class="flex shrink-0 flex-col gap-1">
          <IconButton icon="edit" label={t('Edit question')} onClick={props.onEdit} />
          <IconButton icon="trash" label={t('Discard question')} tone="danger" onClick={props.onDiscard} />
        </div>
      </div>
    </li>
  )
}
