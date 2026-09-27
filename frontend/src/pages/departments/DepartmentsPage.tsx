import { createResource, createSignal, Match, Show, Switch } from 'solid-js'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { DataTable } from '~/components/tables/DataTable'
import { Button, IconButton } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, PageHeader, SkeletonRows } from '~/components/ui/Primitives'
import { DepartmentFormDialog } from '~/features/departments/DepartmentFormDialog'
import { getErrorMessage } from '~/services/api'
import { departmentsApi } from '~/services/courses.api'
import { auth } from '~/stores/auth.store'
import type { Department } from '~/types/models'
import { formatDate } from '~/utils/format'

export default function DepartmentsPage() {
  const [departments, { refetch }] = createResource(departmentsApi.list)
  const [editing, setEditing] = createSignal<Department | null>(null)
  const [dialogOpen, setDialogOpen] = createSignal(false)
  const confirm = useConfirm()

  const openEditor = (department: Department | null) => {
    setEditing(department)
    setDialogOpen(true)
  }

  const remove = (department: Department) =>
    confirm.ask({
      title: `Delete ${department.name}?`,
      description: 'This cannot be undone. Departments that still have courses cannot be deleted.',
      confirmLabel: 'Delete',
      onConfirm: async () => {
        try {
          await departmentsApi.remove(department.id)
          toast.success('Department deleted')
          refetch()
        } catch (err) {
          toast.error('Could not delete department', getErrorMessage(err))
        }
      },
    })

  return (
    <>
      <PageHeader
        title="Departments"
        description="Academic departments that own courses."
        actions={
          <Show when={auth.isAdmin()}>
            <Button icon="plus" onClick={() => openEditor(null)}>New department</Button>
          </Show>
        }
      />
      <Card>
        <Switch>
          <Match when={departments.error}>
            <ErrorState error={departments.error} onRetry={refetch} />
          </Match>
          <Match when={departments.loading && !departments()}>
            <SkeletonRows rows={4} />
          </Match>
          <Match when={departments()?.length === 0}>
            <EmptyState icon="building" title="No departments yet" description={auth.isAdmin() ? 'Create the first department to start adding courses.' : 'An administrator has not created any departments yet.'} />
          </Match>
          <Match when={departments()}>
            {(rows) => (
              <DataTable
                rows={rows()}
                rowKey={(d) => d.id}
                columns={[
                  { header: 'Code', cell: (d) => <span class="font-semibold text-brand-700">{d.code}</span> },
                  { header: 'Name', cell: (d) => <span class="font-medium text-slate-900">{d.name}</span> },
                  { header: 'Description', cell: (d) => <span class="text-slate-600">{d.description ?? '—'}</span> },
                  { header: 'Courses', cell: (d) => d.courseCount },
                  { header: 'Created', cell: (d) => <span class="text-slate-500">{formatDate(d.createdAt)}</span> },
                  {
                    header: '',
                    class: 'text-end',
                    cell: (d) => (
                      <Show when={auth.isAdmin()}>
                        <div class="flex justify-end gap-1">
                          <IconButton icon="edit" label="Edit department" onClick={() => openEditor(d)} />
                          <IconButton icon="trash" label="Delete department" tone="danger" onClick={() => remove(d)} />
                        </div>
                      </Show>
                    ),
                  },
                ]}
              />
            )}
          </Match>
        </Switch>
      </Card>
      <DepartmentFormDialog open={dialogOpen()} department={editing()} onOpenChange={setDialogOpen} onSaved={refetch} />
      <confirm.Dialog />
    </>
  )
}
