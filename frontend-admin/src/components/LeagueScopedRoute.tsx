import { Navigate, Outlet } from 'react-router-dom'
import { useLeagueContext } from '../contexts/LeagueContext'

/**
 * Renders children only when an active league is selected; otherwise redirects to leagues list.
 * Use for routes that rely on context-based league (e.g. /seasons, /divisions).
 */
export function LeagueScopedRoute() {
  const { activeLeague } = useLeagueContext()

  if (!activeLeague) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
