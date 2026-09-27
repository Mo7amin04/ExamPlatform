import { A, useSearchParams } from '@solidjs/router'
import { Show } from 'solid-js'
import { PageHeader } from '~/components/ui/Primitives'
import { AIGeneratorPanel } from '~/features/ai/AIGeneratorPanel'

export default function AIGeneratePage() {
  const [params] = useSearchParams()
  const courseId = typeof params.courseId === 'string' ? params.courseId : undefined
  const examId = typeof params.examId === 'string' ? params.examId : undefined

  return (
    <>
      <PageHeader
        breadcrumb={<Show when={examId}><A href={`/exams/${examId}/edit`} class="hover:text-slate-700">Back to exam builder</A></Show>}
        title="✨ AI Question Generator"
        description="Draft questions with AI, then review, edit and accept the ones you want. Accepted questions are saved as drafts."
      />
      <AIGeneratorPanel initialCourseId={courseId} examId={examId} />
    </>
  )
}
