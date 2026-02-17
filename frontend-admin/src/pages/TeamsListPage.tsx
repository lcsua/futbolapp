import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { useQuery } from '@tanstack/react-query'
import { teamsService } from '../api/teams'
import { leaguesService } from '../api/leagues'
import { useLeagueId, useActiveLeague } from '../contexts/LeagueContext'
import type { Team } from '../api/types'

export function TeamsListPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const navigate = useNavigate()
  const [searchTerm, setSearchTerm] = useState('')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const fromParams = !!params.leagueId
  const seasonsBase = fromParams && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'
  const divisionsBase = fromParams && leagueId ? `/leagues/${leagueId}/divisions` : '/divisions'
  const teamsBase = fromParams && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const { data: league } = useQuery({
    queryKey: ['leagues', leagueId],
    queryFn: ({ signal }) => leaguesService.getById(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: teams, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const filteredTeams = useMemo(() => {
    if (!teams) return []
    return teams.filter((team) =>
      team.name.toLowerCase().includes(searchTerm.toLowerCase())
    )
  }, [teams, searchTerm])

  const sortedTeams = useMemo(() => {
    return [...filteredTeams].sort((a: Team, b: Team) => {
      if (sortOrder === 'asc') return a.name.localeCompare(b.name)
      return b.name.localeCompare(a.name)
    })
  }, [filteredTeams, sortOrder])

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>Go to Leagues</Button>}>
        No league selected. Choose a league from the selector or open a league from the list.
      </Alert>
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {error instanceof Error ? error.message : 'Failed to load teams'}
      </Alert>
    )
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Button component={RouterLink} to="/" size="small">
          Leagues
        </Button>
        <Typography color="text.secondary">/</Typography>
        <Button component={RouterLink} to={seasonsBase} size="small">
          Seasons
        </Button>
        <Typography color="text.secondary">/</Typography>
        <Button component={RouterLink} to={divisionsBase} size="small">
          Divisions
        </Button>
      </Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Typography variant="h5" component="h2" fontWeight={600}>
          {league?.name ?? activeLeague?.name ?? 'League'} — Teams
        </Typography>
        <Button component={RouterLink} to={`${teamsBase}/new`} variant="contained" startIcon={<AddIcon />}>
          Create team
        </Button>
      </Box>

      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          gap: 2,
          mb: 2,
          alignItems: { xs: 'stretch', sm: 'center' },
        }}
      >
        <TextField
          fullWidth
          label="Search by team name"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ minWidth: { sm: 280 } }}
        />
        <ToggleButtonGroup
          value={sortOrder}
          exclusive
          onChange={(_, v) => v != null && setSortOrder(v)}
          size="small"
          aria-label="Sort by name"
        >
          <ToggleButton value="asc" aria-label="Name A to Z">
            Name A → Z
          </ToggleButton>
          <ToggleButton value="desc" aria-label="Name Z to A">
            Name Z → A
          </ToggleButton>
        </ToggleButtonGroup>
      </Box>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Showing {sortedTeams.length} of {teams?.length ?? 0} teams
      </Typography>

      {!teams?.length ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No teams yet. Create one to get started.
        </Typography>
      ) : !sortedTeams.length ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No teams found
        </Typography>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' }, gap: 2 }}>
          {sortedTeams.map((team) => (
            <Card key={team.id} variant="outlined" sx={{ height: '100%' }}>
              <CardActionArea
                component={RouterLink}
                to={`${teamsBase}/${team.id}/edit`}
                sx={{ height: '100%', display: 'block', textAlign: 'left' }}
              >
                <CardContent>
                  <Typography variant="h6" component="h3" gutterBottom>
                    {team.name}
                  </Typography>
                  {team.shortName ? (
                    <Typography variant="body2" color="text.secondary">
                      {team.shortName}
                    </Typography>
                  ) : null}
                </CardContent>
              </CardActionArea>
            </Card>
          ))}
        </Box>
      )}
    </Box>
  )
}
