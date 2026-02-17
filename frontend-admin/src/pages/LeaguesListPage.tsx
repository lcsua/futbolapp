import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { useQuery } from '@tanstack/react-query'
import { leaguesService } from '../api/leagues'
import { useLeagueContext } from '../contexts/LeagueContext'

export function LeaguesListPage() {
  const navigate = useNavigate()
  const { setActiveLeague } = useLeagueContext()
  const { data: leagues, isLoading, isError, error } = useQuery({
    queryKey: ['leagues'],
    queryFn: ({ signal }) => leaguesService.getMyLeagues(signal),
  })

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        {error instanceof Error ? error.message : 'Failed to load leagues'}
      </Alert>
    )
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Typography variant="h5" component="h2" fontWeight={600}>
          Leagues
        </Typography>
        <Button
          component={RouterLink}
          to="/leagues/new"
          variant="contained"
          startIcon={<AddIcon />}
        >
          Create league
        </Button>
      </Box>

      {!leagues?.length ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No leagues yet. Create one to get started.
        </Typography>
      ) : (
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' },
          gap: 2,
        }}
      >
        {leagues.map((league) => (
          <Card key={league.id} variant="outlined" sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" component="h3" gutterBottom>
                {league.name}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {league.country}
              </Typography>
              {league.description ? (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mt: 1, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
                >
                  {league.description}
                </Typography>
              ) : null}
              <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                <Button component={RouterLink} to={`/leagues/${league.id}/edit`} size="small" variant="outlined">
                  Edit
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => {
                    setActiveLeague(league)
                    navigate('/seasons')
                  }}
                >
                  Seasons
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => {
                    setActiveLeague(league)
                    navigate('/divisions')
                  }}
                >
                  Divisions
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => {
                    setActiveLeague(league)
                    navigate('/teams')
                  }}
                >
                  Teams
                </Button>
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>
      )}
    </Box>
  )
}
