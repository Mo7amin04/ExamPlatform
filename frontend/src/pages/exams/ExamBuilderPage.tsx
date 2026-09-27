import { A, useNavigate, useParams } from '@solidjs/router'
import { createEffect, createResource, createSignal, Match, on, Show, Switch } from 'solid-js'
import { createStore } from 'solid-js/store'
import { EmptyState, ErrorState, InlineAlert, LoadingState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, CardHeader, PageHeader } from '~/components/ui/Primitives'
import { ExamQuestionList } from '~/features/exams/ExamQuestionList'
import { ExamSettingsFields, validateExam } from '~/features/exams/ExamSettingsForm'
import { QuestionPickerDialog } from '~/features/exams/QuestionPickerDialog'
import { ExamStatusBadge, isExamEditable } from '~/features/exams/examMeta'
import { ExportMenu } from '~/features/exports/ExportMenu'
import { QuestionPreviewDialog } from '~/features/questions/QuestionPreviewDialog'
import { getErrorMessage, getFieldErrors, isStatus } from '~/services/api'
import { examsApi } from '~/services/exams.api'
import type { ExamDetail, ExamInput, ExamQuestion } from '~/types/models'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'
import { formatDuration } from '~/utils/format'

function toInput(exam: ExamDetail): ExamInput {
  return {
    courseId: exam.courseId,
    title: exam.title,
    description: exam.description ?? '',
    instructions: exam.instructions ?? '',
    type: exam.type,
    durationMinutes: exam.durationMinutes,
    examDate: exam.examDate,
  }
}

export default function ExamBuilderPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const confirm = useConfirm()
  const [exam, { mutate, refetch }] = createResource(() => params.id, examsApi.get)
  const [settings, setSettings] = createStore<ExamInput>({ courseId: '', title: '', type: 'Quiz', durationMinutes: 60 })
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [dirty, setDirty] = createSignal(false)
  const [saving, setSaving] = createSignal(false)
  const [busyAction, setBusyAction] = createSignal<string | null>(null)
  const [pickerOpen, setPickerOpen] = createSignal(false)
  const [previewId, setPreviewId] = createSignal<string | null>(null)

  // Load settings into the form whenever a different exam (or fresh server state) arrives and nothing is pending.
  createEffect(on(() => exam()?.id, () => {
    const current = exam()
    if (current) {
      setSettings(toInput(current))
      setDirty(false)
    }
  }))

  const editable = () => !!exam() && isExamEditable(exam()!.status)

  /** Runs a mutation that returns the updated exam; on failure reloads server state. */
  const run = async (label: string, action: () => Promise<ExamDetail>, success?: string) => {
    setBusyAction(label)
    try {
      const updated = await action()
      mutate(updated)
      if (success) toast.success(success)
      return true
    } catch (err) {
      toast.error('Action failed', getErrorMessage(err))
      refetch()
      return false
    } finally {
      setBusyAction(null)
    }
  }

  const saveSettings = async () => {
    const clientErrors = validateExam(settings)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length) return false
    setSaving(true)
    try {
      const updated = await examsApi.update(params.id, { ...settings })
      mutate(updated)
      setDirty(false)
      toast.success('Draft saved')
      return true
    } catch (err) {
      setErrors(getFieldErrors(err))
      toast.error('Could not save exam', getErrorMessage(err))
      return false
    } finally {
      setSaving(false)
    }
  }

  const reorder = (ids: string[]) => {
    const current = exam()
    if (!current) return
    // Optimistic update for smooth drag & drop.
    const byId = new Map(current.questions.map((q) => [q.questionId, q]))
    mutate({ ...current, questions: ids.map((id, i) => ({ ...byId.get(id)!, order: i + 1 })) })
    void run('reorder', () => examsApi.reorder(params.id, ids))
  }

  const removeQuestion = (q: ExamQuestion) =>
    confirm.ask({
      title: 'Remove question from exam?',
      description: 'The question stays in the question bank; it is only removed from this exam.',
      confirmLabel: 'Remove',
      onConfirm: () => run('remove', () => examsApi.removeQuestion(params.id, q.questionId), 'Question removed'),
    })

  const publish = () =>
    confirm.ask({
      title: 'Publish this exam?',
      description: 'A snapshot of the exam is recorded and the exam becomes read-only. You can move it back to draft later if changes are needed.',
      confirmLabel: 'Publish',
      tone: 'primary',
      onConfirm: async () => {
        if (dirty() && !(await saveSettings())) return
        await run('publish', () => examsApi.publish(params.id), 'Exam published')
      },
    })

  const moveToDraft = () =>
    confirm.ask({
      title: 'Move exam back to draft?',
      description: 'The exam becomes editable again. Its published snapshot is kept in the version history.',
      confirmLabel: 'Move to draft',
      tone: 'primary',
      onConfirm: () => run('draft', () => examsApi.moveToDraft(params.id), 'Exam moved back to draft'),
    })

  const remove = () =>
    confirm.ask({
      title: 'Delete this exam?',
      description: 'Never-published exams are deleted permanently. Exams with published history are archived instead.',
      confirmLabel: 'Delete',
      onConfirm: async () => {
        try {
          const response = await examsApi.remove(params.id)
          toast.success(response.data.archived ? 'Exam archived' : 'Exam deleted')
          navigate('/exams')
        } catch (err) {
          toast.error('Could not delete exam', getErrorMessage(err))
        }
      },
    })

  const goToPreview = async (path: string) => {
    if (dirty() && !(await saveSettings())) return
    navigate(path)
  }

  return (
    <Switch>
      <Match when={exam.error && isStatus(exam.error, 403)}>
        <ForbiddenPage />
      </Match>
      <Match when={exam.error}>
        <Card><ErrorState error={exam.error} onRetry={refetch} /></Card>
      </Match>
      <Match when={!exam()}>
        <LoadingState />
      </Match>
      <Match when={exam()}>
        {(e) => (
          <>
            <PageHeader
              breadcrumb={<A href="/exams" class="hover:text-slate-700">Exams</A>}
              title={<span class="flex flex-wrap items-center gap-3">{e().title} <ExamStatusBadge value={e().status} /></span>}
              description={`${e().courseCode} · ${e().type} · ${formatDuration(e().durationMinutes)}${e().versionCount ? ` · ${e().versionCount} published version(s)` : ''}`}
              actions={
                <>
                  <Button variant="secondary" icon="eye" onClick={() => goToPreview(`/exams/${e().id}/preview`)}>Preview</Button>
                  <Button variant="secondary" icon="key" onClick={() => goToPreview(`/exams/${e().id}/answer-key`)}>Answer key</Button>
                  <ExportMenu examId={e().id} disabled={e().questions.length === 0} />
                  <Show when={editable()} fallback={
                    <Show when={e().status === 'Published'}>
                      <Button variant="secondary" icon="edit" loading={busyAction() === 'draft'} onClick={moveToDraft}>Move to draft</Button>
                    </Show>
                  }>
                    <Button icon="check" loading={busyAction() === 'publish'} disabled={e().questions.length === 0} onClick={publish}>Publish</Button>
                  </Show>
                </>
              }
            />

            <Show when={e().status === 'Published'}>
              <div class="mb-6">
                <InlineAlert tone="success" title="This exam is published">
                  Published exams are read-only to preserve what students received. Use "Move to draft" to make changes.
                </InlineAlert>
              </div>
            </Show>
            <Show when={e().status === 'Archived'}>
              <div class="mb-6"><InlineAlert tone="warning" title="This exam is archived">Archived exams are kept for records and cannot be edited.</InlineAlert></div>
            </Show>

            <div class="grid gap-6 xl:grid-cols-[minmax(0,24rem)_1fr]">
              <Card class="self-start">
                <CardHeader title="Exam settings" />
                <div class="p-5">
                  <ExamSettingsFields
                    value={settings}
                    onChange={(k, v) => {
                      setSettings(k, v as never)
                      setDirty(true)
                    }}
                    errors={errors()}
                    disabled={!editable()}
                    courseLocked={e().questions.length > 0}
                  />
                  <Show when={editable()}>
                    <div class="mt-5 flex flex-wrap gap-2">
                      <Button onClick={saveSettings} loading={saving()} disabled={!dirty()}>Save draft</Button>
                      <Show when={e().status === 'Draft'}>
                        <Button variant="secondary" loading={busyAction() === 'ready'} disabled={!e().questions.length}
                          onClick={() => run('ready', () => examsApi.markReady(params.id), 'Exam marked as ready')}>
                          Mark ready
                        </Button>
                      </Show>
                    </div>
                    <Show when={dirty()}><p class="mt-2 text-xs text-amber-700">You have unsaved changes.</p></Show>
                  </Show>
                  <div class="mt-5 border-t border-slate-100 pt-4">
                    <Button variant="ghost" size="sm" icon="trash" class="text-red-600 hover:bg-red-50 hover:text-red-700" onClick={remove} disabled={e().status === 'Archived'}>
                      Delete exam
                    </Button>
                  </div>
                </div>
              </Card>

              <Card class="self-start">
                <CardHeader
                  title={`Questions (${e().questions.length})`}
                  description={<>Total: <strong class="text-slate-900">{e().totalPoints} points</strong> — calculated from the questions below.</>}
                  actions={
                    <Show when={editable()}>
                      <Button size="sm" icon="plus" onClick={() => setPickerOpen(true)}>Add question</Button>
                      <Button size="sm" variant="ai" icon="sparkles" onClick={() => navigate(`/ai/generate?courseId=${e().courseId}&examId=${e().id}`)}>
                        Generate with AI
                      </Button>
                    </Show>
                  }
                />
                <div class={`p-5 ${busyAction() ? 'opacity-70' : ''}`}>
                  <Show
                    when={e().questions.length}
                    fallback={
                      <EmptyState
                        icon="questions"
                        title="No questions yet"
                        description="Add questions from the question bank or generate new ones with AI."
                        action={<Show when={editable()}><Button size="sm" icon="plus" onClick={() => setPickerOpen(true)}>Add question</Button></Show>}
                      />
                    }
                  >
                    <ExamQuestionList
                      questions={e().questions}
                      editable={editable()}
                      onReorder={reorder}
                      onUpdate={(qid, points, section) => run('update', () => examsApi.updateQuestion(params.id, qid, points, section))}
                      onRemove={removeQuestion}
                      onOpen={setPreviewId}
                    />
                    <div class="mt-4 flex justify-end border-t border-slate-100 pt-3 text-sm">
                      <span class="text-slate-600">Total points:&nbsp;</span>
                      <strong class="tabular-nums text-slate-900">{e().totalPoints}</strong>
                    </div>
                  </Show>
                </div>
              </Card>
            </div>

            <QuestionPickerDialog
              open={pickerOpen()}
              examId={e().id}
              courseId={e().courseId}
              onOpenChange={setPickerOpen}
              onAdd={async (ids) => {
                await run('add', () => examsApi.addQuestions(params.id, ids), `${ids.length} question(s) added`)
              }}
            />
            <QuestionPreviewDialog questionId={previewId()} onClose={() => setPreviewId(null)} />
            <confirm.Dialog />
          </>
        )}
      </Match>
    </Switch>
  )
}
