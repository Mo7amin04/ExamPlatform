import { Navigate, useLocation } from '@solidjs/router'
import { Match, onMount, Switch, type ParentProps } from 'solid-js'
import { LoadingState } from '~/components/feedback/States'
import { auth } from '~/stores/auth.store'
import type { RoleName } from '~/types/models'
import { ForbiddenPage } from '~/pages/errors/ErrorPages'
import { toAppPath } from '~/utils/basePath'

/**
 * Protects routes: restores the session from storage (validated via /api/auth/me),
 * redirects anonymous users to /login and blocks users lacking a required role.
 */
export function RequireAuth(props: ParentProps<{ roles?: RoleName[] }>) {
  const location = useLocation()

  onMount(() => {
    if (auth.state.status === 'loading' || (auth.state.token && !auth.state.user)) void auth.loadCurrentUser()
  })

  const allowed = () => !props.roles || props.roles.some((r) => auth.hasRole(r))

  return (
    <Switch>
      <Match when={auth.state.status === 'loading' || (auth.state.token && !auth.state.user && auth.state.status !== 'anonymous')}>
        <LoadingState label="Restoring your session…" />
      </Match>
      <Match when={!auth.isAuthenticated()}>
        <Navigate href={`/login?redirect=${encodeURIComponent(toAppPath(location.pathname) + location.search)}`} />
      </Match>
      <Match when={!allowed()}>
        <ForbiddenPage />
      </Match>
      <Match when={true}>{props.children}</Match>
    </Switch>
  )
}
