import { api, unwrap } from './api'
import type { ApiResponse } from '~/types/api'
import type { CurrentUser, LoginResponse, RoleName, User } from '~/types/models'

export const authApi = {
  login: (email: string, password: string) =>
    unwrap(api.post<ApiResponse<LoginResponse>>('/api/auth/login', { email, password })),

  me: () => unwrap(api.get<ApiResponse<CurrentUser>>('/api/auth/me')),
}

export const usersApi = {
  list: (params: { role?: RoleName; search?: string } = {}) =>
    unwrap(api.get<ApiResponse<User[]>>('/api/users', { params })),

  create: (body: { fullName: string; email: string; password: string; roles: RoleName[] }) =>
    unwrap(api.post<ApiResponse<User>>('/api/users', body)),

  setActive: (id: string, isActive: boolean) =>
    api.patch<ApiResponse<null>>(`/api/users/${id}/status`, { isActive }),
}
