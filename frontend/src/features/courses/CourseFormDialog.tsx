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
  onOpenChange: (open: boolean) => void
  onSaved: (course: CourseDetail) => void
}) {
  const [departments] = createResource(() => props.open, () => departmentsApi.list())
  const [departmentId, setDepartmentId] = createSignal('')
  const [code, setCode] = createSignal('')
  const [name, setName] = createSignal('')
  const [description, setDescription] = createSignal('')
  const [creditHours, setCreditHours] = createSignal(3)
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [saving, setSaving] = createSignal(false)

  createEffect(() => {
    if (!props.open) return
    setDepartmentId(props.course?.departmentId ?? '')
    setCode(props.course?.code ?? '')
    setName(props.course?.name ?? '')
    setDescription(props.course?.description ?? '')
    setCreditHours(props.course?.creditHours ?? 3)
    setErrors({})
  })

  const save = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setErrors({})
    const body = { departmentId: departmentId(), code: code(), name: name(), description: description() || null, creditHours: creditHours() }
    try {
      const saved = props.course ? await coursesApi.update(props.course.id, body) : await coursesApi.create(body)
      toast.success(props.course ? 'Course updated' : 'Course created')
      props.onSaved(saved)
      props.onOpenChange(false)
    } catch (err) {
      const fieldErrors = getFieldErrors(err)
      setErrors(fieldErrors)
      if (!Object.keys(fieldErrors).length) toast.error('Could not save course', getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={props.open}
      onOpenChange={props.onOpenChange}
      title={props.course ? 'Edit course' : 'New course'}
      description={props.course ? undefined : 'You will be assigned to the course automatically.'}
      footer={
        <>
          <Button variant="secondary" onClick={() => props.onOpenChange(false)}>Cancel</Button>
          <Button type="submit" form="course-form" loading={saving()}>Save</Button>
        </>
      }
    >
      <form id="course-form" class="grid gap-4 sm:grid-cols-2" onSubmit={save}>
        <SelectInput
          class="sm:col-span-2"
          label="Department"
          required
          placeholder={departments.loading ? 'Loading…' : 'Select a department'}
          options={(departments() ?? []).map((d) => ({ value: d.id, label: `${d.code} — ${d.name}` }))}
          value={departmentId()}
          onChange={setDepartmentId}
          error={errors().departmentId}
        />
        <TextInput label="Code" required value={code()} onInput={(e) => setCode(e.currentTarget.value)} error={errors().code} placeholder="CS101" />
        <TextInput
          label="Credit hours"
          type="number"
          min={0}
          max={12}
          value={creditHours()}
          onInput={(e) => setCreditHours(Number(e.currentTarget.value))}
          error={errors().creditHours}
        />
        <TextInput class="sm:col-span-2" label="Name" required value={name()} onInput={(e) => setName(e.currentTarget.value)} error={errors().name} />
        <TextArea class="sm:col-span-2" label="Description" value={description()} onInput={(e) => setDescription(e.currentTarget.value)} error={errors().description} />
      </form>
    </Modal>
  )
}
