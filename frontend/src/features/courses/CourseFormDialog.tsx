import { t } from '~/utils/i18n'
import { createEffect, createResource, createSignal } from 'solid-js'
import { SelectInput, TextArea, TextInput } from '~/components/forms/Fields'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { coursesApi, departmentsApi } from '~/services/courses.api'
import type { Course, CourseDetail } from '~/types/models'

export function CourseFormDialog(props: {
  open: boolean
  course: Course | CourseDetail | null
  defaultGradeLevelId?: string
  onOpenChange: (open: boolean) => void
  onSaved: (course: CourseDetail) => void
}) {
  const [gradeLevels] = createResource(() => props.open, () => departmentsApi.list())
  const [gradeLevelId, setGradeLevelId] = createSignal('')
  const [code, setCode] = createSignal('')
  const [name, setName] = createSignal('')
  const [description, setDescription] = createSignal('')
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [saving, setSaving] = createSignal(false)

  createEffect(() => {
    if (!props.open) return
    setGradeLevelId(props.course?.departmentId ?? props.defaultGradeLevelId ?? '')
    setCode(props.course?.code ?? '')
    setName(props.course?.name ?? '')
    setDescription(props.course?.description ?? '')
    setErrors({})
  })

  const save = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setErrors({})
    const body = {
      departmentId: gradeLevelId(),
      code: code(),
      name: name(),
      description: description() || null,
      creditHours: props.course?.creditHours ?? 0,
    }
    try {
      const saved = props.course ? await coursesApi.update(props.course.id, body) : await coursesApi.create(body)
      toast.success(props.course ? t('Subject updated') : t('Subject created'))
      props.onSaved(saved)
      props.onOpenChange(false)
    } catch (err) {
      const fieldErrors = getFieldErrors(err)
      setErrors(fieldErrors)
      if (!Object.keys(fieldErrors).length) toast.error(t('Could not save subject'), getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={props.open}
      onOpenChange={props.onOpenChange}
      title={props.course ? t('Edit subject') : t('New subject')}
      description={props.course ? undefined : t('You will be assigned to the subject automatically.')}
      footer={
        <>
          <Button variant="secondary" onClick={() => props.onOpenChange(false)}>{t('Cancel')}</Button>
          <Button type="submit" form="course-form" loading={saving()} disabled={gradeLevels.loading || !!gradeLevels.error || !gradeLevelId()}>{t('Save')}</Button>
        </>
      }
    >
      <form id="course-form" class="grid gap-4 sm:grid-cols-2" onSubmit={save}>
        <SelectInput
          class="sm:col-span-2"
          label={t('Grade level')}
          required
          placeholder={gradeLevels.loading ? t('Loading…') : t('Select a grade level')}
          options={(gradeLevels.error ? [] : gradeLevels() ?? []).map((level) => ({ value: level.id, label: level.name }))}
          value={gradeLevelId()}
          onChange={setGradeLevelId}
          error={gradeLevels.error ? getErrorMessage(gradeLevels.error) : errors().departmentId}
          hint={!gradeLevels.loading && !gradeLevels.error && !gradeLevels()?.length ? t('A grade level must be created before adding subjects.') : undefined}
        />
        <TextInput label={t('Subject name')} required value={name()} onInput={(e) => setName(e.currentTarget.value)} error={errors().name} />
        <TextInput label={t('Subject code')} required value={code()} onInput={(e) => setCode(e.currentTarget.value)} error={errors().code} placeholder="MATH" />
        <TextArea class="sm:col-span-2" label={t('Description')} value={description()} onInput={(e) => setDescription(e.currentTarget.value)} error={errors().description} />
      </form>
    </Modal>
  )
}
