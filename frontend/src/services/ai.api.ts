import { api, unwrap } from './api'
import type { ApiResponse } from '~/types/api'
import type {
  GenerateQuestionsRequest,
  GenerateQuestionsResult,
  GeneratedQuestion,
  MaterialText,
  QuestionContent,
  QuestionInput,
} from '~/types/models'

export const aiApi = {
  generate: (body: GenerateQuestionsRequest) =>
    unwrap(api.post<ApiResponse<GenerateQuestionsResult>>('/api/ai/questions/generate', body)),

  /** Improve a saved question (by id) or inline content (courseId + question). Nothing is persisted. */
  improve: (body: { questionId?: string; courseId?: string; topicId?: string | null; question?: QuestionContent; instructions?: string }) =>
    unwrap(api.post<ApiResponse<GeneratedQuestion>>('/api/ai/questions/improve', body)),

  /** Explicit teacher confirmation: saves reviewed questions to the bank as drafts. */
  accept: (questions: QuestionInput[], generationId?: string) =>
    unwrap(api.post<ApiResponse<{ questionIds: string[] }>>('/api/ai/questions/accept', { generationId, questions })),

  extractMaterial: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return unwrap(
      api.post<ApiResponse<MaterialText>>('/api/ai/materials/extract', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      }),
    )
  },
}
