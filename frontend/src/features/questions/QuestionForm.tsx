import { t } from '~/utils/i18n'
import { createEffect, createResource, createSignal, For, on, Show } from 'solid-js'
import { createStore } from 'solid-js/store'
import { InlineAlert } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { SelectInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { Card, CardHeader } from '~/components/ui/Primitives'
import { aiApi } from '~/services/ai.api'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import type { GeneratedQuestion, QuestionContent, QuestionInput, QuestionStatus } from '~/types/models'
import { QuestionContentEditor, QuestionContentView, defaultOptionsFor, validateContent } from './QuestionContentEditor'
import { statusOptions } from './questionMeta'
import { TagInput } from './TagInput'
import { deepClone } from '~/utils/clone'

export type QuestionDraft = QuestionInput

export function emptyDraft(courseId = ''): QuestionDraft {
  return {
    courseId,
    topicId: null,
    text: '',
    type: 'MultipleChoice',
    difficulty: 'Medium',
    bloomLevel: 'Understand',
    points: 1,
    explanation: '',
    expectedAnswer: '',
    status: 'Draft',
    options: defaultOptionsFor('MultipleChoice'),
    tags: [],
  }
}

/** Create/edit form: classification (course, topic, status, tags) + type-aware content editor + AI improve. */
export function QuestionForm(props: {
  initial: QuestionDraft
  submitLabel: string
  onSubmit: (input: QuestionDraft) => Promise<void>
  onCancel: () => void
  lockedReason?: string
}) {
  const [draft, setDraft] = createStore<QuestionDraft>(deepClone(props.initial))
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [formError, setFormError] = createSignal<string | null>(null)
  const [saving, setSaving] = createSignal(false)

  const [courses] = createResource(() => coursesApi.list())
  const [topics] = createResource(() => draft.courseId || null, (id) => coursesApi.topics(id))

  // Reset the topic when the course changes (topics belong to a course).
  createEffect(on(() => draft.courseId, (_, prev) => prev !== undefined && setDraft('topicId', null), { defer: true }))

  const update = <K extends keyof QuestionContent>(key: K, value: QuestionContent[K]) => setDraft(key as keyof QuestionDraft, value as never)

  const submit = async (e: SubmitEvent) => {
    e.preventDefault()
    setFormError(null)
    const clientErrors = validateContent(draft)
    if (!draft.courseId) clientErrors.courseId = t('Select a subject.')
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length) return

    setSaving(true)
    try {
      await props.onSubmit({ ...draft, topicId: draft.topicId || null })
    } catch (err) {
      const fieldErrors = getFieldErrors(err)
      setErrors(fieldErrors)
      setFormError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  // ----- AI improve -----
  const [improveOpen, setImproveOpen] = createSignal(false)
  const [instructions, setInstructions] = createSignal('')
  const [improving, setImproving] = createSignal(false)
  const [improved, setImproved] = createSignal<GeneratedQuestion | null>(null)

  const runImprove = async () => {
    if (!draft.courseId || !draft.text.trim()) {
      toast.info(t('Add a subject and question text first'))
      return
    }
    setImproving(true)
    setImproved(null)
    try {
      const { courseId, topicId, status: _s, tags: _t, ...content } = draft
      setImproved(await aiApi.improve({ courseId, topicId, question: content, instructions: instructions() || undefined }))
    } catch (err) {
      toast.error(t('AI improvement failed'), getErrorMessage(err))
    } finally {
      setImproving(false)
    }
  }

  const applyImproved = () => {
    const q = improved()
    if (!q) return
    setDraft({
      text: q.text,
      type: q.type,
      difficulty: q.difficulty,
      bloomLevel: q.bloomLevel,
      points: q.points,
      explanation: q.explanation ?? '',
      expectedAnswer: q.expectedAnswer ?? '',
      options: q.options.map((o) => ({ ...o })),
    })
    setImproveOpen(false)
    toast.info(t('Improved version applied'), t('Review the changes, then save.'))
  }

  return (
    <form onSubmit={submit} class="grid gap-6 lg:grid-cols-3" novalidate>
      <div class="space-y-6 lg:col-span-2">
        <Show when={props.lockedReason}>
          <InlineAlert tone="warning" title={t('Editing is restricted')}>{props.lockedReason}</InlineAlert>
        </Show>
        <Show when={formError()}>
          <InlineAlert tone="error">{formError()!}</InlineAlert>
        </Show>
        <Card>
          <CardHeader
            title={t('Question')}
            actions={
              <Button size="sm" variant="ai" icon="sparkles" onClick={() => { setImproved(null); setImproveOpen(true) }}>
                {t('Improve with AI')}
              </Button>
            }
          />
          <div class="p-5">
            <QuestionContentEditor value={draft} update={update} errors={errors()} />
          </div>
        </Card>
      </div>

      <div class="space-y-6">
        <Card>
          <CardHeader title={t('Classification')} />
          <div class="space-y-4 p-5">
            <SelectInput
              label={t('Subject')}
              required
              placeholder={courses.loading ? t('Loading…') : t('Select a subject')}
              options={(courses() ?? []).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }))}
              value={draft.courseId}
              onChange={(v) => setDraft('courseId', v)}
              error={errors().courseId}
            />
            <SelectInput
              label={t('Topic')}
              placeholder={draft.courseId ? t('No topic') : t('Select a subject first')}
              options={(topics() ?? []).map((t) => ({ value: t.id, label: t.name }))}
              value={draft.topicId ?? ''}
              onChange={(v) => setDraft('topicId', v || null)}
              disabled={!draft.courseId}
              error={errors().topicId}
            />
            <SelectInput
              label={t('Status')}
              options={statusOptions}
              value={draft.status ?? 'Draft'}
              onChange={(v) => setDraft('status', v as QuestionStatus)}
              hint={t('Only approved questions should be used in final exams.')}
            />
            <TagInput tags={draft.tags} onChange={(tags) => setDraft('tags', tags)} error={errors().tags} />
          </div>
        </Card>
        <div class="flex gap-2">
          <Button type="submit" class="flex-1" loading={saving()} disabled={!!props.lockedReason}>
            {props.submitLabel}
          </Button>
          <Button variant="secondary" onClick={props.onCancel}>{t('Cancel')}</Button>
        </div>
      </div>

      <Modal
        open={improveOpen()}
        onOpenChange={setImproveOpen}
        size="lg"
        title={t('Improve with AI')}
        description={t('The AI proposes a revised version. Nothing changes until you apply it.')}
        footer={
          <>
            <Button variant="secondary" onClick={() => setImproveOpen(false)}>{t('Close')}</Button>
            <Show when={improved()} fallback={<Button variant="ai" icon="sparkles" loading={improving()} onClick={runImprove}>{t('Generate improvement')}</Button>}>
              <Button variant="secondary" icon="refresh" loading={improving()} onClick={runImprove}>{t('Try again')}</Button>
              <Button icon="check" onClick={applyImproved}>{t('Apply to form')}</Button>
            </Show>
          </>
        }
      >
        <div class="space-y-4">
          <label class="block text-sm font-medium text-slate-700" for="improve-instructions">{t('Instructions (optional)')}</label>
          <textarea
            id="improve-instructions"
            rows={2}
            class="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
            placeholder={t('e.g. Make the distractors more plausible, simplify the wording…')}
            value={instructions()}
            onInput={(e) => setInstructions(e.currentTarget.value)}
          />
          <Show when={improved()}>
            {(q) => (
              <div class="rounded-lg border border-violet-200 bg-violet-50/40 p-4">
                <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-violet-700">{t('Proposed version')}</p>
                <QuestionContentView value={q()} showAnswers />
                <Show when={q().issues.length}>
                  <ul class="mt-3 list-inside list-disc text-xs text-amber-700">
                    <For each={q().issues}>{(issue) => <li>{issue}</li>}</For>
                  </ul>
                </Show>
              </div>
            )}
          </Show>
        </div>
      </Modal>
    </form>
  )
}
