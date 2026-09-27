import { api, cleanParams, envelope, unwrap, unwrapPaged } from './api'
import type { ApiResponse, PagedResponse } from '~/types/api'
import type { Question, QuestionFilters, QuestionInput, QuestionListItem, QuestionStatus } from '~/types/models'

export const questionsApi = {
  list: (filters: QuestionFilters) =>
    unwrapPaged(api.get<PagedResponse<QuestionListItem>>('/api/questions', { params: cleanParams(filters) })),
  get: (id: string) => unwrap(api.get<ApiResponse<Question>>(`/api/questions/${id}`)),
  create: (body: QuestionInput) => unwrap(api.post<ApiResponse<Question>>('/api/questions', body)),
  update: (id: string, body: QuestionInput) => unwrap(api.put<ApiResponse<Question>>(`/api/questions/${id}`, body)),
  setStatus: (id: string, status: QuestionStatus) => api.patch(`/api/questions/${id}/status`, { status }),
  /** Deletes unused questions; archives questions referenced by exams (see `data.archived`). */
  remove: (id: string) => envelope(api.delete<ApiResponse<{ archived: boolean }>>(`/api/questions/${id}`)),
  duplicate: (id: string) => unwrap(api.post<ApiResponse<Question>>(`/api/questions/${id}/duplicate`)),
  tags: (search?: string) => unwrap(api.get<ApiResponse<string[]>>('/api/tags', { params: cleanParams({ search }) })),
}
