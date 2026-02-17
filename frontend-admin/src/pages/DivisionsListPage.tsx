import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { useQuery } from '@tanstack/react-query'
import { divisionsService } from '../api/divisions'
import { leaguesService } from '../api/leagues'
import { useLeagueId, useActiveLeague } from '../contexts/LeagueContext'

export function DivisionsListPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const navigate = useNavigate()
  const fromParams = !!params.leagueId
  const seasonsBase = fromParams && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'
  const divisionsBase = fromParams && leagueId ? `/leagues/${leagueId}/divisions` : '/divisions'

  const { data: league } = useQuery({
    queryKey: ['leagues', leagueId],
    queryFn: ({ signal }) => leaguesService.getById(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: divisions, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

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
        {error instanceof Error ? error.message : 'Failed to load divisions'}
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
      </Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Typography variant="h5" component="h2" fontWeight={600}>
          {league?.name ?? activeLeague?.name ?? 'League'} — Divisions
        </Typography>
        <Button component={RouterLink} to={`${divisionsBase}/new`} variant="contained" startIcon={<AddIcon />}>
          Create division
        </Button>
      </Box>

      {!divisions?.length ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No divisions yet. Create one to get started.
        </Typography>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' }, gap: 2 }}>
          {divisions.map((division) => (
            <Card key={division.id} variant="outlined" sx={{ height: '100%' }}>
              <CardActionArea
                component={RouterLink}
                to={`${divisionsBase}/${division.id}/edit`}
                sx={{ height: '100%', display: 'block', textAlign: 'left' }}
              >
                <CardContent>
                  <Typography variant="h6" component="h3" gutterBottom>
                    {division.name}
                  </Typography>
                  {division.description ? (
                    <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                      {division.description}
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
