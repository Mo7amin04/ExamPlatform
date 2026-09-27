import { A, useNavigate, useSearchParams } from '@solidjs/router'
import { toast } from '~/components/feedback/toast'
import { PageHeader } from '~/components/ui/Primitives'
import { QuestionForm, emptyDraft } from '~/features/questions/QuestionForm'
import { questionsApi } from '~/services/questions.api'

export default function QuestionCreatePage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const courseId = typeof params.courseId === 'string' ? params.courseId : ''
  const back = () => navigate(courseId ? `/courses/${courseId}/questions` : '/questions')

  return (
    <>
      <PageHeader
        breadcrumb={<A href="/questions" class="hover:text-slate-700">Question Bank</A>}
        title="New question"
        description="The form adapts to the selected question type."
      />
      <QuestionForm
        initial={emptyDraft(courseId)}
        submitLabel="Create question"
        onCancel={back}
        onSubmit={async (input) => {
          const created = await questionsApi.create(input)
          toast.success('Question created')
          navigate(`/courses/${created.courseId}/questions`)
        }}
      />
    </>
  )
}
