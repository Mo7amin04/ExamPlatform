import { createResource, createSignal, For, Show } from 'solid-js'
import { toast } from '~/components/feedback/toast'
import { SelectInput } from '~/components/forms/Fields'
import { Button, IconButton } from '~/components/ui/Button'
import { Card, CardHeader } from '~/components/ui/Primitives'
import { getErrorMessage } from '~/services/api'
import { usersApi } from '~/services/auth.api'
import { coursesApi } from '~/services/courses.api'
import { auth } from '~/stores/auth.store'
import type { CourseTeacher } from '~/types/models'

/** Lists course teachers; administrators can assign/remove teachers. */
export function TeacherManager(props: { courseId: string; teachers: CourseTeacher[]; onChanged: () => void }) {
  const [teachers] = createResource(() => auth.isAdmin(), (isAdmin) => (isAdmin ? usersApi.list({ role: 'Teacher' }) : Promise.resolve([])))
  const [selected, setSelected] = createSignal('')
  const [busy, setBusy] = createSignal(false)

  const available = () => (teachers() ?? []).filter((t) => t.isActive && !props.teachers.some((ct) => ct.id === t.id))

  const assign = async () => {
    if (!selected()) return
    setBusy(true)
    try {
      await coursesApi.assignTeacher(props.courseId, selected())
      toast.success('Teacher assigned')
      setSelected('')
      props.onChanged()
    } catch (err) {
      toast.error('Could not assign teacher', getErrorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  const remove = async (teacher: CourseTeacher) => {
    try {
      await coursesApi.removeTeacher(props.courseId, teacher.id)
      toast.success(`${teacher.fullName} removed from course`)
      props.onChanged()
    } catch (err) {
      toast.error('Could not remove teacher', getErrorMessage(err))
    }
  }

  return (
    <Card>
      <CardHeader title="Teachers" />
      <ul class="divide-y divide-slate-100">
        <For each={props.teachers} fallback={<li class="px-5 py-4 text-sm text-slate-500">No teachers assigned.</li>}>
          {(teacher) => (
            <li class="flex items-center gap-3 px-5 py-3">
              <div class="min-w-0 flex-1">
                <p class="text-sm font-medium text-slate-900">{teacher.fullName}</p>
                <p class="truncate text-xs text-slate-500">{teacher.email}</p>
              </div>
              <Show when={auth.isAdmin()}>
                <IconButton icon="x" label={`Remove ${teacher.fullName}`} tone="danger" onClick={() => remove(teacher)} />
              </Show>
            </li>
          )}
        </For>
      </ul>
      <Show when={auth.isAdmin()}>
        <div class="flex items-end gap-2 border-t border-slate-100 px-5 py-4">
          <SelectInput
            class="flex-1"
            label="Assign teacher"
            placeholder={available().length ? 'Select a teacher' : 'No available teachers'}
            options={available().map((t) => ({ value: t.id, label: t.fullName }))}
            value={selected()}
            onChange={setSelected}
          />
          <Button onClick={assign} loading={busy()} disabled={!selected()}>Assign</Button>
        </div>
      </Show>
    </Card>
  )
}
