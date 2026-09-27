import { Navigate, Route } from '@solidjs/router'
import { lazy, type ParentProps } from 'solid-js'
import { RequireAuth } from '~/features/auth/guards'
import { AuthLayout } from '~/layouts/AuthLayout'
import { DashboardLayout } from '~/layouts/DashboardLayout'
import { NotFoundPage } from '~/pages/errors/ErrorPages'

const LoginPage = lazy(() => import('~/pages/auth/LoginPage'))
const DashboardPage = lazy(() => import('~/pages/dashboard/DashboardPage'))
const DepartmentsPage = lazy(() => import('~/pages/departments/DepartmentsPage'))
const CoursesPage = lazy(() => import('~/pages/courses/CoursesPage'))
const CourseDetailPage = lazy(() => import('~/pages/courses/CourseDetailPage'))
const QuestionBankPage = lazy(() => import('~/pages/questions/QuestionBankPage'))
const QuestionCreatePage = lazy(() => import('~/pages/questions/QuestionCreatePage'))
const QuestionEditPage = lazy(() => import('~/pages/questions/QuestionEditPage'))
const ExamsPage = lazy(() => import('~/pages/exams/ExamsPage'))
const ExamCreatePage = lazy(() => import('~/pages/exams/ExamCreatePage'))
const ExamBuilderPage = lazy(() => import('~/pages/exams/ExamBuilderPage'))
const ExamPreviewPage = lazy(() => import('~/pages/exams/ExamPreviewPage'))
const AnswerKeyPage = lazy(() => import('~/pages/exams/AnswerKeyPage'))
const AIGeneratePage = lazy(() => import('~/pages/ai/AIGeneratePage'))
const SettingsPage = lazy(() => import('~/pages/settings/SettingsPage'))

const Protected = (props: ParentProps) => (
  <RequireAuth>
    <DashboardLayout>{props.children}</DashboardLayout>
  </RequireAuth>
)

const Public = (props: ParentProps) => <AuthLayout>{props.children}</AuthLayout>

/** Print-oriented pages render without the application chrome. */
const ProtectedBare = (props: ParentProps) => <RequireAuth>{props.children}</RequireAuth>

/** Route table. Declared as a component so it is created inside the router's reactive root. */
export function AppRoutes() {
  return (
    <>
      <Route path="/login" component={Public}>
        <Route path="/" component={LoginPage} />
      </Route>

      <Route path="/exams/:id/preview" component={ProtectedBare}>
        <Route path="/" component={ExamPreviewPage} />
      </Route>
      <Route path="/exams/:id/answer-key" component={ProtectedBare}>
        <Route path="/" component={AnswerKeyPage} />
      </Route>

      <Route path="/" component={Protected}>
        <Route path="/" component={() => <Navigate href="/dashboard" />} />
        <Route path="/dashboard" component={DashboardPage} />
        <Route path="/departments" component={DepartmentsPage} />
        <Route path="/courses" component={CoursesPage} />
        <Route path="/courses/:id" component={CourseDetailPage} />
        <Route path="/courses/:id/questions" component={QuestionBankPage} />
        <Route path="/questions" component={QuestionBankPage} />
        <Route path="/questions/new" component={QuestionCreatePage} />
        <Route path="/questions/:id/edit" component={QuestionEditPage} />
        <Route path="/exams" component={ExamsPage} />
        <Route path="/exams/new" component={ExamCreatePage} />
        <Route path="/exams/:id/edit" component={ExamBuilderPage} />
        <Route path="/ai/generate" component={AIGeneratePage} />
        <Route path="/settings" component={SettingsPage} />
        <Route path="*404" component={NotFoundPage} />
      </Route>
    </>
  )
}
