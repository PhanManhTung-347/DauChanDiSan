import { createContext, useContext } from 'react'
import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'
import type { Profile } from '../../shared/types'

const config = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
}
export const auth = Object.values(config).every(Boolean) ? getAuth(initializeApp(config)) : null
export type Dev = { uid: string; key: string }
type Session = { profile: Profile | null; loading: boolean; login: (email: string, password: string) => Promise<Profile>;
  loginGoogle: () => Promise<Profile>; loginDemo: (value: Dev) => Promise<void>; logout: () => Promise<void>; refresh: () => Promise<void> }
export const SessionContext = createContext<Session | null>(null)

export function useSession() {
  const value = useContext(SessionContext)
  if (!value) throw new Error('Thiếu SessionProvider.')
  return value
}
