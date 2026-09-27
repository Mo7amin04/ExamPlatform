import { createResource } from 'solid-js'
import { SelectInput, TextArea, TextInput } from '~/components/forms/Fields'
import { coursesApi } from '~/services/courses.api'
import type { ExamInput, ExamType } from '~/types/models'
import { fromDateTimeLocal, toDateTimeLocal } from '~/utils/format'
import { examTypeOptions } from './examMeta'

/** Controlled exam settings fields (title, course, type, duration, date, description, instructions). */
export function ExamSettingsFields(props: {
  value: ExamInput
  onChange: <K extends keyof ExamInput>(key: K, value: ExamInput[K]) => void
  errors: Record<string, string>
  disabled?: boolean
  courseLocked?: boolean
}) {
  const [courses] = createResource(() => coursesApi.list())

  return (
    <fieldset class="space-y-4" disabled={props.disabled}>
      <TextInput label="Title" required value={props.value.title} onInput={(e) => props.onChange('title', e.currentTarget.value)} error={props.errors.title} />
      <SelectInput
        label="Course"
        required
        placeholder={courses.loading ? 'Loading…' : 'Select a course'}
        options={(courses() ?? []).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }))}
        value={props.value.courseId}
        onChange={(v) => props.onChange('courseId', v)}
        disabled={props.disabled || props.courseLocked}
        hint={props.courseLocked ? 'Remove all questions to change the course.' : undefined}
        error={props.errors.courseId}
      />
      <div class="grid grid-cols-2 gap-3">
        <SelectInput label="Exam type" options={examTypeOptions} value={props.value.type} onChange={(v) => props.onChange('type', v as ExamType)} error={props.errors.type} />
        <TextInput
          label="Duration (min)"
          type="number"
          min={1}
          required
          value={props.value.durationMinutes}
          onInput={(e) => props.onChange('durationMinutes', Number(e.currentTarget.value))}
          error={props.errors.durationMinutes}
        />
      </div>
      <TextInput
        label="Exam date"
        type="datetime-local"
        value={toDateTimeLocal(props.value.examDate)}
        onInput={(e) => props.onChange('examDate', fromDateTimeLocal(e.currentTarget.value))}
        error={props.errors.examDate}
      />
      <TextArea label="Description" rows={2} value={props.value.description ?? ''} onInput={(e) => props.onChange('description', e.currentTarget.value)} error={props.errors.description} />
      <TextArea
        label="Instructions for students"
        rows={3}
        value={props.value.instructions ?? ''}
        onInput={(e) => props.onChange('instructions', e.currentTarget.value)}
        hint="Printed at the top of the exam paper."
        error={props.errors.instructions}
      />
    </fieldset>
  )
}

export function validateExam(value: ExamInput): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!value.title.trim()) errors.title = 'Title is required.'
  if (!value.courseId) errors.courseId = 'Select a course.'
  if (!(value.durationMinutes > 0)) errors.durationMinutes = 'Duration must be greater than zero.'
  return errors
}
