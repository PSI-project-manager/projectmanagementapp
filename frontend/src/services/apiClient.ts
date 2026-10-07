/**
 * Shared fetch response handling.
 *
 * Errors the backend raises itself come back as ProblemDetails
 * ({ status, title, detail }), but 401/403 are produced by the ASP.NET Core
 * authentication/authorization middleware, which short-circuits the pipeline
 * before the exception handler runs - those have an empty body, so the status
 * code is the only thing to go on.
 */

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const FALLBACK_MESSAGES: Record<number, string> = {
  401: 'Your session has expired. Please log in again.',
  403: 'You do not have permission to do that.',
  404: 'Not found.',
}

export async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    const message =
      problem?.detail ||
      problem?.title ||
      FALLBACK_MESSAGES[response.status] ||
      'Request failed.'
    throw new ApiError(response.status, message)
  }
  return response.json()
}

/** True when a request failed because the user's roles were not enough. */
export function isForbidden(error: unknown): boolean {
  return error instanceof ApiError && error.status === 403
}
