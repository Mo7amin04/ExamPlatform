import { A, useLocation, useNavigate } from '@solidjs/router'
import { createSignal, Show } from 'solid-js'
import { InlineAlert } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { Card } from '~/components/ui/Primitives'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { authApi } from '~/services/auth.api'
import { auth } from '~/stores/auth.store'
import { t } from '~/utils/i18n'

export default function PasswordRecoveryPage(props: { reset?: boolean }) {
  const navigate = useNavigate()
  const location = useLocation()
  const token = () => new URLSearchParams(location.hash.slice(1)).get('token') ?? ''
  const [sent, setSent] = createSignal(false)
  const [busy, setBusy] = createSignal(false)
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [formError, setFormError] = createSignal('')

  async function submit(event: SubmitEvent & { currentTarget: HTMLFormElement }) {
    event.preventDefault()
    if (busy()) return
    const form = event.currentTarget
    const fields = new FormData(form)
    const password = String(fields.get('newPassword') ?? '')
    setErrors({})
    setFormError('')
    if (props.reset && password !== fields.get('confirmPassword')) {
      setErrors({ confirmPassword: t('The passwords do not match.') })
      return
    }

    setBusy(true)
    try {
      if (props.reset) {
        await authApi.resetPassword(token(), password)
        form.reset()
        auth.logout()
        toast.success(t('Password changed'), t('Sign in with your new password.'))
        navigate('/login', { replace: true })
      } else {
        await authApi.requestPasswordReset(String(fields.get('email') ?? '').trim())
        setSent(true)
      }
    } catch (error) {
      const fieldErrors = getFieldErrors(error)
      setErrors(fieldErrors)
      if (fieldErrors.token || !Object.keys(fieldErrors).length) setFormError(fieldErrors.token ?? getErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card class="w-full max-w-md p-6 sm:p-8">
      <h1 class="font-display text-3xl font-extrabold text-ink">{props.reset ? t('Reset password') : t('Forgot password?')}</h1>
      <div class="mt-5">
        <Show when={sent()} fallback={
          <Show when={!props.reset || token()} fallback={<InlineAlert tone="error">{t('This password reset link is invalid or has expired.')}</InlineAlert>}>
            <form class="space-y-4" onSubmit={submit}>
              <Show when={formError()}>{(error) => <InlineAlert tone="error">{error()}</InlineAlert>}</Show>
              <Show when={props.reset} fallback={
                <TextInput name="email" label={t('auth.email')} type="email" autocomplete="email" required maxLength={256}
                  disabled={busy()} error={errors().email} hint={t('Enter the email address associated with your account.')} />
              }>
                <TextInput name="newPassword" label={t('New password')} type="password" autocomplete="new-password"
                  required minLength={8} maxLength={128} disabled={busy()} error={errors().newPassword}
                  hint={t('At least 8 characters with upper-case, lower-case and a digit.')} />
                <TextInput name="confirmPassword" label={t('Confirm new password')} type="password" autocomplete="new-password"
                  required maxLength={128} disabled={busy()} error={errors().confirmPassword} />
              </Show>
              <Button type="submit" class="w-full" loading={busy()}>{props.reset ? t('Reset password') : t('Send reset link')}</Button>
            </form>
          </Show>
        }>
          <InlineAlert tone="success">{t('If an active account uses that email, a password reset link will be sent.')}</InlineAlert>
        </Show>
      </div>
      <div class="mt-5 flex flex-wrap gap-4 text-sm font-semibold text-brand-700">
        <A href="/login" class="hover:underline">{t('Back to sign in')}</A>
        <Show when={props.reset}><A href="/forgot-password" class="hover:underline">{t('Request a new link')}</A></Show>
      </div>
    </Card>
  )
}
