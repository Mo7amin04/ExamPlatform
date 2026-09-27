import { For, Index, Match, Show, Switch } from 'solid-js'
import type { ExamAnswerKey, ExamHeader, ExamPreview, PreviewQuestion } from '~/types/models'
import { formatDate, formatDuration } from '~/utils/format'
import { questionTypeLabels } from '~/features/questions/questionMeta'

const hints: Partial<Record<PreviewQuestion['type'], string>> = {
  MultipleChoice: 'Choose one answer.',
  TrueFalse: 'Circle True or False.',
  MultipleSelect: 'Select all that apply.',
  Matching: 'Match each item on the left with the correct item on the right.',
  Ordering: 'Write the letters in the correct order.',
  FillBlank: 'Fill in the blank(s).',
}

const marks = (points: number) => `${points} mark${points === 1 ? '' : 's'}`

function PaperHeader(props: { header: ExamHeader; answerKey?: boolean }) {
  return (
    <header class="text-center">
      <p class="text-xl font-bold tracking-wide">{props.header.universityName}</p>
      <p class="text-sm text-slate-600">{props.header.departmentName}</p>
      <p class="text-sm">{props.header.courseCode} — {props.header.courseName}</p>
      <h1 class="mt-3 text-2xl font-bold">{props.header.title}</h1>
      <p class={`text-sm font-semibold uppercase tracking-wider ${props.answerKey ? 'text-red-700' : 'text-slate-500'}`}>
        {props.answerKey ? 'Answer key — confidential' : `${props.header.type} examination`}
      </p>
      <div class="mt-4 grid grid-cols-3 border-y border-slate-300 py-2 text-sm">
        <span class="text-start"><strong>Date:</strong> {props.header.examDate ? formatDate(props.header.examDate, { dateStyle: 'full' }) : '____________'}</span>
        <span><strong>Duration:</strong> {formatDuration(props.header.durationMinutes)}</span>
        <span class="text-end"><strong>Total marks:</strong> {props.header.totalPoints}</span>
      </div>
    </header>
  )
}

/** Student-facing exam paper, styled to resemble the printed/PDF version. Contains no answers. */
export function ExamPaper(props: { preview: ExamPreview }) {
  const sectionStarts = (index: number) => {
    const q = props.preview.questions[index]
    return !!q.section && (index === 0 || props.preview.questions[index - 1].section !== q.section)
  }

  return (
    <article class="exam-paper print-root mx-auto max-w-[210mm] bg-white px-[16mm] py-[14mm] text-[15px] text-slate-900 shadow-sm ring-1 ring-slate-200">
      <PaperHeader header={props.preview.header} />

      <section class="mt-5 space-y-4 rounded border border-slate-300 p-4 text-sm" aria-label="Student details">
        <p>Student Name: <span class="inline-block w-[70%] border-b border-slate-500 align-bottom">&nbsp;</span></p>
        <div class="flex justify-between gap-6">
          <p class="flex-1">Student ID: <span class="inline-block w-[60%] border-b border-slate-500 align-bottom">&nbsp;</span></p>
          <p class="flex-1 text-end">Signature: <span class="inline-block w-[55%] border-b border-slate-500 align-bottom">&nbsp;</span></p>
        </div>
      </section>

      <Show when={props.preview.header.instructions}>
        <section class="mt-4 rounded bg-slate-50 px-4 py-3 text-sm">
          <p class="font-semibold">Instructions</p>
          <p class="whitespace-pre-wrap">{props.preview.header.instructions}</p>
        </section>
      </Show>

      <ol class="mt-6 space-y-6">
        <Index each={props.preview.questions}>
          {(q, i) => (
            <>
              <Show when={sectionStarts(i)}>
                <li class="border-b border-slate-300 pb-1 pt-2 font-sans text-base font-bold" aria-hidden="true">{q().section}</li>
              </Show>
              <li class="exam-question">
                <div class="flex gap-3">
                  <span class="w-6 shrink-0 font-bold">{q().number}.</span>
                  <p class="flex-1 whitespace-pre-wrap">{q().text}</p>
                  <span class="shrink-0 font-sans text-xs text-slate-500">({marks(q().points)})</span>
                </div>
                <div class="ps-9">
                  <Show when={hints[q().type]}>
                    <p class="mt-1 font-sans text-xs italic text-slate-500">{hints[q().type]}</p>
                  </Show>
                  <Switch>
                    <Match when={q().type === 'Matching'}>
                      <div class="mt-2 grid grid-cols-2 gap-6">
                        <ul class="space-y-1">
                          <For each={q().options}>{(o) => <li>{o.label}. {o.text} <span class="text-slate-400">_____</span></li>}</For>
                        </ul>
                        <ul class="space-y-1">
                          <For each={q().matchItems}>{(o) => <li>{o.label}) {o.text}</li>}</For>
                        </ul>
                      </div>
                    </Match>
                    <Match when={q().type === 'Ordering'}>
                      <ul class="mt-2 space-y-1">
                        <For each={q().options}>{(o) => <li>{o.label}. {o.text}</li>}</For>
                      </ul>
                      <p class="mt-2 text-sm">Correct order: <span class="inline-block w-48 border-b border-slate-500">&nbsp;</span></p>
                    </Match>
                    <Match when={q().options.length}>
                      <ul class="mt-2 space-y-1.5">
                        <For each={q().options}>
                          {(o) => (
                            <li class="flex items-baseline gap-2">
                              <span class={`inline-block size-3.5 shrink-0 border border-slate-500 ${q().type === 'MultipleSelect' ? 'rounded-sm' : 'rounded-full'}`} />
                              <span>{o.label}. {o.text}</span>
                            </li>
                          )}
                        </For>
                      </ul>
                    </Match>
                  </Switch>
                  <Show when={q().answerLines > 0}>
                    <div class="mt-3 space-y-6" aria-hidden="true">
                      <For each={Array.from({ length: q().answerLines })}>{() => <div class="border-b border-slate-300" />}</For>
                    </div>
                  </Show>
                </div>
              </li>
            </>
          )}
        </Index>
      </ol>

      <p class="mt-10 text-center font-sans text-xs text-slate-400">— End of exam —</p>
    </article>
  )
}

/** Teacher-only answer key sheet. */
export function AnswerKeySheet(props: { answerKey: ExamAnswerKey }) {
  return (
    <article class="exam-paper print-root mx-auto max-w-[210mm] bg-white px-[16mm] py-[14mm] text-[15px] text-slate-900 shadow-sm ring-1 ring-slate-200">
      <PaperHeader header={props.answerKey.header} answerKey />
      <table class="mt-6 w-full border-collapse font-sans text-sm">
        <thead>
          <tr class="bg-slate-100 text-start">
            <th class="w-14 border-b border-slate-300 px-2 py-2 text-start">Q#</th>
            <th class="w-36 border-b border-slate-300 px-2 py-2 text-start">Type</th>
            <th class="border-b border-slate-300 px-2 py-2 text-start">Correct answer</th>
            <th class="w-20 border-b border-slate-300 px-2 py-2 text-end">Marks</th>
          </tr>
        </thead>
        <tbody>
          <For each={props.answerKey.items}>
            {(item) => (
              <tr class="exam-question align-top">
                <td class="border-b border-slate-200 px-2 py-2 font-bold">{item.number}</td>
                <td class="border-b border-slate-200 px-2 py-2 text-slate-600">{questionTypeLabels[item.type]}</td>
                <td class="border-b border-slate-200 px-2 py-2">
                  <p class="whitespace-pre-wrap">{item.correctAnswer}</p>
                  <Show when={item.explanation}>
                    <p class="mt-1 text-xs italic text-slate-500">{item.explanation}</p>
                  </Show>
                </td>
                <td class="border-b border-slate-200 px-2 py-2 text-end tabular-nums">{item.points}</td>
              </tr>
            )}
          </For>
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={3} class="px-2 py-2 text-end font-bold">Total</td>
            <td class="px-2 py-2 text-end font-bold tabular-nums">{props.answerKey.header.totalPoints}</td>
          </tr>
        </tfoot>
      </table>
    </article>
  )
}
