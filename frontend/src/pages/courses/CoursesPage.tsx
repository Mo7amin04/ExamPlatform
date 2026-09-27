import { A, useNavigate } from '@solidjs/router'
import { createResource, createSignal, For, Match, Switch } from 'solid-js'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Card, PageHeader, Skeleton } from '~/components/ui/Primitives'
import { CourseFormDialog } from '~/features/courses/CourseFormDialog'
import { coursesApi } from '~/services/courses.api'

export default function CoursesPage() {
  const navigate = useNavigate()
  const [search, setSearch] = createSignal('')
  const [debounced, setDebounced] = createSignal('')
  const [courses, { refetch }] = createResource(() => ({ search: debounced() }), (q) => coursesApi.list(q))
  const [dialogOpen, setDialogOpen] = createSignal(false)

  let timer: ReturnType<typeof setTimeout> | undefined
  const onSearch = (value: string) => {
    setSearch(value)
    clearTimeout(timer)
    timer = setTimeout(() => setDebounced(value), 300)
  }

  return (
    <>
      <PageHeader
        title="Courses"
        description="Courses you teach. Administrators see every course."
        actions={<Button icon="plus" onClick={() => setDialogOpen(true)}>New course</Button>}
      />
      <div class="mb-4 max-w-sm">
        <TextInput placeholder="Search by code or name…" aria-label="Search courses" value={search()} onInput={(e) => onSearch(e.currentTarget.value)} />
      </div>

      <Switch>
        <Match when={courses.error}>
          <Card><ErrorState error={courses.error} onRetry={refetch} /></Card>
        </Match>
        <Match when={courses.loading && !courses()}>
          <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <For each={[1, 2, 3]}>{() => <Card class="p-5"><Skeleton class="h-5 w-24" /><Skeleton class="mt-3 h-4 w-3/4" /><Skeleton class="mt-6 h-4 w-1/2" /></Card>}</For>
          </div>
        </Match>
        <Match when={courses()?.length === 0}>
          <Card>
            <EmptyState icon="book" title="No courses found" description="Create a course or ask an administrator to assign you to one."
              action={<Button size="sm" icon="plus" onClick={() => setDialogOpen(true)}>New course</Button>} />
          </Card>
        </Match>
        <Match when={courses()}>
          {(list) => (
            <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <For each={list()}>
                {(course) => (
                  <A href={`/courses/${course.id}`} class="group block">
                    <Card class="h-full p-5 transition-shadow group-hover:shadow-md">
                      <div class="flex items-start justify-between gap-2">
                        <span class="rounded-md bg-brand-50 px-2 py-0.5 text-xs font-semibold text-brand-700">{course.code}</span>
                        <span class="text-xs text-slate-500">{course.departmentName}</span>
                      </div>
                      <h3 class="mt-3 font-semibold text-slate-900 group-hover:text-brand-700">{course.name}</h3>
                      <p class="mt-1 line-clamp-2 text-sm text-slate-500">{course.description ?? 'No description'}</p>
                      <div class="mt-4 flex gap-4 text-xs text-slate-600">
                        <span><strong class="text-slate-900">{course.topicCount}</strong> topics</span>
                        <span><strong class="text-slate-900">{course.questionCount}</strong> questions</span>
                        <span><strong class="text-slate-900">{course.examCount}</strong> exams</span>
                      </div>
                      <p class="mt-3 truncate text-xs text-slate-500">
                        {course.teachers.map((t) => t.fullName).join(', ') || 'No teachers assigned'}
                      </p>
                    </Card>
                  </A>
                )}
              </For>
            </div>
          )}
        </Match>
      </Switch>

      <CourseFormDialog open={dialogOpen()} course={null} onOpenChange={setDialogOpen} onSaved={(c) => navigate(`/courses/${c.id}`)} />
    </>
  )
}
