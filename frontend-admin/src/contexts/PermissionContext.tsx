import { createContext, useCallback, useContext, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { usersService, type LeagueAccess } from '../api/users'
import { useLeagueId } from './LeagueContext'

type PermissionContextValue = {
  access: LeagueAccess | null
  permissions: string[]
  isLoading: boolean
  hasPermission: (code: string) => boolean
}

const PermissionContext = createContext<PermissionContextValue | null>(null)

export function PermissionProvider({ children }: { children: React.ReactNode }) {
  const leagueId = useLeagueId()
  const { data, isLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'my-access'],
    queryFn: ({ signal }) => usersService.getMyAccess(leagueId!, signal),
    enabled: !!leagueId,
  })

  const permissions = data?.permissions ?? []

  const hasPermission = useCallback(
    (code: string) => permissions.some((p) => p.toLowerCase() === code.toLowerCase()),
    [permissions],
  )

  const value = useMemo<PermissionContextValue>(
    () => ({
      access: data ?? null,
      permissions,
      isLoading: !!leagueId && isLoading,
      hasPermission,
    }),
    [data, permissions, isLoading, leagueId, hasPermission],
  )

  return <PermissionContext.Provider value={value}>{children}</PermissionContext.Provider>
}

export function usePermissions(): PermissionContextValue {
  const ctx = useContext(PermissionContext)
  if (!ctx) throw new Error('usePermissions must be used within PermissionProvider')
  return ctx
}
