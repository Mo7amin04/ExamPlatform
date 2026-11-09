import { t } from '~/utils/i18n'
import { A } from '@solidjs/router'
import { createResource, createSignal, Match, Show, Switch } from 'solid-js'
import { EmptyState, ErrorState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { DataTable } from '~/components/tables/DataTable'
import { Button, IconButton } from '~/components/ui/Button'
import { useConfirm } from '~/components/ui/Dialog'
import { Card, PageHeader, SkeletonRows } from '~/components/ui/Primitives'
import { GradeLevelFormDialog } from '~/features/grade-levels/GradeLevelFormDialog'
import { getErrorMessage } from '~/services/api'
import { departmentsApi } from '~/services/courses.api'
import { auth } from '~/stores/auth.store'
import type { Department } from '~/types/models'
import { formatDate } from '~/utils/format'

export default function GradeLevelsPage() {
  const [gradeLevels, { refetch }] = createResource(departmentsApi.list)
  const [editing, setEditing] = createSignal<Department | null>(null)
  const [dialogOpen, setDialogOpen] = createSignal(false)
  const confirm = useConfirm()

  const openEditor = (gradeLevel: Department | null) => {
    setEditing(gradeLevel)
    setDialogOpen(true)
  }

  const remove = (gradeLevel: Department) =>
    confirm.ask({
      title: t('Delete {name}?', { name: gradeLevel.name }),
      description: t('Move or delete the subjects in this grade level before deleting it.'),
      confirmLabel: t('Delete'),
      onConfirm: async () => {
        try {
          await departmentsApi.remove(gradeLevel.id)
          toast.success(t('Grade level deleted'))
          refetch()
        } catch (err) {
          toast.error(t('Could not delete grade level'), getErrorMessage(err))
        }
      },
    })

  return (
    <>
      <PageHeader
        title={t('Grade levels')}
        description={t('Organize subjects by grade level.')}
        actions={
          <Show when={auth.isAdmin()}>
            <Button icon="plus" onClick={() => openEditor(null)}>{t('New grade level')}</Button>
          </Show>
        }
      />
      <Card>
        <Switch>
          <Match when={gradeLevels.error}>
            <ErrorState error={gradeLevels.error} onRetry={refetch} />
          </Match>
          <Match when={gradeLevels.loading && !gradeLevels()}>
            <SkeletonRows rows={4} />
          </Match>
          <Match when={gradeLevels()?.length === 0}>
            <EmptyState icon="calendar" title={t('No grade levels yet')} description={auth.isAdmin() ? t('Create a grade level to start adding subjects.') : t('Ask an administrator to create a grade level.')} />
          </Match>
          <Match when={gradeLevels()}>
            {(rows) => (
              <DataTable
                rows={rows()}
                rowKey={(d) => d.id}
                columns={[
                  { get header() { return t('Code') }, cell: (d) => <span class="font-semibold text-brand-700">{d.code}</span> },
                  { get header() { return t('Grade level') }, cell: (level) => <A href={`/courses?gradeLevelId=${level.id}`} class="font-semibold text-brand-700 hover:underline">{level.name}</A> },
                  { get header() { return t('Description') }, cell: (d) => <span class="text-slate-600">{d.description ?? '—'}</span> },
                  { get header() { return t('Subjects') }, cell: (d) => d.courseCount },
                  { get header() { return t('Created') }, cell: (d) => <span class="text-slate-500">{formatDate(d.createdAt)}</span> },
                  {
                    header: '',
                    class: 'text-end',
                    cell: (d) => (
                      <Show when={auth.isAdmin()}>
                        <div class="flex justify-end gap-1">
                          <IconButton icon="edit" label={t('Edit grade level')} onClick={() => openEditor(d)} />
                          <IconButton icon="trash" label={t('Delete grade level')} tone="danger" onClick={() => remove(d)} />
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
      <GradeLevelFormDialog open={dialogOpen()} gradeLevel={editing()} onOpenChange={setDialogOpen} onSaved={refetch} />
      <confirm.Dialog />
    </>
  )
}
