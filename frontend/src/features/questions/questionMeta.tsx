import { Badge, type BadgeTone } from '~/components/ui/Primitives'
import {
  BLOOM_LEVELS,
  DIFFICULTIES,
  QUESTION_STATUSES,
  QUESTION_TYPES,
  type Difficulty,
  type QuestionSource,
  type QuestionStatus,
  type QuestionType,
} from '~/types/models'
import { humanize } from '~/utils/format'

export const questionTypeLabels: Record<QuestionType, string> = {
  MultipleChoice: 'Multiple Choice',
  TrueFalse: 'True / False',
  MultipleSelect: 'Multiple Select',
  ShortAnswer: 'Short Answer',
  Essay: 'Essay',
  FillBlank: 'Fill in the Blank',
  Matching: 'Matching',
  Ordering: 'Ordering',
}

export const questionTypeOptions = QUESTION_TYPES.map((t) => ({ value: t, label: questionTypeLabels[t] }))
export const difficultyOptions = DIFFICULTIES.map((d) => ({ value: d, label: d }))
export const bloomOptions = BLOOM_LEVELS.map((b) => ({ value: b, label: b }))
export const statusOptions = QUESTION_STATUSES.map((s) => ({ value: s, label: s }))

const difficultyTone: Record<Difficulty, BadgeTone> = { Easy: 'green', Medium: 'amber', Hard: 'red' }
const statusTone: Record<QuestionStatus, BadgeTone> = { Draft: 'amber', Approved: 'green', Archived: 'gray' }

export function DifficultyBadge(props: { value: Difficulty }) {
  return <Badge tone={difficultyTone[props.value]}>{props.value}</Badge>
}

export function QuestionStatusBadge(props: { value: QuestionStatus }) {
  return <Badge tone={statusTone[props.value]}>{props.value}</Badge>
}

export function QuestionTypeBadge(props: { value: QuestionType }) {
  return <Badge tone="blue">{questionTypeLabels[props.value] ?? humanize(props.value)}</Badge>
}

export function SourceBadge(props: { value: QuestionSource }) {
  return props.value === 'AI' ? <Badge tone="violet">✨ AI</Badge> : null
}

export const usesOptions = (type: QuestionType) =>
  type === 'MultipleChoice' || type === 'MultipleSelect' || type === 'TrueFalse' || type === 'Matching' || type === 'Ordering'

export const requiresExpectedAnswer = (type: QuestionType) => type === 'ShortAnswer' || type === 'FillBlank'
