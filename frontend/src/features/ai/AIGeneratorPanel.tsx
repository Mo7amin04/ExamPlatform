import { t } from '~/utils/i18n'
import { useNavigate } from '@solidjs/router'
import { createResource, createSignal, For, Match, Show, Switch } from 'solid-js'
import { createStore, produce } from 'solid-js/store'
import { EmptyState, InlineAlert } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { SelectInput, TextArea, TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Modal, useConfirm } from '~/components/ui/Dialog'
import { Card, CardHeader, Spinner } from '~/components/ui/Primitives'
import { QuestionContentEditor, validateContent } from '~/features/questions/QuestionContentEditor'
import { bloomOptions, difficultyOptions, questionTypeOptions } from '~/features/questions/questionMeta'
import { aiApi } from '~/services/ai.api'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import { examsApi } from '~/services/exams.api'
import type { GenerateQuestionsRequest, GeneratedQuestion, MaterialText, QuestionContent } from '~/types/models'
import { GeneratedQuestionCard, type ReviewItem } from './GeneratedQuestionCard'
import { MaterialUpload } from './MaterialUpload'
import { deepClone } from '~/utils/clone'

let keySeed = 0
const toItem = (question: GeneratedQuestion): ReviewItem => ({ key: `g${++keySeed}`, question, selected: question.isValid, edited: false })

/**
 * Generate → preview → teacher review (edit / discard) → accept.
 * Nothing reaches the question bank until "Accept selected" is confirmed.
 */
export function AIGeneratorPanel(props: { initialCourseId?: string; examId?: string }) {
  const navigate = useNavigate()
  const confirm = useConfirm()
  const [request, setRequest] = createStore<GenerateQuestionsRequest>({
    courseId: props.initialCourseId ?? '',
    topicId: null,
    numberOfQuestions: 5,
    questionType: 'MultipleChoice',
    difficulty: 'Medium',
    bloomLevel: 'Understand',
    additionalInstructions: '',
  })
  const [material, setMaterial] = createSignal<MaterialText | null>(null)
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [generating, setGenerating] = createSignal(false)
  const [generationError, setGenerationError] = createSignal<string | null>(null)
  const [items, setItems] = createStore<ReviewItem[]>([])
  const [meta, setMeta] = createSignal<{ generationId: string; provider: string; model: string | null } | null>(null)
  const [accepting, setAccepting] = createSignal(false)
  const [lastAccepted, setLastAccepted] = createSignal<number | null>(null)

  const [courses] = createResource(() => coursesApi.list())
  const [topics] = createResource(() => request.courseId || null, (id) => coursesApi.topics(id))

  const selectedCount = () => items.filter((i) => i.selected).length

  const generate = async () => {
    const clientErrors: Record<string, string> = {}
    if (!request.courseId) clientErrors.courseId = t('Select a subject.')
    if (!(request.numberOfQuestions >= 1 && request.numberOfQuestions <= 20)) clientErrors.numberOfQuestions = t('Choose between 1 and 20 questions.')
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length) return

    setGenerating(true)
    setGenerationError(null)
    setLastAccepted(null)
    try {
      const result = await aiApi.generate({
        ...request,
        topicId: request.topicId || null,
        additionalInstructions: request.additionalInstructions || undefined,
        sourceMaterial: material()?.text,
      })
      setItems(result.questions.map(toItem))
      setMeta({ generationId: result.generationId, provider: result.provider, model: result.model })
      if (!result.questions.length) setGenerationError(t('The AI did not return any questions. Try adjusting your instructions.'))
    } catch (err) {
      setErrors(getFieldErrors(err))
      setGenerationError(getErrorMessage(err))
    } finally {
      setGenerating(false)
    }
  }

  const regenerate = () => {
    if (!items.length) return void generate()
    confirm.ask({
      title: t('Regenerate questions?'),
      description: t('The current proposals (including your edits) will be replaced by a new set.'),
      confirmLabel: t('Regenerate'),
      tone: 'primary',
      onConfirm: generate,
    })
  }

  const discardAll = () =>
    confirm.ask({
      title: t('Discard all generated questions?'),
      description: t('None of these questions have been saved. They will be removed from this page.'),
      confirmLabel: t('Discard all'),
      onConfirm: () => {
        setItems([])
        setMeta(null)
      },
    })

  const accept = async () => {
    const chosen = items.filter((i) => i.selected)
    if (!chosen.length) return
    setAccepting(true)
    try {
      const { questionIds } = await aiApi.accept(
        chosen.map((i) => ({
          courseId: request.courseId,
          topicId: request.topicId || null,
          text: i.question.text,
          type: i.question.type,
          difficulty: i.question.difficulty,
          bloomLevel: i.question.bloomLevel,
          points: i.question.points,
          explanation: i.question.explanation,
          expectedAnswer: i.question.expectedAnswer,
          options: i.question.options,
          status: 'Draft',
          tags: [],
        })),
        meta()?.generationId,
      )
      setItems((list) => list.filter((i) => !i.selected))

      if (props.examId) {
        await examsApi.addQuestions(props.examId, questionIds)
        toast.success(t('{count} question(s) saved and added to the exam', { count: questionIds.length }))
        navigate(`/exams/${props.examId}/edit`)
        return
      }
      setLastAccepted(questionIds.length)
      toast.success(t('{count} question(s) added to the question bank', { count: questionIds.length }), t('They are saved as drafts for your review.'))
    } catch (err) {
      toast.error(t('Could not accept questions'), getErrorMessage(err))
    } finally {
      setAccepting(false)
    }
  }

  // ----- Edit dialog -----
  const [editingKey, setEditingKey] = createSignal<string | null>(null)
  const [editDraft, setEditDraft] = createStore<QuestionContent>({
    text: '', type: 'MultipleChoice', difficulty: 'Medium', bloomLevel: 'Understand', points: 1, options: [],
  })
  const [editErrors, setEditErrors] = createSignal<Record<string, string>>({})

  const openEdit = (item: ReviewItem) => {
    setEditDraft(deepClone({ ...item.question, explanation: item.question.explanation ?? '', expectedAnswer: item.question.expectedAnswer ?? '' }))
    setEditErrors({})
    setEditingKey(item.key)
  }

  const saveEdit = () => {
    const errs = validateContent(editDraft)
    setEditErrors(errs)
    if (Object.keys(errs).length) return
    const key = editingKey()
    setItems(
      (i) => i.key === key,
      produce((item) => {
        item.question = { ...deepClone(editDraft), isValid: true, issues: [] }
        item.edited = true
        item.selected = true
      }),
    )
    setEditingKey(null)
  }

  return (
    <div class="grid gap-6 xl:grid-cols-[24rem_1fr]">
      <Card class="self-start">
        <CardHeader title={t('Generation settings')} description={t('AI proposals are never saved automatically.')} />
        <form
          class="space-y-4 p-5"
          onSubmit={(e) => {
            e.preventDefault()
            regenerate()
          }}
        >
          <SelectInput
            label={t('Subject')}
            required
            placeholder={courses.loading ? t('Loading…') : t('Select a subject')}
            options={(courses() ?? []).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }))}
            value={request.courseId}
            onChange={(v) => setRequest({ courseId: v, topicId: null })}
            error={errors().courseId}
            disabled={!!props.examId}
          />
          <SelectInput
            label={t('Topic')}
            placeholder={request.courseId ? t('Whole subject') : t('Select a subject first')}
            options={(topics() ?? []).map((t) => ({ value: t.id, label: t.name }))}
            value={request.topicId ?? ''}
            onChange={(v) => setRequest('topicId', v || null)}
            disabled={!request.courseId}
            error={errors().topicId}
          />
          <div class="grid grid-cols-2 gap-3">
            <TextInput
              label={t('Number of questions')}
              type="number"
              min={1}
              max={20}
              value={request.numberOfQuestions}
              onInput={(e) => setRequest('numberOfQuestions', Number(e.currentTarget.value))}
              error={errors().numberOfQuestions}
            />
            <SelectInput label={t('Question type')} options={questionTypeOptions} value={request.questionType}
              onChange={(v) => setRequest('questionType', v as GenerateQuestionsRequest['questionType'])} />
            <SelectInput label={t('Difficulty')} options={difficultyOptions} value={request.difficulty}
              onChange={(v) => setRequest('difficulty', v as GenerateQuestionsRequest['difficulty'])} />
            <SelectInput label={t('Bloom level')} options={bloomOptions} value={request.bloomLevel}
              onChange={(v) => setRequest('bloomLevel', v as GenerateQuestionsRequest['bloomLevel'])} />
          </div>
          <TextArea
            label={t('Additional instructions')}
            rows={3}
            placeholder={t('e.g. Focus on real-world scenarios; avoid \'all of the above\'.')}
            value={request.additionalInstructions ?? ''}
            onInput={(e) => setRequest('additionalInstructions', e.currentTarget.value)}
            error={errors().additionalInstructions}
          />
          <MaterialUpload value={material()} onChange={setMaterial} />
          <Button type="submit" variant="ai" icon="sparkles" class="w-full" loading={generating()}>
            {items.length ? t('Regenerate') : t('Generate questions')}
          </Button>
        </form>
      </Card>

      <div class="min-w-0">
        <Switch>
          <Match when={generating()}>
            <Card>
              <div class="flex flex-col items-center justify-center gap-3 py-20 text-sm text-slate-600">
                <Spinner class="size-6 text-violet-600" />
                {t('Generating questions… this can take up to a minute.')}
              </div>
            </Card>
          </Match>
          <Match when={generationError() && !items.length}>
            <Card class="p-5">
              <InlineAlert tone="error" title={t('Generation failed')}>{generationError()!}</InlineAlert>
              <Button class="mt-4" variant="secondary" icon="refresh" onClick={generate}>{t('Try again')}</Button>
            </Card>
          </Match>
          <Match when={!items.length}>
            <Card>
              <Show
                when={lastAccepted()}
                fallback={<EmptyState icon="sparkles" title={t('No generated questions yet')} description={t('Choose a subject and settings, then generate. You will review every question before anything is saved.')} />}
              >
                <EmptyState
                  icon="check"
                  title={t('{count} question(s) added to the question bank', { count: lastAccepted() ?? 0 })}
                  description={t('Accepted questions are saved as drafts. Approve them in the question bank after review.')}
                  action={<Button size="sm" onClick={() => navigate(`/courses/${request.courseId}/questions`)}>{t('Open question bank')}</Button>}
                />
              </Show>
            </Card>
          </Match>
          <Match when={items.length}>
            <Card>
              <CardHeader
                title={t('Generated questions ({count})', { count: items.length })}
                description={meta() ? t('Provider: {provider} — review carefully; AI output can be wrong.', { provider: [meta()!.provider, meta()!.model].filter(Boolean).join(' · ') }) : undefined}
                actions={
                  <>
                    <Button size="sm" variant="ghost" onClick={() => setItems({}, 'selected', false)}>{t('Clear selection')}</Button>
                    <Button size="sm" variant="secondary" icon="trash" onClick={discardAll}>{t('Discard all')}</Button>
                    <Button size="sm" variant="secondary" icon="refresh" onClick={regenerate}>{t('Regenerate')}</Button>
                    <Button size="sm" icon="check" loading={accepting()} disabled={!selectedCount()} onClick={accept}>
                      {t('Accept selected ({count})', { count: selectedCount() })}
                    </Button>
                  </>
                }
              />
              <Show when={props.examId}>
                <div class="px-5 pt-4"><InlineAlert tone="info">{t('Accepted questions will be saved to the bank and added to your exam.')}</InlineAlert></div>
              </Show>
              <ul class="space-y-3 p-5">
                <For each={items}>
                  {(item, index) => (
                    <GeneratedQuestionCard
                      item={item}
                      index={index()}
                      onToggle={(selected) => setItems((i) => i.key === item.key, 'selected', selected)}
                      onEdit={() => openEdit(item)}
                      onDiscard={() => setItems((list) => list.filter((i) => i.key !== item.key))}
                    />
                  )}
                </For>
              </ul>
            </Card>
          </Match>
        </Switch>
      </div>

      <Modal
        open={!!editingKey()}
        onOpenChange={(open) => !open && setEditingKey(null)}
        size="lg"
        title={t('Edit generated question')}
        description={t('Your edits are kept locally until you accept the question.')}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditingKey(null)}>{t('Cancel')}</Button>
            <Button icon="check" onClick={saveEdit}>{t('Save edits')}</Button>
          </>
        }
      >
        <QuestionContentEditor value={editDraft} update={(k, v) => setEditDraft(k, v as never)} errors={editErrors()} />
      </Modal>
      <confirm.Dialog />
    </div>
  )
}
