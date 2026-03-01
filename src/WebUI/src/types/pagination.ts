/**
 * Backend PagedResult<T> karşılığı.
 * Axios interceptor body.data'yı unwrap ettiği için
 * component'lerde doğrudan bu yapı gelir.
 */
export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

/** Backend'e gönderilen pagination querystring parametreleri. */
export interface PaginationParams {
  page?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
  search?: string
}
