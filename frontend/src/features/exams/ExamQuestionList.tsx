import { createSignal, For, Show } from 'solid-js'
import { IconButton } from '~/components/ui/Button'
import { DifficultyBadge, QuestionTypeBadge } from '~/features/questions/questionMeta'
import type { ExamQuestion } from '~/types/models'

/**
 * Ordered exam questions with drag-and-drop reordering (mouse) plus move up/down buttons (keyboard),
 * inline points/section editing and removal.
 */
export function ExamQuestionList(props: {
  questions: ExamQuestion[]
  editable: boolean
  onReorder: (orderedIds: string[]) => void
  onUpdate: (questionId: string, points: number, section: string | null) => void
  onRemove: (question: ExamQuestion) => void
  onOpen: (questionId: string) => void
}) {
  const [dragId, setDragId] = createSignal<string | null>(null)
  const [overId, setOverId] = createSignal<string | null>(null)

  const ids = () => props.questions.map((q) => q.questionId)

  const moveTo = (id: string, targetIndex: number) => {
    const list = ids().filter((x) => x !== id)
    list.splice(targetIndex, 0, id)
    if (list.join() !== ids().join()) props.onReorder(list)
  }

  const onDrop = (targetId: string) => {
    const source = dragId()
    setDragId(null)
    setOverId(null)
    if (!source || source === targetId) return
    moveTo(source, ids().indexOf(targetId))
  }

  return (
    <ol class="space-y-2">
      <For each={props.questions}>
        {(q, index) => {
          const showSection = () => {
            const prev = index() > 0 ? props.questions[index() - 1].section : null
            return !!q.section && q.section !== prev
          }
          return (
            <>
              <Show when={showSection()}>
                <li class="pt-2 text-xs font-semibold uppercase tracking-wide text-slate-500" aria-hidden="true">{q.section}</li>
              </Show>
              <li
                draggable={props.editable}
                onDragStart={(e) => {
                  setDragId(q.questionId)
                  e.dataTransfer?.setData('text/plain', q.questionId)
                  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move'
                }}
                onDragOver={(e) => {
                  if (!dragId()) return
                  e.preventDefault()
                  setOverId(q.questionId)
                }}
                onDragLeave={() => setOverId(null)}
                onDrop={(e) => {
                  e.preventDefault()
                  onDrop(q.questionId)
                }}
                onDragEnd={() => {
                  setDragId(null)
                  setOverId(null)
                }}
                class={`group flex gap-3 rounded-lg border bg-white p-3 transition ${
                  overId() === q.questionId && dragId() !== q.questionId ? 'border-brand-400 ring-2 ring-brand-100' : 'border-slate-200'
                } ${dragId() === q.questionId ? 'opacity-50' : ''} ${q.questionStatus === 'Archived' ? 'border-amber-300 bg-amber-50/40' : ''}`}
              >
                <Show when={props.editable}>
                  <div class="flex flex-col items-center gap-0.5 text-slate-400">
                    <span class="cursor-grab p-1 active:cursor-grabbing" title="Drag to reorder" aria-hidden="true">
                      <svg viewBox="0 0 24 24" class="size-4" fill="currentColor"><circle cx="9" cy="6" r="1.5" /><circle cx="15" cy="6" r="1.5" /><circle cx="9" cy="12" r="1.5" /><circle cx="15" cy="12" r="1.5" /><circle cx="9" cy="18" r="1.5" /><circle cx="15" cy="18" r="1.5" /></svg>
                    </span>
                  </div>
                </Show>
                <div class="flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-50 text-sm font-semibold text-brand-700">
                  {index() + 1}
                </div>
                <div class="min-w-0 flex-1">
                  <button type="button" class="block text-start text-sm text-slate-900 hover:text-brand-700" onClick={() => props.onOpen(q.questionId)}>
                    {q.text}
                  </button>
                  <div class="mt-1.5 flex flex-wrap items-center gap-1.5">
                    <QuestionTypeBadge value={q.type} />
                    <DifficultyBadge value={q.difficulty} />
                    <Show when={q.topicName}><span class="text-xs text-slate-500">{q.topicName}</span></Show>
                    <Show when={q.questionStatus === 'Archived'}>
                      <span class="text-xs font-medium text-amber-700">Archived in bank — replace before publishing</span>
                    </Show>
                  </div>
                  <Show when={props.editable}>
                    <div class="mt-2 flex flex-wrap items-center gap-3 text-xs text-slate-600">
                      <label class="flex items-center gap-1.5">
                        Points
                        <input
                          type="number"
                          min={0.25}
                          step={0.25}
                          value={q.points}
                          class="h-7 w-20 rounded-md border border-slate-300 px-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
                          onChange={(e) => {
                            const value = Number(e.currentTarget.value)
                            if (value > 0 && value !== q.points) props.onUpdate(q.questionId, value, q.section)
                            else e.currentTarget.value = String(q.points)
                          }}
                        />
                      </label>
                      <label class="flex items-center gap-1.5">
                        Section
                        <input
                          value={q.section ?? ''}
                          placeholder="e.g. Part A"
                          class="h-7 w-36 rounded-md border border-slate-300 px-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
                          onChange={(e) => {
                            const value = e.currentTarget.value.trim() || null
                            if (value !== q.section) props.onUpdate(q.questionId, q.points, value)
                          }}
                        />
                      </label>
                    </div>
                  </Show>
                </div>
                <div class="flex shrink-0 flex-col items-end justify-between gap-1">
                  <span class="whitespace-nowrap text-sm font-semibold tabular-nums text-slate-700">{q.points} pts</span>
                  <Show when={props.editable}>
                    <div class="flex">
                      <IconButton icon="up" label={`Move question ${index() + 1} up`} disabled={index() === 0} onClick={() => moveTo(q.questionId, index() - 1)} />
                      <IconButton icon="down" label={`Move question ${index() + 1} down`} disabled={index() === props.questions.length - 1} onClick={() => moveTo(q.questionId, index() + 1)} />
                      <IconButton icon="trash" label={`Remove question ${index() + 1}`} tone="danger" onClick={() => props.onRemove(q)} />
                    </div>
                  </Show>
                </div>
              </li>
            </>
          )
        }}
      </For>
    </ol>
  )
}
