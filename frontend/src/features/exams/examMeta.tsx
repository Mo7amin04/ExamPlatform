import { Badge, type BadgeTone } from '~/components/ui/Primitives'
import { EXAM_STATUSES, EXAM_TYPES, type ExamStatus } from '~/types/models'
import { t } from '~/utils/i18n'

const statusTone: Record<ExamStatus, BadgeTone> = {
  Draft: 'amber',
  Ready: 'blue',
  Published: 'green',
  Archived: 'gray',
}

export function ExamStatusBadge(props: { value: ExamStatus }) {
  return <Badge tone={statusTone[props.value]}>{t(props.value)}</Badge>
}

export const examTypeOptions = EXAM_TYPES.map((value) => ({ value, get label() { return t(value) } }))
export const examStatusOptions = EXAM_STATUSES.map((value) => ({ value, get label() { return t(value) } }))

export const isExamEditable = (status: ExamStatus) => status === 'Draft' || status === 'Ready'
