import { createRoot } from 'solid-js'
import { createStore } from 'solid-js/store'
import { configureApiAuth } from '~/services/api'
import { authApi } from '~/services/auth.api'
import type { CurrentUser, RoleName } from '~/types/models'
import { showToast } from '~/components/feedback/toast'

/**
 * Token storage model: the JWT is short-lived (8h by default) and kept in localStorage together with its
 * expiry, so it survives reloads/new tabs. It is cleared on logout, on expiry and on any 401 response.
 * The API never trusts client-provided identity: every request is re-authorized from the token.
 */
const STORAGE_KEY = 'exam-platform.session'

interface StoredSession {
  token: string
  expiresAt: string
}

type AuthStatus = 'anonymous' | 'loading' | 'authenticated'

interface AuthState {
  token: string | null
  expiresAt: string | null
  user: CurrentUser | null
  status: AuthStatus
}

function readSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw) as StoredSession
    if (!session.token || new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return session
  } catch {
    return null
  }
}

function createAuthStore() {
  const session = readSession()
  const [state, setState] = createStore<AuthState>({
    token: session?.token ?? null,
    expiresAt: session?.expiresAt ?? null,
    user: null,
    status: session ? 'loading' : 'anonymous',
  })

  let unauthorizedHandler: (() => void) | null = null

  function isTokenValid() {
    return !!state.token && !!state.expiresAt && new Date(state.expiresAt).getTime() > Date.now()
  }

  function clearSession() {
    try {
      localStorage.removeItem(STORAGE_KEY)
    } catch {
      /* storage unavailable */
    }
    setState({ token: null, expiresAt: null, user: null, status: 'anonymous' })
  }

  async function login(email: string, password: string) {
    const result = await authApi.login(email, password)
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ token: result.accessToken, expiresAt: result.expiresAt }))
    } catch {
      /* storage unavailable: session lasts for this page only */
    }
    setState({ token: result.accessToken, expiresAt: result.expiresAt, user: result.user, status: 'authenticated' })
    return result.user
  }

  /** Validates a stored token by loading /api/auth/me. */
  async function loadCurrentUser() {
    if (!isTokenValid()) {
      clearSession()
      return null
    }
    setState('status', 'loading')
    try {
      const user = await authApi.me()
      setState({ user, status: 'authenticated' })
      return user
    } catch {
      clearSession()
      return null
    }
  }

  function logout() {
    clearSession()
  }

  const hasRole = (role: RoleName) => state.user?.roles.includes(role) ?? false

  configureApiAuth({
    getToken: () => (isTokenValid() ? state.token : null),
    onUnauthorized: () => {
      const wasAuthenticated = state.status === 'authenticated'
      clearSession()
      if (wasAuthenticated) showToast({ title: 'Session expired', description: 'Please sign in again.', variant: 'warning' })
      unauthorizedHandler?.()
    },
    onForbidden: (message) => showToast({ title: 'Access denied', description: message, variant: 'error' }),
  })

  return {
    state,
    login,
    logout,
    loadCurrentUser,
    hasRole,
    isAdmin: () => hasRole('Admin'),
    isAuthenticated: () => state.status === 'authenticated' && !!state.user,
    /** Registers the navigation callback used after a 401 (set by the router root). */
    onUnauthorized: (handler: () => void) => {
      unauthorizedHandler = handler
    },
  }
}

export const auth = createRoot(createAuthStore)
