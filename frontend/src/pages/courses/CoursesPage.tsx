import { t } from '~/utils/i18n'
import { A, useNavigate, useSearchParams } from '@solidjs/router'
import { createResource, createSignal, For, Match, Switch } from 'solid-js'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { SelectInput, TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Icon } from '~/components/ui/Icon'
import { Badge, Card, PageHeader, Skeleton } from '~/components/ui/Primitives'
import { CourseFormDialog } from '~/features/courses/CourseFormDialog'
import { coursesApi, departmentsApi } from '~/services/courses.api'

export default function CoursesPage() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const gradeLevelId = () => {
    const id = params.gradeLevelId ?? params.schoolYearId
    return typeof id === 'string' ? id : undefined
  }
  const [gradeLevels] = createResource(departmentsApi.list)
  const [search, setSearch] = createSignal('')
  const [debounced, setDebounced] = createSignal('')
  const [courses, { refetch }] = createResource(() => ({ search: debounced(), departmentId: gradeLevelId() }), (q) => coursesApi.list(q))
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
        title={t('Subjects')}
        description={t('Subjects you teach. Administrators see every subject.')}
        actions={<Button icon="plus" onClick={() => setDialogOpen(true)}>{t('New subject')}</Button>}
      />
      <div class="mb-4 grid max-w-2xl gap-3 sm:grid-cols-2">
        <TextInput placeholder={t('Search by code or name…')} aria-label={t('Search subjects')} value={search()} onInput={(e) => onSearch(e.currentTarget.value)} />
        <SelectInput
          aria-label={t('Grade level')}
          placeholder={t('All grade levels')}
          options={(gradeLevels.error ? [] : gradeLevels() ?? []).map((level) => ({ value: level.id, label: level.name }))}
          value={gradeLevelId() ?? ''}
          onChange={(id) => setParams({ gradeLevelId: id || undefined, schoolYearId: undefined })}
        />
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
            <EmptyState icon="book" title={t('No subjects found')} description={t('Create a subject or ask an administrator to assign you to one.')}
              action={<Button size="sm" icon="plus" onClick={() => setDialogOpen(true)}>{t('New subject')}</Button>} />
          </Card>
        </Match>
        <Match when={courses()}>
          {(list) => (
            <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <For each={list()}>
                {(course) => (
                  <A href={`/courses/${course.id}`} class="group block">
                    <Card class="panel-link h-full p-5">
                      <div class="flex items-center gap-3">
                        <span class="sticker size-10 bg-brand-100"><Icon name="book" class="size-5" /></span>
                        <div class="min-w-0 flex-1">
                          <Badge tone="blue">{course.code}</Badge>
                          <p class="mt-1 truncate text-xs text-slate-500">{course.departmentName}</p>
                        </div>
                        <Icon name="chevronRight" class="size-4 shrink-0 text-ink rtl:rotate-180" />
                      </div>
                      <h3 class="mt-3 font-display text-lg font-bold text-ink group-hover:text-brand-700">{course.name}</h3>
                      <p class="mt-1 line-clamp-2 text-sm text-slate-500">{course.description ?? t('No description')}</p>
                      <div class="mt-4 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-600">
                        <span>{t('{count} topics', { count: course.topicCount })}</span>
                        <span>{t('{count} questions', { count: course.questionCount })}</span>
                        <span>{t('{count} exams', { count: course.examCount })}</span>
                      </div>
                      <p class="mt-3 truncate text-xs text-slate-500">
                        {course.teachers.map((teacher) => teacher.fullName).join(', ') || t('No teachers assigned')}
                      </p>
                    </Card>
                  </A>
                )}
              </For>
            </div>
          )}
        </Match>
      </Switch>

      <CourseFormDialog open={dialogOpen()} course={null} defaultGradeLevelId={gradeLevelId()} onOpenChange={setDialogOpen} onSaved={(c) => navigate(`/courses/${c.id}`)} />
    </>
  )
}
