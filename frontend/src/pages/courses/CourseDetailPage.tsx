import { A, useNavigate, useParams } from '@solidjs/router'
import { createResource, createSignal, Match, Show, Switch } from 'solid-js'
import { ErrorState, LoadingState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, PageHeader, StatCard } from '~/components/ui/Primitives'
import { CourseFormDialog } from '~/features/courses/CourseFormDialog'
import { TeacherManager } from '~/features/courses/TeacherManager'
import { TopicManager } from '~/features/courses/TopicManager'
import { getErrorMessage, isStatus } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'

export default function CourseDetailPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [course, { refetch, mutate }] = createResource(() => params.id, coursesApi.get)
  const [editOpen, setEditOpen] = createSignal(false)
  const confirm = useConfirm()

  const remove = () =>
    confirm.ask({
      title: `Delete ${course()?.code}?`,
      description: 'The course and its topics will be deleted. Courses that already contain questions or exams cannot be deleted.',
      confirmLabel: 'Delete course',
      onConfirm: async () => {
        try {
          await coursesApi.remove(params.id)
          toast.success('Course deleted')
          navigate('/courses')
        } catch (err) {
          toast.error('Could not delete course', getErrorMessage(err))
        }
      },
    })

  return (
    <Switch>
      <Match when={course.error && isStatus(course.error, 403)}>
        <ForbiddenPage />
      </Match>
      <Match when={course.error}>
        <Card><ErrorState error={course.error} onRetry={refetch} /></Card>
      </Match>
      <Match when={!course()}>
        <LoadingState />
      </Match>
      <Match when={course()}>
        {(c) => (
          <>
            <PageHeader
              breadcrumb={<A href="/courses" class="hover:text-slate-700">Courses</A>}
              title={<><span class="text-brand-700">{c().code}</span> · {c().name}</>}
              description={`${c().departmentName} · ${c().creditHours} credit hours`}
              actions={
                <>
                  <Button variant="secondary" icon="edit" onClick={() => setEditOpen(true)}>Edit</Button>
                  <Button variant="secondary" icon="trash" onClick={remove}>Delete</Button>
                  <Button icon="questions" onClick={() => navigate(`/courses/${c().id}/questions`)}>Question bank</Button>
                </>
              }
            />
            <Show when={c().description}>
              <p class="-mt-2 mb-6 max-w-3xl text-sm text-slate-600">{c().description}</p>
            </Show>
            <div class="mb-6 grid gap-4 sm:grid-cols-3">
              <StatCard label="Topics" value={c().topics.length} />
              <StatCard label="Questions" value={<A href={`/courses/${c().id}/questions`} class="hover:text-brand-700">{c().questionCount}</A>} />
              <StatCard label="Exams" value={<A href={`/exams?courseId=${c().id}`} class="hover:text-brand-700">{c().examCount}</A>} />
            </div>
            <div class="grid gap-6 lg:grid-cols-3">
              <div class="lg:col-span-2">
                <TopicManager courseId={c().id} topics={c().topics} onChanged={refetch} />
              </div>
              <TeacherManager courseId={c().id} teachers={c().teachers} onChanged={refetch} />
            </div>
            <CourseFormDialog open={editOpen()} course={c()} onOpenChange={setEditOpen} onSaved={(updated) => mutate(updated)} />
            <confirm.Dialog />
          </>
        )}
      </Match>
    </Switch>
  )
}
