import { t } from '~/utils/i18n'
import { A, useNavigate, useParams } from '@solidjs/router'
import { createResource, Match, Show, Switch } from 'solid-js'
import { ErrorState, LoadingState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { Card, PageHeader } from '~/components/ui/Primitives'
import { QuestionForm, type QuestionDraft } from '~/features/questions/QuestionForm'
import { QuestionStatusBadge, SourceBadge } from '~/features/questions/questionMeta'
import { getErrorMessage, isStatus } from '~/services/api'
import { questionsApi } from '~/services/questions.api'
import type { Question } from '~/types/models'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'

function toDraft(q: Question): QuestionDraft {
  return {
    courseId: q.courseId,
    topicId: q.topicId,
    text: q.text,
    type: q.type,
    difficulty: q.difficulty,
    bloomLevel: q.bloomLevel,
    points: q.points,
    explanation: q.explanation ?? '',
    expectedAnswer: q.expectedAnswer ?? '',
    status: q.status,
    options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect, matchText: o.matchText })),
    tags: [...q.tags],
  }
}

export default function QuestionEditPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [question, { refetch }] = createResource(() => params.id, questionsApi.get)

  const duplicate = async () => {
    try {
      const copy = await questionsApi.duplicate(params.id)
      toast.success(t('Question duplicated'), t('You are now editing the copy.'))
      navigate(`/questions/${copy.id}/edit`)
    } catch (err) {
      toast.error(t('Could not duplicate question'), getErrorMessage(err))
    }
  }

  return (
    <Switch>
      <Match when={question.error && isStatus(question.error, 403)}>
        <ForbiddenPage />
      </Match>
      <Match when={question.error}>
        <Card><ErrorState error={question.error} onRetry={refetch} /></Card>
      </Match>
      <Match when={!question()}>
        <LoadingState />
      </Match>
      <Match when={question()}>
        {(q) => (
          <>
            <PageHeader
              breadcrumb={<A href={`/courses/${q().courseId}/questions`} class="hover:text-slate-700">{q().courseCode}{' '}{t('· Question Bank')}</A>}
              title={<span class="flex items-center gap-2">{t('Edit question')}{' '}<QuestionStatusBadge value={q().status} /><SourceBadge value={q().source} /></span>}
              description={t(q().usageCount === 1 ? 'Used in {count} exam.' : 'Used in {count} exams.', { count: q().usageCount })}
              actions={<Button variant="secondary" icon="copy" onClick={duplicate}>{t('Duplicate')}</Button>}
            />
            <Show when={q().source === 'AI' && q().status === 'Draft'}>
              <p class="-mt-3 mb-5 text-sm text-violet-700">
                {t('This question was generated with AI. Review it carefully and set the status to Approved once verified.')}
              </p>
            </Show>
            {/* Keyed on id so navigating to a duplicate re-initializes the form. */}
            <Show when={q().id} keyed>
              <QuestionForm
                initial={toDraft(q())}
                submitLabel={t('Save changes')}
                lockedReason={
                  q().isUsedInPublishedExam
                    ? t('This question is part of a published exam, so its content is frozen to preserve the exam record. Duplicate it to create an editable copy.')
                    : undefined
                }
                onCancel={() => navigate(`/courses/${q().courseId}/questions`)}
                onSubmit={async (input) => {
                  await questionsApi.update(q().id, input)
                  toast.success(t('Question saved'))
                  navigate(`/courses/${input.courseId}/questions`)
                }}
              />
            </Show>
          </>
        )}
      </Match>
    </Switch>
  )
}
