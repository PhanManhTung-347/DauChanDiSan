import type { User } from 'firebase/auth'

let firebaseUser: User | null = null
let demo: { uid: string; key: string } | null = null

export function setCredential(user: User | null, dev: typeof demo = null) {
  firebaseUser = user
  demo = dev
}

async function request(path: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  if (firebaseUser) headers.set('Authorization', 'Bearer ' + await firebaseUser.getIdToken())
  if (demo && import.meta.env.DEV) {
    headers.set('X-Dev-Uid', demo.uid)
    headers.set('X-Dev-Key', demo.key)
  }
  return fetch(path, { ...init, headers })
}

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export async function api<T>(path: string, method = 'GET', data?: unknown, signal?: AbortSignal): Promise<T> {
  let response: Response
  try {
    response = await request('/api' + path, {
      method, signal,
      headers: data !== undefined && !(data instanceof FormData) ? { 'Content-Type': 'application/json' } : undefined,
      body: data === undefined ? undefined : data instanceof FormData ? data : JSON.stringify(data),
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError(0, 'Không kết nối được API. Kiểm tra backend và thử lại.')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as {
      detail?: string; title?: string; errors?: Record<string, string[]>
    }
    const errors = Object.values(problem.errors ?? {}).flat().join(' ')
    const fallback = response.status === 401 ? 'Hãy đăng nhập lại để tiếp tục.'
      : response.status === 403 ? 'Bạn chưa có quyền thực hiện thao tác này.'
      : `Yêu cầu chưa thành công (HTTP ${response.status}).`
    throw new ApiError(response.status, problem.detail || errors || problem.title || fallback)
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}

export async function upload(file: File): Promise<string> {
  const form = new FormData()
  form.set('file', file)
  const result = await api<{ path: string }>('/media', 'POST', form)
  return result.path
}

export async function privateMedia(path: string, signal: AbortSignal) {
  const result = await request(mediaUrl(path), { signal })
  if (!result.ok) throw new ApiError(result.status, 'Ảnh chưa khả dụng.')
  return result.blob()
}

export function mediaUrl(path: string) {
  if (path.startsWith('/images/')) return path
  return '/api/media?path=' + encodeURIComponent(path)
}

export function qs(values: Record<string, string | number | null | undefined>) {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') params.set(key, String(value))
  })
  return params.size ? '?' + params : ''
}

export const errorText = (error: unknown) => error instanceof Error ? error.message : 'Đã xảy ra lỗi.'
