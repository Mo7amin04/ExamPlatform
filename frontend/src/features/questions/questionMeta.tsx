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
import { t, type TranslationKey } from '~/utils/i18n'

const questionTypeMessages: Record<QuestionType, TranslationKey> = {
  MultipleChoice: 'Multiple Choice',
  TrueFalse: 'True / False',
  MultipleSelect: 'Multiple Select',
  ShortAnswer: 'Short Answer',
  Essay: 'Essay',
  FillBlank: 'Fill in the Blank',
  Matching: 'Matching',
  Ordering: 'Ordering',
}

export function questionTypeLabel(value: QuestionType) {
  const message = questionTypeMessages[value]
  return message ? t(message) : humanize(value)
}

export function questionOptionLabel(type: QuestionType, text: string) {
  return type === 'TrueFalse' && (text === 'True' || text === 'False') ? t(text) : text
}

export const questionTypeOptions = QUESTION_TYPES.map((value) => ({ value, get label() { return questionTypeLabel(value) } }))
export const difficultyOptions = DIFFICULTIES.map((value) => ({ value, get label() { return t(value) } }))
export const bloomOptions = BLOOM_LEVELS.map((value) => ({ value, get label() { return t(value) } }))
export const statusOptions = QUESTION_STATUSES.map((value) => ({ value, get label() { return t(value) } }))

const difficultyTone: Record<Difficulty, BadgeTone> = { Easy: 'green', Medium: 'amber', Hard: 'red' }
const statusTone: Record<QuestionStatus, BadgeTone> = { Draft: 'amber', Approved: 'green', Archived: 'gray' }

export function DifficultyBadge(props: { value: Difficulty }) {
  return <Badge tone={difficultyTone[props.value]}>{t(props.value)}</Badge>
}

export function QuestionStatusBadge(props: { value: QuestionStatus }) {
  return <Badge tone={statusTone[props.value]}>{t(props.value)}</Badge>
}

export function QuestionTypeBadge(props: { value: QuestionType }) {
  return <Badge tone="blue">{questionTypeLabel(props.value)}</Badge>
}

export function SourceBadge(props: { value: QuestionSource }) {
  return props.value === 'AI' ? <Badge tone="violet">{t('✨ AI')}</Badge> : null
}

export const usesOptions = (type: QuestionType) =>
  type === 'MultipleChoice' || type === 'MultipleSelect' || type === 'TrueFalse' || type === 'Matching' || type === 'Ordering'

export const requiresExpectedAnswer = (type: QuestionType) => type === 'ShortAnswer' || type === 'FillBlank'
