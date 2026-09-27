import { A, useNavigate, useSearchParams } from '@solidjs/router'
import { createSignal, Show } from 'solid-js'
import { createStore } from 'solid-js/store'
import { InlineAlert } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { Card, CardHeader, PageHeader } from '~/components/ui/Primitives'
import { ExamSettingsFields, validateExam } from '~/features/exams/ExamSettingsForm'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { examsApi } from '~/services/exams.api'
import type { ExamInput } from '~/types/models'

export default function ExamCreatePage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [exam, setExam] = createStore<ExamInput>({
    courseId: typeof params.courseId === 'string' ? params.courseId : '',
    title: '',
    description: '',
    instructions: 'Answer all questions. Write your answers clearly.',
    type: 'Quiz',
    durationMinutes: 60,
    examDate: null,
  })
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [formError, setFormError] = createSignal<string | null>(null)
  const [saving, setSaving] = createSignal(false)

  const submit = async (e: SubmitEvent) => {
    e.preventDefault()
    const clientErrors = validateExam(exam)
    setErrors(clientErrors)
    setFormError(null)
    if (Object.keys(clientErrors).length) return
    setSaving(true)
    try {
      const created = await examsApi.create({ ...exam })
      toast.success('Exam created', 'Now add questions to your exam.')
      navigate(`/exams/${created.id}/edit`, { replace: true })
    } catch (err) {
      setErrors(getFieldErrors(err))
      setFormError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader breadcrumb={<A href="/exams" class="hover:text-slate-700">Exams</A>} title="New exam" description="Start with the exam settings; you will add questions next." />
      <form class="max-w-2xl" onSubmit={submit} novalidate>
        <Card>
          <CardHeader title="Exam settings" />
          <div class="space-y-4 p-5">
            <Show when={formError()}>
              <InlineAlert tone="error">{formError()!}</InlineAlert>
            </Show>
            <ExamSettingsFields value={exam} onChange={(k, v) => setExam(k, v as never)} errors={errors()} />
          </div>
        </Card>
        <div class="mt-4 flex gap-2">
          <Button type="submit" loading={saving()}>Create and add questions</Button>
          <Button variant="secondary" onClick={() => navigate('/exams')}>Cancel</Button>
        </div>
      </form>
    </>
  )
}
