import { t } from '~/utils/i18n'
import { createEffect, createSignal } from 'solid-js'
import { TextArea, TextInput } from '~/components/forms/Fields'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { departmentsApi } from '~/services/courses.api'
import type { Department } from '~/types/models'

export function GradeLevelFormDialog(props: {
  open: boolean
  gradeLevel: Department | null
  onOpenChange: (open: boolean) => void
  onSaved: () => void
}) {
  const [name, setName] = createSignal('')
  const [code, setCode] = createSignal('')
  const [description, setDescription] = createSignal('')
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [saving, setSaving] = createSignal(false)

  createEffect(() => {
    if (!props.open) return
    setName(props.gradeLevel?.name ?? '')
    setCode(props.gradeLevel?.code ?? '')
    setDescription(props.gradeLevel?.description ?? '')
    setErrors({})
  })

  const save = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setErrors({})
    const body = { name: name(), code: code(), description: description() || null }
    try {
      if (props.gradeLevel) await departmentsApi.update(props.gradeLevel.id, body)
      else await departmentsApi.create(body)
      toast.success(props.gradeLevel ? t('Grade level updated') : t('Grade level created'))
      props.onSaved()
      props.onOpenChange(false)
    } catch (err) {
      setErrors(getFieldErrors(err))
      if (!Object.keys(getFieldErrors(err)).length) toast.error(t('Could not save grade level'), getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={props.open}
      onOpenChange={props.onOpenChange}
      title={props.gradeLevel ? t('Edit grade level') : t('New grade level')}
      footer={
        <>
          <Button variant="secondary" onClick={() => props.onOpenChange(false)}>{t('Cancel')}</Button>
          <Button type="submit" form="grade-level-form" loading={saving()}>{t('Save')}</Button>
        </>
      }
    >
      <form id="grade-level-form" class="space-y-4" onSubmit={save}>
        <TextInput label={t('Grade level name')} required value={name()} onInput={(e) => setName(e.currentTarget.value)} error={errors().name}
          hint={t('Examples: First Year Primary or Third Year Secondary — Science.')} />
        <TextInput label={t('Grade level code')} required value={code()} onInput={(e) => setCode(e.currentTarget.value)} error={errors().code} placeholder="P1" />
        <TextArea label={t('Description')} value={description()} onInput={(e) => setDescription(e.currentTarget.value)} error={errors().description} />
      </form>
    </Modal>
  )
}
