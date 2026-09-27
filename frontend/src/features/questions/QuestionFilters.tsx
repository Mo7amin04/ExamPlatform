import { createResource, createSignal } from 'solid-js'
import { SelectInput, TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { coursesApi } from '~/services/courses.api'
import type { BloomLevel, Difficulty, QuestionFilters, QuestionStatus, QuestionType } from '~/types/models'
import { bloomOptions, difficultyOptions, questionTypeOptions, statusOptions } from './questionMeta'

/** Filter bar for the question bank. Search is debounced; other filters apply immediately. */
export function QuestionFilterBar(props: {
  filters: QuestionFilters
  onChange: (patch: Partial<QuestionFilters>) => void
  lockCourse?: boolean
}) {
  const [courses] = createResource(() => coursesApi.list())
  const [topics] = createResource(() => props.filters.courseId || null, (id) => coursesApi.topics(id))
  const [search, setSearch] = createSignal(props.filters.search ?? '')

  let timer: ReturnType<typeof setTimeout> | undefined
  const onSearch = (value: string) => {
    setSearch(value)
    clearTimeout(timer)
    timer = setTimeout(() => props.onChange({ search: value || undefined }), 300)
  }

  const hasFilters = () =>
    !!(props.filters.topicId || props.filters.type || props.filters.difficulty || props.filters.bloomLevel || props.filters.status || props.filters.search ||
      (!props.lockCourse && props.filters.courseId))

  const reset = () => {
    setSearch('')
    props.onChange({
      search: undefined,
      topicId: undefined,
      type: undefined,
      difficulty: undefined,
      bloomLevel: undefined,
      status: undefined,
      ...(props.lockCourse ? {} : { courseId: undefined }),
    })
  }

  return (
    <div class="flex flex-wrap items-start gap-3 border-b border-slate-100 p-4">
      <TextInput
        class="w-full min-w-[14rem] flex-1 sm:w-auto"
        placeholder="Search text or tags…"
        aria-label="Search questions"
        value={search()}
        onInput={(e) => onSearch(e.currentTarget.value)}
      />
      <SelectInput
        class="w-full sm:w-36"
        aria-label="Course"
        placeholder="All courses"
        disabled={props.lockCourse}
        options={(courses() ?? []).map((c) => ({ value: c.id, label: c.code }))}
        value={props.filters.courseId ?? ''}
        onChange={(v) => props.onChange({ courseId: v || undefined, topicId: undefined })}
      />
      <SelectInput
        class="w-full sm:w-44"
        aria-label="Topic"
        placeholder="All topics"
        disabled={!props.filters.courseId}
        options={(topics() ?? []).map((t) => ({ value: t.id, label: t.name }))}
        value={props.filters.topicId ?? ''}
        onChange={(v) => props.onChange({ topicId: v || undefined })}
      />
      <SelectInput class="w-full sm:w-44" aria-label="Type" placeholder="All types" options={questionTypeOptions} value={props.filters.type ?? ''}
        onChange={(v) => props.onChange({ type: (v || undefined) as QuestionType | undefined })} />
      <SelectInput class="w-full sm:w-36" aria-label="Difficulty" placeholder="Any difficulty" options={difficultyOptions} value={props.filters.difficulty ?? ''}
        onChange={(v) => props.onChange({ difficulty: (v || undefined) as Difficulty | undefined })} />
      <SelectInput class="w-full sm:w-40" aria-label="Bloom level" placeholder="Any Bloom level" options={bloomOptions} value={props.filters.bloomLevel ?? ''}
        onChange={(v) => props.onChange({ bloomLevel: (v || undefined) as BloomLevel | undefined })} />
      <div class="flex w-full gap-2 sm:w-auto">
        <SelectInput class="flex-1 sm:w-32" aria-label="Status" placeholder="Active" options={statusOptions} value={props.filters.status ?? ''}
          onChange={(v) => props.onChange({ status: (v || undefined) as QuestionStatus | undefined })} />
        <Button variant="ghost" onClick={reset} disabled={!hasFilters()} title="Clear filters" aria-label="Clear filters" icon="x" class="px-2" />
      </div>
    </div>
  )
}
