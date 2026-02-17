import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import type { League } from '../api/types'

const STORAGE_KEY = 'admin_active_league'

function readStoredLeague(): League | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as League
    if (typeof parsed?.id === 'string' && typeof parsed?.name === 'string') return parsed
  } catch {
    // ignore
  }
  return null
}

function writeStoredLeague(league: League | null): void {
  if (league) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(league))
  } else {
    localStorage.removeItem(STORAGE_KEY)
  }
}

type LeagueContextValue = {
  activeLeague: League | null
  setActiveLeague: (league: League | null) => void
}

const LeagueContext = createContext<LeagueContextValue | null>(null)

export function LeagueProvider({ children }: { children: React.ReactNode }) {
  const [activeLeague, setActiveLeagueState] = useState<League | null>(readStoredLeague)

  const setActiveLeague = useCallback((league: League | null) => {
    setActiveLeagueState(league)
    writeStoredLeague(league)
  }, [])

  const value = useMemo<LeagueContextValue>(
    () => ({ activeLeague, setActiveLeague }),
    [activeLeague, setActiveLeague],
  )

  return (
    <LeagueContext.Provider value={value}>{children}</LeagueContext.Provider>
  )
}

export function useLeagueContext(): LeagueContextValue {
  const ctx = useContext(LeagueContext)
  if (!ctx) throw new Error('useLeagueContext must be used within LeagueProvider')
  return ctx
}

export function useActiveLeague(): League | null {
  return useLeagueContext().activeLeague
}

export function useSetActiveLeague(): (league: League | null) => void {
  return useLeagueContext().setActiveLeague
}

/** League id from route params or active league. Use in league-scoped pages. */
export function useLeagueId(): string | null {
  const { leagueId: leagueIdFromParams } = useParams<{ leagueId?: string }>()
  const { activeLeague } = useLeagueContext()
  return leagueIdFromParams ?? activeLeague?.id ?? null
}
