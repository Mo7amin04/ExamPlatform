import { createResource, createSignal, Match, Show, Switch } from 'solid-js'
import { ErrorState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { SelectInput, TextInput } from '~/components/forms/Fields'
import { DataTable } from '~/components/tables/DataTable'
import { Button } from '~/components/ui/Button'
import { Modal } from '~/components/ui/Dialog'
import { Badge, Card, CardHeader, PageHeader, SkeletonRows } from '~/components/ui/Primitives'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { usersApi } from '~/services/auth.api'
import { auth } from '~/stores/auth.store'
import type { RoleName } from '~/types/models'
import { formatDateTime } from '~/utils/format'
import { locale, setLocale } from '~/utils/i18n'

function UserManagement() {
  const [users, { refetch }] = createResource(() => usersApi.list())
  const [open, setOpen] = createSignal(false)
  const [form, setForm] = createSignal({ fullName: '', email: '', password: '', role: 'Teacher' as RoleName })
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [saving, setSaving] = createSignal(false)

  const create = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setErrors({})
    try {
      const f = form()
      await usersApi.create({ fullName: f.fullName, email: f.email, password: f.password, roles: [f.role] })
      toast.success('User created')
      setOpen(false)
      refetch()
    } catch (err) {
      const fe = getFieldErrors(err)
      setErrors(fe)
      if (!Object.keys(fe).length) toast.error('Could not create user', getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const toggleActive = async (id: string, isActive: boolean) => {
    try {
      await usersApi.setActive(id, isActive)
      toast.success(isActive ? 'User activated' : 'User deactivated')
      refetch()
    } catch (err) {
      toast.error('Could not update user', getErrorMessage(err))
    }
  }

  return (
    <Card class="mt-6">
      <CardHeader
        title="Users"
        description="Create teacher and administrator accounts."
        actions={
          <Button size="sm" icon="plus" onClick={() => { setForm({ fullName: '', email: '', password: '', role: 'Teacher' }); setErrors({}); setOpen(true) }}>
            New user
          </Button>
        }
      />
      <Switch>
        <Match when={users.error}><ErrorState error={users.error} onRetry={refetch} /></Match>
        <Match when={!users()}><SkeletonRows rows={3} /></Match>
        <Match when={users()}>
          {(list) => (
            <DataTable
              rows={list()}
              rowKey={(u) => u.id}
              columns={[
                { header: 'Name', cell: (u) => <span class="font-medium text-slate-900">{u.fullName}</span> },
                { header: 'Email', cell: (u) => u.email },
                { header: 'Roles', cell: (u) => <div class="flex gap-1">{u.roles.map((r) => <Badge tone={r === 'Admin' ? 'violet' : 'blue'}>{r}</Badge>)}</div> },
                { header: 'Last login', cell: (u) => <span class="text-slate-500">{formatDateTime(u.lastLoginAt)}</span> },
                { header: 'Status', cell: (u) => <Badge tone={u.isActive ? 'green' : 'gray'}>{u.isActive ? 'Active' : 'Inactive'}</Badge> },
                {
                  header: '',
                  class: 'text-end',
                  cell: (u) => (
                    <Show when={u.id !== auth.state.user?.id}>
                      <Button size="sm" variant="ghost" onClick={() => toggleActive(u.id, !u.isActive)}>
                        {u.isActive ? 'Deactivate' : 'Activate'}
                      </Button>
                    </Show>
                  ),
                },
              ]}
            />
          )}
        </Match>
      </Switch>

      <Modal
        open={open()}
        onOpenChange={setOpen}
        title="New user"
        footer={
          <>
            <Button variant="secondary" onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" form="user-form" loading={saving()}>Create user</Button>
          </>
        }
      >
        <form id="user-form" class="space-y-4" onSubmit={create}>
          <TextInput label="Full name" required value={form().fullName} onInput={(e) => setForm({ ...form(), fullName: e.currentTarget.value })} error={errors().fullName} />
          <TextInput label="Email" type="email" required value={form().email} onInput={(e) => setForm({ ...form(), email: e.currentTarget.value })} error={errors().email} />
          <TextInput
            label="Initial password"
            type="password"
            autocomplete="new-password"
            required
            value={form().password}
            onInput={(e) => setForm({ ...form(), password: e.currentTarget.value })}
            error={errors().password}
            hint="At least 8 characters with upper-case, lower-case and a digit."
          />
          <SelectInput
            label="Role"
            options={[{ value: 'Teacher', label: 'Teacher' }, { value: 'Admin', label: 'Administrator' }]}
            value={form().role}
            onChange={(v) => setForm({ ...form(), role: v as RoleName })}
            error={errors().roles}
          />
        </form>
      </Modal>
    </Card>
  )
}

export default function SettingsPage() {
  return (
    <>
      <PageHeader title="Settings" description="Your profile and preferences." />
      <div class="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader title="Profile" />
          <dl class="grid gap-4 p-5 text-sm sm:grid-cols-2">
            <div><dt class="text-slate-500">Name</dt><dd class="font-medium text-slate-900">{auth.state.user?.fullName}</dd></div>
            <div><dt class="text-slate-500">Email</dt><dd class="font-medium text-slate-900">{auth.state.user?.email}</dd></div>
            <div><dt class="text-slate-500">Roles</dt><dd class="font-medium text-slate-900">{auth.state.user?.roles.join(', ')}</dd></div>
          </dl>
        </Card>
        <Card>
          <CardHeader title="Language & direction" />
          <div class="p-5">
            <SelectInput
              label="Interface language"
              options={[{ value: 'en', label: 'English (LTR)' }, { value: 'ar', label: 'العربية (RTL)' }]}
              value={locale()}
              onChange={(v) => setLocale(v as 'en' | 'ar')}
              hint="Arabic switches the layout to right-to-left."
            />
          </div>
        </Card>
      </div>
      <Show when={auth.isAdmin()}>
        <UserManagement />
      </Show>
    </>
  )
}
