import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
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
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useQuery } from '@tanstack/react-query'
import { seasonsService } from '../api/seasons'
import { leaguesService } from '../api/leagues'
import { useLeagueId, useActiveLeague } from '../contexts/LeagueContext'
import { useTranslation } from 'react-i18next'

export function SeasonsListPage() {
  const { t } = useTranslation()
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
  const { data: seasons, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>{t('seasons.goToLeagues')}</Button>}>
        {t('seasons.noLeague')}
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
        {error instanceof Error ? error.message : t('seasons.loadError')}
      </Alert>
    )
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Button
          component={RouterLink}
          to="/"
          startIcon={<ArrowBackIcon />}
          size="small"
        >
          {t('nav.leagues')}
        </Button>
      </Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Typography variant="h5" component="h2" fontWeight={600}>
          {league?.name ?? activeLeague?.name ?? ''} — {t('nav.seasons')}
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button component={RouterLink} to={divisionsBase} variant="outlined">
            {t('nav.divisions')}
          </Button>
          <Button component={RouterLink} to={`${seasonsBase}/new`} variant="contained" startIcon={<AddIcon />}>
            {t('seasons.create')}
          </Button>
        </Box>
      </Box>

      {!seasons?.length ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          {t('seasons.empty')}
        </Typography>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' }, gap: 2 }}>
          {seasons.map((season) => (
            <Card key={season.id} variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Typography variant="h6" component="h3" gutterBottom>
                  {season.name}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  {season.startDate}
                  {season.endDate ? ` — ${season.endDate}` : ''}
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, alignItems: 'flex-start' }}>
                  <Button component={RouterLink} to={`${seasonsBase}/${season.id}/edit`} size="small" variant="outlined">
                    {t('seasons.edit')}
                  </Button>
                  <Button
                    component={RouterLink}
                    to={`${seasonsBase}/${season.id}/division-scheduling`}
                    size="small"
                    variant="contained"
                    color="secondary"
                  >
                    {t('seasons.schedulingRules')}
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
