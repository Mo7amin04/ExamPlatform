import { t } from '~/utils/i18n'
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
      title: t('Delete {name}?', { name: course()?.code ?? '' }),
      description: t('The subject and its topics will be deleted. Subjects that already contain questions or exams cannot be deleted.'),
      confirmLabel: t('Delete subject'),
      onConfirm: async () => {
        try {
          await coursesApi.remove(params.id)
          toast.success(t('Subject deleted'))
          navigate('/courses')
        } catch (err) {
          toast.error(t('Could not delete subject'), getErrorMessage(err))
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
              breadcrumb={<A href="/courses" class="hover:text-slate-700">{t('Subjects')}</A>}
              title={<><span class="text-brand-700">{c().code}</span> · {c().name}</>}
              description={t('Grade level: {name}', { name: c().departmentName })}
              actions={
                <>
                  <Button variant="secondary" icon="edit" onClick={() => setEditOpen(true)}>{t('Edit')}</Button>
                  <Button variant="secondary" icon="trash" onClick={remove}>{t('Delete')}</Button>
                  <Button icon="questions" onClick={() => navigate(`/courses/${c().id}/questions`)}>{t('Question bank')}</Button>
                </>
              }
            />
            <Show when={c().description}>
              <p class="-mt-2 mb-6 max-w-3xl text-sm text-slate-600">{c().description}</p>
            </Show>
            <div class="mb-6 grid gap-4 sm:grid-cols-3">
              <StatCard label={t('Topics')} value={c().topics.length} icon="book" />
              <StatCard label={t('Questions')} value={<A href={`/courses/${c().id}/questions`} class="hover:text-brand-700">{c().questionCount}</A>} icon="questions" tone="peach" />
              <StatCard label={t('Exams')} value={<A href={`/exams?courseId=${c().id}`} class="hover:text-brand-700">{c().examCount}</A>} icon="exam" tone="butter" />
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
