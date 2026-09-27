import { useNavigate, useParams } from '@solidjs/router'
import { createResource, Match, Switch } from 'solid-js'
import { EmptyState, ErrorState, LoadingState } from '~/components/feedback/States'
import { Button } from '~/components/ui/Button'
import { AnswerKeySheet } from '~/features/exams/ExamPaper'
import { PrintToolbar } from '~/features/exams/PrintToolbar'
import { isStatus } from '~/services/api'
import { examsApi } from '~/services/exams.api'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'

export default function AnswerKeyPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [answerKey, { refetch }] = createResource(() => params.id, examsApi.answerKey)

  return (
    <div class="min-h-screen bg-slate-100 print:bg-white">
      <PrintToolbar examId={params.id} title={`${answerKey()?.header.title ?? 'Exam'} — Answer key`}>
        <Button variant="secondary" icon="eye" onClick={() => navigate(`/exams/${params.id}/preview`)}>Exam paper</Button>
      </PrintToolbar>
      <div class="px-4 py-8 print:p-0">
        <Switch>
          <Match when={answerKey.error && isStatus(answerKey.error, 403)}><ForbiddenPage /></Match>
          <Match when={answerKey.error}><ErrorState error={answerKey.error} onRetry={refetch} /></Match>
          <Match when={!answerKey()}><LoadingState label="Preparing answer key…" /></Match>
          <Match when={answerKey()?.items.length === 0}>
            <EmptyState icon="key" title="No questions yet" description="Add questions to the exam to build its answer key." />
          </Match>
          <Match when={answerKey()}>{(k) => <AnswerKeySheet answerKey={k()} />}</Match>
        </Switch>
      </div>
    </div>
  )
}
