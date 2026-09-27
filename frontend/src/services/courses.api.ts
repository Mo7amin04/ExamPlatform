import { api, cleanParams, unwrap } from './api'
import type { ApiResponse } from '~/types/api'
import type { Course, CourseDetail, Dashboard, Department, Topic } from '~/types/models'

export interface DepartmentInput {
  name: string
  code: string
  description?: string | null
}

export interface CourseInput {
  departmentId: string
  code: string
  name: string
  description?: string | null
  creditHours: number
}

export interface TopicInput {
  name: string
  description?: string | null
  order?: number | null
}

export const departmentsApi = {
  list: () => unwrap(api.get<ApiResponse<Department[]>>('/api/departments')),
  create: (body: DepartmentInput) => unwrap(api.post<ApiResponse<Department>>('/api/departments', body)),
  update: (id: string, body: DepartmentInput) => unwrap(api.put<ApiResponse<Department>>(`/api/departments/${id}`, body)),
  remove: (id: string) => api.delete(`/api/departments/${id}`),
}

export const coursesApi = {
  list: (params: { departmentId?: string; search?: string } = {}) =>
    unwrap(api.get<ApiResponse<Course[]>>('/api/courses', { params: cleanParams(params) })),
  get: (id: string) => unwrap(api.get<ApiResponse<CourseDetail>>(`/api/courses/${id}`)),
  create: (body: CourseInput) => unwrap(api.post<ApiResponse<CourseDetail>>('/api/courses', body)),
  update: (id: string, body: CourseInput) => unwrap(api.put<ApiResponse<CourseDetail>>(`/api/courses/${id}`, body)),
  remove: (id: string) => api.delete(`/api/courses/${id}`),

  assignTeacher: (courseId: string, teacherId: string) => api.post(`/api/courses/${courseId}/teachers`, { teacherId }),
  removeTeacher: (courseId: string, teacherId: string) => api.delete(`/api/courses/${courseId}/teachers/${teacherId}`),

  topics: (courseId: string) => unwrap(api.get<ApiResponse<Topic[]>>(`/api/courses/${courseId}/topics`)),
  createTopic: (courseId: string, body: TopicInput) =>
    unwrap(api.post<ApiResponse<Topic>>(`/api/courses/${courseId}/topics`, body)),
  updateTopic: (id: string, body: TopicInput) => unwrap(api.put<ApiResponse<Topic>>(`/api/topics/${id}`, body)),
  removeTopic: (id: string) => api.delete(`/api/topics/${id}`),
}

export const dashboardApi = {
  get: () => unwrap(api.get<ApiResponse<Dashboard>>('/api/dashboard')),
}
