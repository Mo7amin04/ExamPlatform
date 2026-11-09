import { t, translateMessage } from '~/utils/i18n'
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
import { formatDate, formatDuration, formatPoints } from '~/utils/format'

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
      title: t('Delete {name}?', { name: exam.title }),
      description: t('Never-published exams are deleted. Exams with published history are archived to preserve records.'),
      confirmLabel: t('Delete'),
      onConfirm: async () => {
        try {
          const response = await examsApi.remove(exam.id)
          toast.success(response.data.archived ? t('Exam archived') : t('Exam deleted'), response.message && translateMessage(response.message))
          refetch()
        } catch (err) {
          toast.error(t('Could not delete exam'), getErrorMessage(err))
        }
      },
    })

  return (
    <>
      <PageHeader
        title={t('Exams')}
        description={t('Build, preview, publish and export exams.')}
        actions={<Button icon="plus" onClick={() => navigate('/exams/new')}>{t('New exam')}</Button>}
      />
      <Card>
        <div class="grid gap-3 border-b border-slate-100 p-4 sm:grid-cols-2 lg:grid-cols-4">
          <TextInput placeholder={t('Search by title or subject…')} aria-label={t('Search exams')} value={search()} onInput={(e) => onSearch(e.currentTarget.value)} />
          <SelectInput
            aria-label={t('Subject')}
            placeholder={t('All subjects')}
            options={(courses() ?? []).map((c) => ({ value: c.id, label: c.code }))}
            value={filters.courseId ?? ''}
            onChange={(v) => setFilters({ courseId: v || undefined, page: 1 })}
          />
          <SelectInput aria-label={t('Type')} placeholder={t('All types')} options={examTypeOptions} value={filters.type ?? ''}
            onChange={(v) => setFilters({ type: (v || undefined) as ExamType | undefined, page: 1 })} />
          <SelectInput aria-label={t('Status')} placeholder={t('Active (not archived)')} options={examStatusOptions} value={filters.status ?? ''}
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
            <EmptyState icon="exam" title={t('No exams found')} description={t('Create an exam and add questions from your question bank.')}
              action={<Button size="sm" icon="plus" onClick={() => navigate('/exams/new')}>{t('New exam')}</Button>} />
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
                      get header() { return t('Exam') },
                      cell: (e) => (
                        <div>
                          <p class="font-medium text-slate-900">{e.title}</p>
                          <p class="text-xs text-slate-500">{e.courseCode} · {e.courseName}</p>
                        </div>
                      ),
                    },
                    { get header() { return t('Type') }, cell: (e) => t(e.type) },
                    { get header() { return t('Questions') }, cell: (e) => <span class="tabular-nums">{e.questionCount}</span> },
                    { get header() { return t('Total') }, cell: (e) => <span class="tabular-nums">{formatPoints(e.totalPoints)}</span> },
                    { get header() { return t('Duration') }, cell: (e) => formatDuration(e.durationMinutes) },
                    { get header() { return t('Date') }, cell: (e) => <span class="whitespace-nowrap">{formatDate(e.examDate)}</span> },
                    { get header() { return t('Status') }, cell: (e) => <ExamStatusBadge value={e.status} /> },
                    {
                      header: '',
                      class: 'text-end',
                      cell: (e) => (
                        <div class="flex justify-end gap-0.5" onClick={(ev) => ev.stopPropagation()}>
                          <IconButton icon="eye" label={t('Preview')} onClick={() => navigate(`/exams/${e.id}/preview`)} />
                          <IconButton icon="edit" label={t('Open builder')} onClick={() => navigate(`/exams/${e.id}/edit`)} />
                          <IconButton icon="trash" label={t('Delete')} tone="danger" disabled={e.status === 'Archived'} onClick={() => remove(e)} />
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
