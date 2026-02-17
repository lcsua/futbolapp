import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
} from 'react'
import { authApi } from '../api/auth'
import {
  clearAuth,
  getStoredUser,
  setAuth,
  type StoredAuthUser,
} from '../lib/authStorage'

type AuthContextValue = {
  user: StoredAuthUser | null
  isAuthenticated: boolean
  login: (email: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function readStoredAuth(): StoredAuthUser | null {
  const user = getStoredUser()
  const token = localStorage.getItem('auth_token')
  if (!user || !token) return null
  return user
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<StoredAuthUser | null>(readStoredAuth)

  const login = useCallback(async (email: string) => {
    const res = await authApi.login(email.trim())
    const stored: StoredAuthUser = {
      userId: res.userId,
      email: res.email,
      role: res.role,
    }
    setAuth(stored, res.token)
    setUser(stored)
  }, [])

  const logout = useCallback(() => {
    clearAuth()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      login,
      logout,
    }),
    [user, login, logout],
  )

  return (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
