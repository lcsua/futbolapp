import { useCallback, useEffect } from 'react'
import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  type SelectChangeEvent,
  Skeleton,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useLeagueContext } from '../contexts/LeagueContext'
import { leaguesService } from '../api/leagues'

export function LeagueSelector() {
  const { activeLeague, setActiveLeague } = useLeagueContext()
  const { data: leagues, isLoading, isError } = useQuery({
    queryKey: ['leagues'],
    queryFn: ({ signal }) => leaguesService.getMyLeagues(signal),
  })

  const handleChange = useCallback(
    (e: SelectChangeEvent<string>) => {
      const value = e.target.value
      if (!leagues) return
      const league = value === '' ? null : leagues.find((l) => l.id === value) ?? null
      setActiveLeague(league)
    },
    [leagues, setActiveLeague],
  )

  useEffect(() => {
    if (!leagues?.length) return
    if (activeLeague && !leagues.some((l) => l.id === activeLeague.id)) {
      setActiveLeague(null)
    }
  }, [leagues, activeLeague, setActiveLeague])

  if (isLoading) {
    return (
      <FormControl size="small" sx={{ minWidth: 180 }}>
        <Skeleton variant="rounded" height={40} />
      </FormControl>
    )
  }

  if (isError || !leagues?.length) {
    return null
  }

  return (
    <FormControl size="small" sx={{ minWidth: { xs: 140, sm: 200 } }}>
      <InputLabel id="league-select-label">League</InputLabel>
      <Select
        labelId="league-select-label"
        label="League"
        value={activeLeague?.id ?? ''}
        onChange={handleChange}
      >
        <MenuItem value="">
          <em>Select league</em>
        </MenuItem>
        {leagues.map((league) => (
          <MenuItem key={league.id} value={league.id}>
            {league.name}
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  )
}
