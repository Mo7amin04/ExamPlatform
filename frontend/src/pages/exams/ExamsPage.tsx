import { useNavigate, useSearchParams } from '@solidjs/router'
import { createResource, createSignal, Match, Switch } from 'solid-js'
import { createStore } from 'solid-js/store'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { SelectInput, TextInput } from '~/components/forms/Fields'
import { DataTable, Pagination } from '~/components/tables/DataTable'
import { Button, IconButton } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, PageHeader, SkeletonRows } from '~/components/ui/Primitives'
import { ExamStatusBadge, examStatusOptions, examTypeOptions } from '~/features/exams/examMeta'
import { getErrorMessage } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import { examsApi } from '~/services/exams.api'
import type { ExamFilters, ExamStatus, ExamSummary, ExamType } from '~/types/models'
import { formatDate, formatDuration } from '~/utils/format'

export default function ExamsPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const confirm = useConfirm()
  const [filters, setFilters] = createStore<ExamFilters>({
    page: 1,
    pageSize: 20,
    courseId: typeof params.courseId === 'string' ? params.courseId : undefined,
  })
  const [courses] = createResource(() => coursesApi.list())
  const [result, { refetch }] = createResource(() => ({ ...filters }), examsApi.list)
  const [search, setSearch] = createSignal('')

  let timer: ReturnType<typeof setTimeout> | undefined
  const onSearch = (value: string) => {
    setSearch(value)
    clearTimeout(timer)
    timer = setTimeout(() => setFilters({ search: value || undefined, page: 1 }), 300)
  }

  const remove = (exam: ExamSummary) =>
    confirm.ask({
      title: `Delete "${exam.title}"?`,
      description: 'Never-published exams are deleted. Exams with published history are archived to preserve records.',
      confirmLabel: 'Delete',
      onConfirm: async () => {
        try {
          const response = await examsApi.remove(exam.id)
          toast.success(response.data.archived ? 'Exam archived' : 'Exam deleted', response.message)
          refetch()
        } catch (err) {
          toast.error('Could not delete exam', getErrorMessage(err))
        }
      },
    })

  return (
    <>
      <PageHeader
        title="Exams"
        description="Build, preview, publish and export exams."
        actions={<Button icon="plus" onClick={() => navigate('/exams/new')}>New exam</Button>}
      />
      <Card>
        <div class="grid gap-3 border-b border-slate-100 p-4 sm:grid-cols-2 lg:grid-cols-4">
          <TextInput placeholder="Search by title or course…" aria-label="Search exams" value={search()} onInput={(e) => onSearch(e.currentTarget.value)} />
          <SelectInput
            aria-label="Course"
            placeholder="All courses"
            options={(courses() ?? []).map((c) => ({ value: c.id, label: c.code }))}
            value={filters.courseId ?? ''}
            onChange={(v) => setFilters({ courseId: v || undefined, page: 1 })}
          />
          <SelectInput aria-label="Type" placeholder="All types" options={examTypeOptions} value={filters.type ?? ''}
            onChange={(v) => setFilters({ type: (v || undefined) as ExamType | undefined, page: 1 })} />
          <SelectInput aria-label="Status" placeholder="Active (not archived)" options={examStatusOptions} value={filters.status ?? ''}
            onChange={(v) => setFilters({ status: (v || undefined) as ExamStatus | undefined, page: 1 })} />
        </div>
        <Switch>
          <Match when={result.error}>
            <ErrorState error={result.error} onRetry={refetch} />
          </Match>
          <Match when={!result.latest}>
            <SkeletonRows rows={6} />
          </Match>
          <Match when={result.latest?.items.length === 0}>
            <EmptyState icon="exam" title="No exams found" description="Create an exam and add questions from your question bank."
              action={<Button size="sm" icon="plus" onClick={() => navigate('/exams/new')}>New exam</Button>} />
          </Match>
          <Match when={result.latest}>
            {(data) => (
              <>
                <DataTable
                  rows={data().items}
                  rowKey={(e) => e.id}
                  onRowClick={(e) => navigate(`/exams/${e.id}/edit`)}
                  columns={[
                    {
                      header: 'Exam',
                      cell: (e) => (
                        <div>
                          <p class="font-medium text-slate-900">{e.title}</p>
                          <p class="text-xs text-slate-500">{e.courseCode} · {e.courseName}</p>
                        </div>
                      ),
                    },
                    { header: 'Type', cell: (e) => e.type },
                    { header: 'Questions', cell: (e) => <span class="tabular-nums">{e.questionCount}</span> },
                    { header: 'Total', cell: (e) => <span class="tabular-nums">{e.totalPoints} pts</span> },
                    { header: 'Duration', cell: (e) => formatDuration(e.durationMinutes) },
                    { header: 'Date', cell: (e) => <span class="whitespace-nowrap">{formatDate(e.examDate)}</span> },
                    { header: 'Status', cell: (e) => <ExamStatusBadge value={e.status} /> },
                    {
                      header: '',
                      class: 'text-end',
                      cell: (e) => (
                        <div class="flex justify-end gap-0.5" onClick={(ev) => ev.stopPropagation()}>
                          <IconButton icon="eye" label="Preview" onClick={() => navigate(`/exams/${e.id}/preview`)} />
                          <IconButton icon="edit" label="Open builder" onClick={() => navigate(`/exams/${e.id}/edit`)} />
                          <IconButton icon="trash" label="Delete" tone="danger" disabled={e.status === 'Archived'} onClick={() => remove(e)} />
                        </div>
                      ),
                    },
                  ]}
                />
                <Pagination pagination={data().pagination} onPageChange={(page) => setFilters('page', page)} />
              </>
            )}
          </Match>
        </Switch>
      </Card>
      <confirm.Dialog />
    </>
  )
}
