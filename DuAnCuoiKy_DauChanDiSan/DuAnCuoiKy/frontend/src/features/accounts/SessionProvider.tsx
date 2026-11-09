import { useEffect, useState } from 'react'
import type { User } from 'firebase/auth'
import type { ReactNode } from 'react'
import { onAuthStateChanged, signInWithEmailAndPassword, GoogleAuthProvider, signInWithPopup, signOut } from 'firebase/auth'
import { api, setCredential } from '../../shared/api'
import type { Profile } from '../../shared/types'
import { auth, SessionContext } from './session'
import type { Dev } from './session'

const demoName = 'dau-chan-demo'
let pending: { uid: string; request: Promise<Profile> } | undefined

function sync(user: User) {
  if (pending?.uid === user.uid) return pending.request
  setCredential(user)
  const request = api<Profile>('/tai-khoan/dong-bo', 'POST').finally(() => {
    if (pending?.request === request) pending = undefined
  })
  pending = { uid: user.uid, request }
  return request
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(!!auth || (import.meta.env.DEV && !!sessionStorage.getItem(demoName)))

  useEffect(() => {
    let alive = true
    const saved = import.meta.env.DEV ? sessionStorage.getItem(demoName) : null
    if (saved) {
      const demo = JSON.parse(saved) as Dev
      setCredential(null, demo)
      api<Profile>('/tai-khoan/toi').then(p => { if (alive) setProfile(p) }).catch(() => {
        if (alive) { sessionStorage.removeItem(demoName); setCredential(null) }
      }).finally(() => { if (alive) setLoading(false) })
      return () => { alive = false }
    }
    if (!auth) return
    const stop = onAuthStateChanged(auth, user => {
      setCredential(user)
      if (!user) { setProfile(null); setLoading(false); return }
      if (!user.emailVerified) { setCredential(null); setProfile(null); setLoading(false); return }
      sync(user).then(p => { if (alive && auth?.currentUser?.uid === user.uid) setProfile(p) })
        .catch(() => { if (alive) setProfile(null) }).finally(() => { if (alive) setLoading(false) })
    })
    return () => { alive = false; stop() }
  }, [])

  return <SessionContext.Provider value={{
    profile, loading,
    async login(email, password) {
      if (!auth) throw new Error('Chưa cấu hình Firebase cho website.')
      sessionStorage.removeItem(demoName)
      const { user } = await signInWithEmailAndPassword(auth, email, password)
      if (!user.emailVerified) {
        await signOut(auth)
        throw new Error('Email chưa xác minh. Nếu đây là tài khoản từ bản cũ, hãy chọn xác minh tài khoản cũ.')
      }
      const current = await sync(user)
      setProfile(current)
      return current
    },
    async loginGoogle() {
      if (!auth) throw new Error('Chưa cấu hình Firebase cho website.')
      sessionStorage.removeItem(demoName)
      const provider = new GoogleAuthProvider()
      provider.setCustomParameters({ prompt: 'select_account' })
      const { user } = await signInWithPopup(auth, provider)
      const current = await sync(user)
      setProfile(current)
      return current
    },
    async loginDemo(value) {
      if (!import.meta.env.DEV) return
      if (auth?.currentUser) await signOut(auth)
      setCredential(null, value)
      try {
        setProfile(await api<Profile>('/tai-khoan/toi'))
        sessionStorage.setItem(demoName, JSON.stringify(value))
      } catch (error) { setCredential(null); throw error }
    },
    async logout() { sessionStorage.removeItem(demoName); if (auth?.currentUser) await signOut(auth); setCredential(null); setProfile(null) },
    async refresh() { setProfile(await api<Profile>('/tai-khoan/toi')) },
  }}>{children}</SessionContext.Provider>
}
