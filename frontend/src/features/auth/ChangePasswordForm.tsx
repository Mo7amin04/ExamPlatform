import { createSignal, Show } from 'solid-js'
import { InlineAlert } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Card, CardHeader } from '~/components/ui/Primitives'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { authApi } from '~/services/auth.api'
import { auth } from '~/stores/auth.store'
import { t } from '~/utils/i18n'

export function ChangePasswordForm() {
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [formError, setFormError] = createSignal('')
  const [saving, setSaving] = createSignal(false)

  async function submit(event: SubmitEvent & { currentTarget: HTMLFormElement }) {
    event.preventDefault()
    if (saving()) return
    const form = event.currentTarget
    const fields = new FormData(form)
    const currentPassword = String(fields.get('currentPassword') ?? '')
    const newPassword = String(fields.get('newPassword') ?? '')
    setErrors({})
    setFormError('')

    if (newPassword !== fields.get('confirmPassword')) {
      setErrors({ confirmPassword: t('The passwords do not match.') })
      return
    }

    setSaving(true)
    try {
      await authApi.changePassword(currentPassword, newPassword)
      form.reset()
      toast.success(t('Password changed'), t('Sign in with your new password.'))
      auth.logout()
    } catch (error) {
      const fieldErrors = getFieldErrors(error)
      setErrors(fieldErrors)
      if (!Object.keys(fieldErrors).length) setFormError(getErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card class="mt-6">
      <CardHeader title={t('Change password')} description={t('You will need to sign in again after changing your password.')} />
      <form class="max-w-lg space-y-4 p-5" onSubmit={submit}>
        <Show when={formError()}>{(error) => <InlineAlert tone="error">{error()}</InlineAlert>}</Show>
        <TextInput name="currentPassword" label={t('Current password')} type="password" autocomplete="current-password"
          required maxLength={128} disabled={saving()} error={errors().currentPassword} />
        <TextInput name="newPassword" label={t('New password')} type="password" autocomplete="new-password"
          required minLength={8} maxLength={128} disabled={saving()} error={errors().newPassword}
          hint={t('At least 8 characters with upper-case, lower-case and a digit.')} />
        <TextInput name="confirmPassword" label={t('Confirm new password')} type="password" autocomplete="new-password"
          required maxLength={128} disabled={saving()} error={errors().confirmPassword} />
        <Button type="submit" icon="key" loading={saving()}>{t('Change password')}</Button>
      </form>
    </Card>
  )
}
