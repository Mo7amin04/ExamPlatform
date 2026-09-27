import { useNavigate, useParams } from '@solidjs/router'
import { createResource, Match, Switch } from 'solid-js'
import { EmptyState, ErrorState, LoadingState } from '~/components/feedback/States'
import { Button } from '~/components/ui/Button'
import { ExamPaper } from '~/features/exams/ExamPaper'
import { PrintToolbar } from '~/features/exams/PrintToolbar'
import { isStatus } from '~/services/api'
import { examsApi } from '~/services/exams.api'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'

export default function ExamPreviewPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [preview, { refetch }] = createResource(() => params.id, examsApi.preview)

  return (
    <div class="min-h-screen bg-slate-100 print:bg-white">
      <PrintToolbar examId={params.id} title={preview()?.header.title ?? 'Exam preview'}>
        <Button variant="secondary" icon="key" onClick={() => navigate(`/exams/${params.id}/answer-key`)}>Answer key</Button>
      </PrintToolbar>
      <div class="px-4 py-8 print:p-0">
        <Switch>
          <Match when={preview.error && isStatus(preview.error, 403)}><ForbiddenPage /></Match>
          <Match when={preview.error}><ErrorState error={preview.error} onRetry={refetch} /></Match>
          <Match when={!preview()}><LoadingState label="Preparing preview…" /></Match>
          <Match when={preview()?.questions.length === 0}>
            <EmptyState icon="exam" title="This exam has no questions yet" description="Add questions in the exam builder to preview the paper." />
          </Match>
          <Match when={preview()}>{(p) => <ExamPaper preview={p()} />}</Match>
        </Switch>
      </div>
    </div>
  )
}
