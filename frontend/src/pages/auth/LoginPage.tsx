import { Navigate, useNavigate, useSearchParams } from '@solidjs/router'
import { createSignal, Show } from 'solid-js'
import { InlineAlert } from '~/components/feedback/States'
import { TextInput } from '~/components/forms/Fields'
import { Button } from '~/components/ui/Button'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { auth } from '~/stores/auth.store'
import { t } from '~/utils/i18n'

export default function LoginPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [email, setEmail] = createSignal('')
  const [password, setPassword] = createSignal('')
  const [error, setError] = createSignal<string | null>(null)
  const [fieldErrors, setFieldErrors] = createSignal<Record<string, string>>({})
  const [submitting, setSubmitting] = createSignal(false)

  const redirectTarget = () => {
    const target = typeof params.redirect === 'string' ? params.redirect : '/dashboard'
    // Only allow in-app relative redirects.
    return target.startsWith('/') && !target.startsWith('//') ? target : '/dashboard'
  }

  const submit = async (e: SubmitEvent) => {
    e.preventDefault()
    setError(null)
    setFieldErrors({})
    setSubmitting(true)
    try {
      await auth.login(email().trim(), password())
      navigate(redirectTarget(), { replace: true })
    } catch (err) {
      setFieldErrors(getFieldErrors(err))
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Show when={!auth.isAuthenticated()} fallback={<Navigate href={redirectTarget()} />}>
      <div class="w-full max-w-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">{t('auth.title')}</h1>
        <p class="mt-1 text-sm text-slate-500">{t('app.tagline')}</p>

        <form class="mt-8 space-y-4" onSubmit={submit} novalidate>
          <Show when={error()}>
            <InlineAlert tone="error">{error()!}</InlineAlert>
          </Show>
          <TextInput
            label={t('auth.email')}
            type="email"
            autocomplete="username"
            required
            value={email()}
            onInput={(e) => setEmail(e.currentTarget.value)}
            error={fieldErrors().email}
            autofocus
          />
          <TextInput
            label={t('auth.password')}
            type="password"
            autocomplete="current-password"
            required
            value={password()}
            onInput={(e) => setPassword(e.currentTarget.value)}
            error={fieldErrors().password}
          />
          <Button type="submit" class="w-full" loading={submitting()}>
            {t('auth.submit')}
          </Button>
        </form>
      </div>
    </Show>
  )
}
