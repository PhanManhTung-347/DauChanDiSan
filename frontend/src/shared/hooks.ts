import { useEffect, useState } from 'react'
import { api, errorText } from './api'

export function useResource<T>(path: string | null) {
  const [state, setState] = useState<{ path: string | null; data?: T; error?: string }>({ path: null })
  const [version, setVersion] = useState(0)
  useEffect(() => {
    if (!path) return
    const abort = new AbortController()
    api<T>(path, 'GET', undefined, abort.signal).then(data => {
      if (!abort.signal.aborted) setState({ path, data })
    }).catch(error => {
      if (!abort.signal.aborted) setState({ path, error: errorText(error) })
    })
    return () => abort.abort()
  }, [path, version])
  return { data: path === state.path ? state.data : undefined, error: path === state.path ? state.error : undefined,
    loading: !!path && state.path !== path, reload: () => setVersion(v => v + 1) }
}

export function useAction() {
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  async function run<T>(task: () => Promise<T>, success = 'Đã lưu thay đổi.') {
    setBusy(true); setError(''); setMessage('')
    try {
      const result = await task()
      setMessage(success)
      return result
    } catch (err) { setError(errorText(err)); return undefined }
    finally { setBusy(false) }
  }
  return { busy, message, error, run }
}

