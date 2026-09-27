import { api, cleanParams, envelope, unwrap, unwrapPaged } from './api'
import type { ApiResponse, PagedResponse } from '~/types/api'
import type { ExamAnswerKey, ExamDetail, ExamFilters, ExamInput, ExamPreview, ExamSummary } from '~/types/models'

export type ExportKind = 'export/pdf' | 'export/word' | 'answer-key/pdf' | 'answer-key/word'

export const examsApi = {
  list: (filters: ExamFilters) =>
    unwrapPaged(api.get<PagedResponse<ExamSummary>>('/api/exams', { params: cleanParams(filters) })),
  get: (id: string) => unwrap(api.get<ApiResponse<ExamDetail>>(`/api/exams/${id}`)),
  create: (body: ExamInput) => unwrap(api.post<ApiResponse<ExamDetail>>('/api/exams', body)),
  update: (id: string, body: ExamInput) => unwrap(api.put<ApiResponse<ExamDetail>>(`/api/exams/${id}`, body)),
  remove: (id: string) => envelope(api.delete<ApiResponse<{ archived: boolean }>>(`/api/exams/${id}`)),

  addQuestions: (id: string, questionIds: string[], section?: string | null) =>
    unwrap(api.post<ApiResponse<ExamDetail>>(`/api/exams/${id}/questions`, { questionIds, section })),
  updateQuestion: (id: string, questionId: string, points: number, section: string | null) =>
    unwrap(api.put<ApiResponse<ExamDetail>>(`/api/exams/${id}/questions/${questionId}`, { points, section })),
  removeQuestion: (id: string, questionId: string) =>
    unwrap(api.delete<ApiResponse<ExamDetail>>(`/api/exams/${id}/questions/${questionId}`)),
  reorder: (id: string, questionIds: string[]) =>
    unwrap(api.put<ApiResponse<ExamDetail>>(`/api/exams/${id}/questions/order`, { questionIds })),

  publish: (id: string) => unwrap(api.post<ApiResponse<ExamDetail>>(`/api/exams/${id}/publish`)),
  markReady: (id: string) => unwrap(api.post<ApiResponse<ExamDetail>>(`/api/exams/${id}/ready`)),
  moveToDraft: (id: string) => unwrap(api.post<ApiResponse<ExamDetail>>(`/api/exams/${id}/draft`)),

  preview: (id: string) => unwrap(api.get<ApiResponse<ExamPreview>>(`/api/exams/${id}/preview`)),
  answerKey: (id: string) => unwrap(api.get<ApiResponse<ExamAnswerKey>>(`/api/exams/${id}/answer-key`)),

  /** Downloads a generated document as a Blob together with the server-suggested file name. */
  download: async (id: string, kind: ExportKind) => {
    const response = await api.get<Blob>(`/api/exams/${id}/${kind}`, { responseType: 'blob' })
    const disposition = response.headers['content-disposition'] as string | undefined
    return { blob: response.data, fileName: parseFileName(disposition) ?? `exam.${kind.endsWith('pdf') ? 'pdf' : 'docx'}` }
  },
}

function parseFileName(disposition?: string): string | null {
  if (!disposition) return null
  const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(disposition)
  if (utf8) return decodeURIComponent(utf8[1])
  const plain = /filename="?([^";]+)"?/i.exec(disposition)
  return plain ? plain[1] : null
}
