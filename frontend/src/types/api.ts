/** Standard API envelope returned by every JSON endpoint. */
export interface FieldError {
  field: string | null
  message: string
}

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errors: FieldError[]
}

export interface Pagination {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PagedResponse<T> extends ApiResponse<T[]> {
  pagination: Pagination
}

export interface Paged<T> {
  items: T[]
  pagination: Pagination
}
