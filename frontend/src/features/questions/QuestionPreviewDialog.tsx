import { createResource, For, Match, Show, Switch } from 'solid-js'
import { ErrorState, LoadingState } from '~/components/feedback/States'
import { Modal } from '~/components/ui/Dialog'
import { questionsApi } from '~/services/questions.api'
import { formatPoints } from '~/utils/format'
import { QuestionContentView } from './QuestionContentEditor'
import { DifficultyBadge, QuestionStatusBadge, QuestionTypeBadge, SourceBadge } from './questionMeta'

export function QuestionPreviewDialog(props: { questionId: string | null; onClose: () => void }) {
  const [question, { refetch }] = createResource(() => props.questionId, questionsApi.get)

  return (
    <Modal open={!!props.questionId} onOpenChange={(open) => !open && props.onClose()} title="Question preview" size="lg">
      <Switch>
        <Match when={question.error}>
          <ErrorState error={question.error} onRetry={refetch} />
        </Match>
        <Match when={question.loading || !question()}>
          <LoadingState />
        </Match>
        <Match when={question()}>
          {(q) => (
            <div class="space-y-4">
              <div class="flex flex-wrap items-center gap-2">
                <QuestionTypeBadge value={q().type} />
                <DifficultyBadge value={q().difficulty} />
                <QuestionStatusBadge value={q().status} />
                <SourceBadge value={q().source} />
                <span class="ms-auto text-sm font-medium text-slate-600">{formatPoints(q().points)}</span>
              </div>
              <QuestionContentView value={q()} showAnswers />
              <dl class="grid grid-cols-2 gap-3 border-t border-slate-100 pt-4 text-sm sm:grid-cols-4">
                <div><dt class="text-xs text-slate-500">Course</dt><dd class="font-medium">{q().courseCode}</dd></div>
                <div><dt class="text-xs text-slate-500">Topic</dt><dd class="font-medium">{q().topicName ?? '—'}</dd></div>
                <div><dt class="text-xs text-slate-500">Bloom level</dt><dd class="font-medium">{q().bloomLevel}</dd></div>
                <div><dt class="text-xs text-slate-500">Used in exams</dt><dd class="font-medium">{q().usageCount}</dd></div>
              </dl>
              <Show when={q().tags.length}>
                <div class="flex flex-wrap gap-1.5">
                  <For each={q().tags}>{(tag) => <span class="rounded-md bg-slate-100 px-2 py-0.5 text-xs text-slate-600">#{tag}</span>}</For>
                </div>
              </Show>
            </div>
          )}
        </Match>
      </Switch>
    </Modal>
  )
}
