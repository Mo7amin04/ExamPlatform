import { useLocation, useNavigate } from '@solidjs/router'
import type { ParentProps } from 'solid-js'
import { ToastRegion } from '~/components/feedback/toast'
import { auth } from '~/stores/auth.store'

/**
 * Router root: global toasts and 401 redirect handling.
 * Deliberately no app-wide <Suspense>: pages render their own loading/empty/error states, and a global
 * boundary would blank the whole page every time any resource refetches.
 */
export function AppRoot(props: ParentProps) {
  const navigate = useNavigate()
  const location = useLocation()

  auth.onUnauthorized(() => {
    if (location.pathname !== '/login') {
      navigate(`/login?redirect=${encodeURIComponent(location.pathname + location.search)}`, { replace: true })
    }
  })

  return (
    <>
      {props.children}
      <ToastRegion />
    </>
  )
}
