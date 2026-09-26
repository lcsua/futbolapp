import { Navigate, Outlet } from 'react-router-dom'
import { CircularProgress, Box } from '@mui/material'
import { usePermissions } from '../contexts/PermissionContext'

type PermissionRouteProps = {
  permission: string
  children?: React.ReactNode
}

export function PermissionRoute({ permission, children }: PermissionRouteProps) {
  const { hasPermission, isLoading, permissions } = usePermissions()

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (!hasPermission(permission)) {
    const fallback = permissions.includes('matches')
      ? '/matches'
      : permissions.includes('standings')
        ? '/standings'
        : '/'
    return <Navigate to={fallback} replace />
  }

  return children ? <>{children}</> : <Outlet />
}
