import { A } from '@solidjs/router'
import { createResource, For, Match, Show, Switch } from 'solid-js'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { Button } from '~/components/ui/Button'
import { Card, CardHeader, PageHeader, Skeleton, SkeletonRows, StatCard } from '~/components/ui/Primitives'
import { ExamStatusBadge } from '~/features/exams/examMeta'
import { DifficultyBadge, QuestionTypeBadge, SourceBadge, QuestionStatusBadge } from '~/features/questions/questionMeta'
import { dashboardApi } from '~/services/courses.api'
import { auth } from '~/stores/auth.store'
import { formatDate, formatDuration, truncate } from '~/utils/format'
import { t } from '~/utils/i18n'
import { useNavigate } from '@solidjs/router'

export default function DashboardPage() {
  const navigate = useNavigate()
  const [dashboard, { refetch }] = createResource(dashboardApi.get)

  const firstName = () => auth.state.user?.fullName ?? ''

  return (
    <>
      <PageHeader
        title={`Welcome back, ${firstName()}`}
        description="Here is an overview of your courses, question bank and exams."
        actions={
          <>
            <Button variant="secondary" icon="plus" onClick={() => navigate('/questions/new')}>
              {t('action.createQuestion')}
            </Button>
            <Button variant="secondary" icon="plus" onClick={() => navigate('/exams/new')}>
              {t('action.createExam')}
            </Button>
            <Button variant="ai" icon="sparkles" onClick={() => navigate('/ai/generate')}>
              {t('action.generate')}
            </Button>
          </>
        }
      />

      <Switch>
        <Match when={dashboard.error}>
          <Card>
            <ErrorState error={dashboard.error} onRetry={refetch} />
          </Card>
        </Match>
        <Match when={dashboard.loading && !dashboard()}>
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <For each={[1, 2, 3, 4]}>{() => <Card class="p-5"><Skeleton class="h-4 w-24" /><Skeleton class="mt-3 h-8 w-16" /></Card>}</For>
          </div>
          <Card class="mt-6"><SkeletonRows /></Card>
        </Match>
        <Match when={dashboard()}>
          {(data) => (
            <>
              <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                <StatCard label="My Courses" value={data().courseCount} />
                <StatCard
                  label="Question Bank"
                  value={data().questionCount}
                  hint={data().aiGeneratedPendingReviewCount ? `${data().aiGeneratedPendingReviewCount} AI drafts awaiting review` : undefined}
                />
                <StatCard label="Draft Exams" value={data().draftExamCount} accent="text-amber-600" />
                <StatCard label="Published Exams" value={data().publishedExamCount} accent="text-emerald-600" />
              </div>

              <div class="mt-6 grid gap-6 lg:grid-cols-3">
                <Card class="lg:col-span-2">
                  <CardHeader title="Recent Exams" actions={<A href="/exams" class="text-sm font-medium text-brand-600 hover:underline">View all</A>} />
                  <Show when={data().recentExams.length} fallback={
                    <EmptyState icon="exam" title="No exams yet" description="Create your first exam from your question bank."
                      action={<Button size="sm" icon="plus" onClick={() => navigate('/exams/new')}>Create exam</Button>} />
                  }>
                    <ul class="divide-y divide-slate-100">
                      <For each={data().recentExams}>
                        {(exam) => (
                          <li>
                            <A href={`/exams/${exam.id}/edit`} class="flex items-center gap-4 px-5 py-3 hover:bg-slate-50">
                              <div class="min-w-0 flex-1">
                                <p class="truncate text-sm font-medium text-slate-900">{exam.title}</p>
                                <p class="text-xs text-slate-500">
                                  {exam.courseCode} · {exam.questionCount} questions · {exam.totalPoints} pts · {formatDuration(exam.durationMinutes)}
                                </p>
                              </div>
                              <span class="hidden text-xs text-slate-500 sm:inline">{formatDate(exam.examDate)}</span>
                              <ExamStatusBadge value={exam.status} />
                            </A>
                          </li>
                        )}
                      </For>
                    </ul>
                  </Show>
                </Card>

                <Card>
                  <CardHeader title="My Courses" actions={<A href="/courses" class="text-sm font-medium text-brand-600 hover:underline">View all</A>} />
                  <Show when={data().courses.length} fallback={<EmptyState icon="book" title="No courses assigned" description="Ask an administrator to assign you to a course, or create one." />}>
                    <ul class="divide-y divide-slate-100">
                      <For each={data().courses}>
                        {(course) => (
                          <li>
                            <A href={`/courses/${course.id}`} class="block px-5 py-3 hover:bg-slate-50">
                              <p class="text-sm font-medium text-slate-900">
                                <span class="text-brand-700">{course.code}</span> · {course.name}
                              </p>
                              <p class="text-xs text-slate-500">
                                {course.departmentName} · {course.questionCount} questions · {course.examCount} exams
                              </p>
                            </A>
                          </li>
                        )}
                      </For>
                    </ul>
                  </Show>
                </Card>
              </div>

              <Card class="mt-6">
                <CardHeader title="Recent Questions" actions={<A href="/questions" class="text-sm font-medium text-brand-600 hover:underline">Open question bank</A>} />
                <Show when={data().recentQuestions.length} fallback={<EmptyState icon="questions" title="Your question bank is empty" />}>
                  <ul class="divide-y divide-slate-100">
                    <For each={data().recentQuestions}>
                      {(q) => (
                        <li>
                          <A href={`/questions/${q.id}/edit`} class="flex flex-wrap items-center gap-2 px-5 py-3 hover:bg-slate-50">
                            <span class="min-w-0 flex-1 text-sm text-slate-800">{truncate(q.text, 110)}</span>
                            <span class="text-xs font-medium text-slate-500">{q.courseCode}</span>
                            <QuestionTypeBadge value={q.type} />
                            <DifficultyBadge value={q.difficulty} />
                            <QuestionStatusBadge value={q.status} />
                            <SourceBadge value={q.source} />
                          </A>
                        </li>
                      )}
                    </For>
                  </ul>
                </Show>
              </Card>
            </>
          )}
        </Match>
      </Switch>
    </>
  )
}
