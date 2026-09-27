import { Badge, type BadgeTone } from '~/components/ui/Primitives'
import { EXAM_STATUSES, EXAM_TYPES, type ExamStatus } from '~/types/models'

const statusTone: Record<ExamStatus, BadgeTone> = {
  Draft: 'amber',
  Ready: 'blue',
  Published: 'green',
  Archived: 'gray',
}

export function ExamStatusBadge(props: { value: ExamStatus }) {
  return <Badge tone={statusTone[props.value]}>{props.value}</Badge>
}

export const examTypeOptions = EXAM_TYPES.map((t) => ({ value: t, label: t }))
export const examStatusOptions = EXAM_STATUSES.map((s) => ({ value: s, label: s }))

export const isExamEditable = (status: ExamStatus) => status === 'Draft' || status === 'Ready'
