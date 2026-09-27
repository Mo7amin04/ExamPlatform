import { createEffect, createResource, createSignal, For, Match, Show, Switch } from 'solid-js'
import { createStore } from 'solid-js/store'
import { EmptyState, ErrorState, LoadingState } from '~/components/feedback/States'
import { Pagination } from '~/components/tables/DataTable'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { QuestionFilterBar } from '~/features/questions/QuestionFilters'
import { DifficultyBadge, QuestionStatusBadge, QuestionTypeBadge, SourceBadge } from '~/features/questions/questionMeta'
import { questionsApi } from '~/services/questions.api'
import type { QuestionFilters } from '~/types/models'
import { formatPoints, truncate } from '~/utils/format'

/** Selects questions from the exam's course that are not yet in the exam (server-side filtered & paged). */
export function QuestionPickerDialog(props: {
  open: boolean
  examId: string
  courseId: string
  onOpenChange: (open: boolean) => void
  onAdd: (questionIds: string[]) => Promise<void>
}) {
  const [filters, setFilters] = createStore<QuestionFilters>({ page: 1, pageSize: 10 })
  const [selected, setSelected] = createSignal<string[]>([])
  const [adding, setAdding] = createSignal(false)

  createEffect(() => {
    if (props.open) {
      setFilters({ page: 1, pageSize: 10, courseId: props.courseId, excludeExamId: props.examId, topicId: undefined, search: undefined })
      setSelected([])
    }
  })

  const [result, { refetch }] = createResource(() => (props.open ? { ...filters } : null), questionsApi.list)

  const toggle = (id: string) => setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]))

  const add = async () => {
    setAdding(true)
    try {
      await props.onAdd(selected())
      props.onOpenChange(false)
    } finally {
      setAdding(false)
    }
  }

  return (
    <Modal
      open={props.open}
      onOpenChange={props.onOpenChange}
      size="xl"
      title="Add questions from the question bank"
      description="Only non-archived questions from this exam's course that are not already in the exam are listed."
      footer={
        <>
          <span class="me-auto self-center text-sm text-slate-600">{selected().length} selected</span>
          <Button variant="secondary" onClick={() => props.onOpenChange(false)}>Cancel</Button>
          <Button icon="plus" disabled={!selected().length} loading={adding()} onClick={add}>
            Add {selected().length || ''} question{selected().length === 1 ? '' : 's'}
          </Button>
        </>
      }
    >
      <div class="-mx-5 -mt-4">
        <QuestionFilterBar filters={filters} onChange={(patch) => setFilters({ ...patch, page: 1 })} lockCourse />
      </div>
      <Switch>
        <Match when={result.error}>
          <ErrorState error={result.error} onRetry={refetch} />
        </Match>
        <Match when={!result.latest}>
          <LoadingState />
        </Match>
        <Match when={result.latest?.items.length === 0}>
          <EmptyState icon="questions" title="No available questions" description="All matching questions are already in this exam, or the course has none yet." />
        </Match>
        <Match when={result.latest}>
          {(data) => (
            <>
              <ul class="divide-y divide-slate-100">
                <For each={data().items}>
                  {(q) => (
                    <li>
                      <label class="flex cursor-pointer items-start gap-3 py-3 hover:bg-slate-50">
                        <input type="checkbox" class="mt-1 size-4 accent-brand-600" checked={selected().includes(q.id)} onChange={() => toggle(q.id)} />
                        <div class="min-w-0 flex-1">
                          <p class="text-sm text-slate-900">{truncate(q.text, 180)}</p>
                          <div class="mt-1 flex flex-wrap items-center gap-1.5">
                            <QuestionTypeBadge value={q.type} />
                            <DifficultyBadge value={q.difficulty} />
                            <QuestionStatusBadge value={q.status} />
                            <SourceBadge value={q.source} />
                            <Show when={q.topicName}><span class="text-xs text-slate-500">{q.topicName}</span></Show>
                          </div>
                        </div>
                        <span class="whitespace-nowrap text-sm tabular-nums text-slate-600">{formatPoints(q.points)}</span>
                      </label>
                    </li>
                  )}
                </For>
              </ul>
              <div class="-mx-5">
                <Pagination pagination={data().pagination} onPageChange={(page) => setFilters('page', page)} />
              </div>
            </>
          )}
        </Match>
      </Switch>
    </Modal>
  )
}
