import { A, useNavigate, useParams } from '@solidjs/router'
import { createEffect, createResource, createSignal, Match, on, Show, Switch } from 'solid-js'
import { createStore } from 'solid-js/store'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { DataTable, Pagination } from '~/components/tables/DataTable'
import { Button, IconButton } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, PageHeader, SkeletonRows } from '~/components/ui/Primitives'
import { QuestionFilterBar } from '~/features/questions/QuestionFilters'
import { QuestionPreviewDialog } from '~/features/questions/QuestionPreviewDialog'
import { DifficultyBadge, QuestionStatusBadge, QuestionTypeBadge, SourceBadge } from '~/features/questions/questionMeta'
import { getErrorMessage } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import { questionsApi } from '~/services/questions.api'
import type { QuestionFilters, QuestionListItem } from '~/types/models'
import { formatPoints, truncate } from '~/utils/format'

/** Question bank for all accessible courses (/questions) or a single course (/courses/:id/questions). */
export default function QuestionBankPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const confirm = useConfirm()
  const [previewId, setPreviewId] = createSignal<string | null>(null)

  const [filters, setFilters] = createStore<QuestionFilters>({ page: 1, pageSize: 20, courseId: params.id })

  // Route param drives the course filter when viewing a single course.
  createEffect(on(() => params.id, (id) => setFilters({ courseId: id, topicId: undefined, page: 1 }), { defer: true }))

  const [course] = createResource(() => params.id, coursesApi.get)
  const [result, { refetch }] = createResource(() => ({ ...filters }), questionsApi.list)

  const applyFilters = (patch: Partial<QuestionFilters>) => setFilters({ ...patch, page: 1 })

  const duplicate = async (q: QuestionListItem) => {
    try {
      const copy = await questionsApi.duplicate(q.id)
      toast.success('Question duplicated')
      navigate(`/questions/${copy.id}/edit`)
    } catch (err) {
      toast.error('Could not duplicate question', getErrorMessage(err))
    }
  }

  const archiveOrDelete = (q: QuestionListItem) =>
    confirm.ask({
      title: q.usageCount > 0 ? 'Archive this question?' : 'Delete this question?',
      description:
        q.usageCount > 0
          ? 'The question is used in exams, so it will be archived (kept for exam history) instead of deleted.'
          : 'The question is not used in any exam and will be permanently deleted.',
      confirmLabel: q.usageCount > 0 ? 'Archive' : 'Delete',
      onConfirm: async () => {
        try {
          const response = await questionsApi.remove(q.id)
          toast.success(response.data.archived ? 'Question archived' : 'Question deleted', response.message)
          refetch()
        } catch (err) {
          toast.error('Action failed', getErrorMessage(err))
        }
      },
    })

  const restore = async (q: QuestionListItem) => {
    try {
      await questionsApi.setStatus(q.id, 'Draft')
      toast.success('Question restored as draft')
      refetch()
    } catch (err) {
      toast.error('Could not restore question', getErrorMessage(err))
    }
  }

  const newQuestionHref = () => (params.id ? `/questions/new?courseId=${params.id}` : '/questions/new')

  return (
    <>
      <PageHeader
        breadcrumb={
          <Show when={params.id}>
            <A href={`/courses/${params.id}`} class="hover:text-slate-700">{course()?.code ?? 'Course'}</A>
          </Show>
        }
        title={params.id && course() ? `${course()!.code} · Question Bank` : 'Question Bank'}
        description="Search, filter and manage reusable questions."
        actions={
          <>
            <Button variant="ai" icon="sparkles" onClick={() => navigate(params.id ? `/ai/generate?courseId=${params.id}` : '/ai/generate')}>
              Generate with AI
            </Button>
            <Button icon="plus" onClick={() => navigate(newQuestionHref())}>New question</Button>
          </>
        }
      />

      <Card>
        <QuestionFilterBar filters={filters} onChange={applyFilters} lockCourse={!!params.id} />
        <Switch>
          <Match when={result.error}>
            <ErrorState error={result.error} onRetry={refetch} />
          </Match>
          <Match when={result.loading && !result.latest}>
            <SkeletonRows rows={8} />
          </Match>
          <Match when={result.latest?.items.length === 0}>
            <EmptyState
              icon="questions"
              title="No questions found"
              description="Try different filters, create a question, or generate drafts with AI."
              action={<Button size="sm" icon="plus" onClick={() => navigate(newQuestionHref())}>New question</Button>}
            />
          </Match>
          <Match when={result.latest}>
            {(data) => (
              <div class={result.loading ? 'opacity-60 transition-opacity' : ''}>
                <DataTable
                  rows={data().items}
                  rowKey={(q) => q.id}
                  columns={[
                    {
                      header: 'Question',
                      class: 'max-w-xl',
                      cell: (q) => (
                        <button type="button" class="text-start" onClick={() => setPreviewId(q.id)}>
                          <span class="font-medium text-slate-900 hover:text-brand-700">{truncate(q.text, 140)}</span>
                          <span class="mt-1 flex flex-wrap gap-1 text-xs text-slate-500">
                            {q.courseCode}
                            {q.topicName ? ` · ${q.topicName}` : ''}
                            {q.tags.length ? ` · #${q.tags.join(' #')}` : ''}
                          </span>
                        </button>
                      ),
                    },
                    { header: 'Type', cell: (q) => <QuestionTypeBadge value={q.type} /> },
                    { header: 'Difficulty', cell: (q) => <DifficultyBadge value={q.difficulty} /> },
                    { header: 'Bloom', cell: (q) => <span class="text-slate-600">{q.bloomLevel}</span> },
                    { header: 'Points', cell: (q) => <span class="whitespace-nowrap tabular-nums">{formatPoints(q.points)}</span> },
                    {
                      header: 'Status',
                      cell: (q) => (
                        <div class="flex flex-wrap gap-1">
                          <QuestionStatusBadge value={q.status} />
                          <SourceBadge value={q.source} />
                        </div>
                      ),
                    },
                    { header: 'Used', cell: (q) => <span class="tabular-nums text-slate-600">{q.usageCount}</span> },
                    {
                      header: '',
                      class: 'text-end',
                      cell: (q) => (
                        <div class="flex justify-end gap-0.5">
                          <IconButton icon="eye" label="Preview" onClick={() => setPreviewId(q.id)} />
                          <IconButton icon="edit" label="Edit" onClick={() => navigate(`/questions/${q.id}/edit`)} />
                          <IconButton icon="copy" label="Duplicate" onClick={() => duplicate(q)} />
                          <Show
                            when={q.status !== 'Archived'}
                            fallback={<IconButton icon="refresh" label="Restore" onClick={() => restore(q)} />}
                          >
                            <IconButton icon={q.usageCount > 0 ? 'archive' : 'trash'} label={q.usageCount > 0 ? 'Archive' : 'Delete'} tone="danger" onClick={() => archiveOrDelete(q)} />
                          </Show>
                        </div>
                      ),
                    },
                  ]}
                />
                <Pagination pagination={data().pagination} onPageChange={(page) => setFilters('page', page)} />
              </div>
            )}
          </Match>
        </Switch>
      </Card>

      <QuestionPreviewDialog questionId={previewId()} onClose={() => setPreviewId(null)} />
      <confirm.Dialog />
    </>
  )
}
