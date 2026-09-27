import axios, { AxiosError, type AxiosResponse } from 'axios'
import type { ApiResponse, FieldError, Paged, PagedResponse } from '~/types/api'

/**
 * Shared Axios instance. In development the base URL is empty and Vite proxies /api to the backend.
 * Auth handling (token attach, 401/403) is wired by the auth store via `configureApiAuth`.
 */
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '',
  headers: { 'Content-Type': 'application/json' },
  timeout: 120_000,
})

interface AuthHooks {
  getToken: () => string | null
  onUnauthorized: () => void
  onForbidden: (message: string) => void
}

let hooks: AuthHooks | null = null

export function configureApiAuth(authHooks: AuthHooks) {
  hooks = authHooks
}

api.interceptors.request.use((config) => {
  const token = hooks?.getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiResponse<unknown>>) => {
    const status = error.response?.status
    const isLoginCall = error.config?.url?.includes('/api/auth/login')
    if (status === 401 && !isLoginCall) hooks?.onUnauthorized()
    if (status === 403) hooks?.onForbidden(getErrorMessage(error))
    return Promise.reject(error)
  },
)

/** Returns `data` from the standard envelope. */
export async function unwrap<T>(request: Promise<AxiosResponse<ApiResponse<T>>>): Promise<T> {
  const response = await request
  return response.data.data
}

/** Returns items + pagination from a paginated envelope. */
export async function unwrapPaged<T>(request: Promise<AxiosResponse<PagedResponse<T>>>): Promise<Paged<T>> {
  const response = await request
  return { items: response.data.data, pagination: response.data.pagination }
}

/** Returns the full envelope (for endpoints whose message matters, e.g. delete vs archive). */
export async function envelope<T>(request: Promise<AxiosResponse<ApiResponse<T>>>): Promise<ApiResponse<T>> {
  const response = await request
  return response.data
}

/** Builds a query object without empty values. */
export function cleanParams<T extends object>(params: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ''),
  ) as Partial<T>
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) {
      return error.code === 'ECONNABORTED'
        ? 'The request timed out. Please try again.'
        : 'Cannot reach the server. Check your connection and try again.'
    }
    const body = error.response.data as Partial<ApiResponse<unknown>> | undefined
    if (body?.message) return body.message
    if (error.response.status === 404) return 'The requested resource was not found.'
  }
  if (error instanceof Error && error.message) return error.message
  return fallback
}

/** Maps API validation errors to a { fieldName: message } record (first message per field, camel-cased). */
export function getFieldErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError(error)) return {}
  const errors = (error.response?.data as Partial<ApiResponse<unknown>> | undefined)?.errors ?? []
  const result: Record<string, string> = {}
  for (const e of errors as FieldError[]) {
    const key = normalizeField(e.field)
    if (!(key in result)) result[key] = e.message
  }
  return result
}

function normalizeField(field: string | null): string {
  if (!field) return '_'
  return field
    .split('.')
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join('.')
}

export function isStatus(error: unknown, status: number) {
  return axios.isAxiosError(error) && error.response?.status === status
}
