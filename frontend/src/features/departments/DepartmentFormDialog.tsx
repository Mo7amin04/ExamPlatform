import { createEffect, createSignal } from 'solid-js'
import { TextArea, TextInput } from '~/components/forms/Fields'
import { toast } from '~/components/feedback/toast'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { departmentsApi } from '~/services/courses.api'
import type { Department } from '~/types/models'

export function DepartmentFormDialog(props: {
  open: boolean
  department: Department | null
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
    setName(props.department?.name ?? '')
    setCode(props.department?.code ?? '')
    setDescription(props.department?.description ?? '')
    setErrors({})
  })

  const save = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setErrors({})
    const body = { name: name(), code: code(), description: description() || null }
    try {
      if (props.department) await departmentsApi.update(props.department.id, body)
      else await departmentsApi.create(body)
      toast.success(props.department ? 'Department updated' : 'Department created')
      props.onSaved()
      props.onOpenChange(false)
    } catch (err) {
      setErrors(getFieldErrors(err))
      if (!Object.keys(getFieldErrors(err)).length) toast.error('Could not save department', getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={props.open}
      onOpenChange={props.onOpenChange}
      title={props.department ? 'Edit department' : 'New department'}
      footer={
        <>
          <Button variant="secondary" onClick={() => props.onOpenChange(false)}>Cancel</Button>
          <Button type="submit" form="department-form" loading={saving()}>Save</Button>
        </>
      }
    >
      <form id="department-form" class="space-y-4" onSubmit={save}>
        <TextInput label="Name" required value={name()} onInput={(e) => setName(e.currentTarget.value)} error={errors().name} />
        <TextInput label="Code" required value={code()} onInput={(e) => setCode(e.currentTarget.value)} error={errors().code} hint="Short unique code, e.g. CS" />
        <TextArea label="Description" value={description()} onInput={(e) => setDescription(e.currentTarget.value)} error={errors().description} />
      </form>
    </Modal>
  )
}
