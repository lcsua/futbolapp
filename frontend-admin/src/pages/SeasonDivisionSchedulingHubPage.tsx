import { Link as RouterLink, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import TuneIcon from '@mui/icons-material/Tune'
import { useQuery } from '@tanstack/react-query'
import { divisionsService } from '../api/divisions'
import { seasonsService } from '../api/seasons'
import { leaguesService } from '../api/leagues'
import { useLeagueId } from '../contexts/LeagueContext'

/**
 * Lista las divisiones de la liga para una temporada concreta y enlaza al editor de reglas
 * (match + campos) por división-temporada.
 */
export function SeasonDivisionSchedulingHubPage() {
  const params = useParams<{ leagueId?: string; seasonId: string }>()
  const leagueIdFromContext = useLeagueId()
  const leagueId = params.leagueId ?? leagueIdFromContext
  const seasonId = params.seasonId

  const { data: league } = useQuery({
    queryKey: ['leagues', leagueId],
    queryFn: ({ signal }) => leaguesService.getById(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: seasons } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const season = seasons?.find((s) => s.id === seasonId)
  const { data: divisions, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId && !!seasonId,
  })

  const fromParams = !!params.leagueId
  const seasonsBase = fromParams && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'

  const schedulingPath = (divisionId: string) =>
    leagueId
      ? fromParams
        ? `/leagues/${leagueId}/seasons/${seasonId}/divisions/${divisionId}/division-scheduling-rules`
        : `/seasons/${seasonId}/divisions/${divisionId}/division-scheduling-rules`
      : '#'

  if (!leagueId || !seasonId) {
    return <Alert severity="error">Falta liga o temporada en la URL.</Alert>
  }

  if (!season && seasons) {
    return (
      <Alert severity="warning" action={<Button component={RouterLink} to={seasonsBase}>Ir a temporadas</Button>}>
        No se encontró esta temporada en la liga actual.
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
      <Alert severity="error">{error instanceof Error ? error.message : 'Error al cargar divisiones'}</Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to={seasonsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a temporadas
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 1, fontWeight: 600 }}>
        Reglas por división
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Temporada: <strong>{season?.name ?? seasonId}</strong>
        {league ? ` · Liga: ${league.name}` : null}. Aquí defines overrides sobre las{' '}
        <Button component={RouterLink} to={fromParams ? `/leagues/${leagueId}/match-rules` : '/match-rules'} size="small">
          reglas globales de partido
        </Button>{' '}
        y qué campos puede usar cada categoría en esta temporada.
      </Typography>

      {!divisions?.length ? (
        <Alert severity="info" sx={{ mb: 2 }}>
          No hay divisiones en esta liga. Crea divisiones en <Button component={RouterLink} to={fromParams ? `/leagues/${leagueId}/divisions` : '/divisions'} size="small">Divisiones</Button> y asigna equipos en Season setup.
        </Alert>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2 }}>
          {divisions.map((d) => (
            <Card key={d.id} variant="outlined">
              <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Typography variant="h6" component="h2">
                  {d.name}
                </Typography>
                <Button
                  component={RouterLink}
                  to={schedulingPath(d.id)}
                  variant="contained"
                  size="small"
                  startIcon={<TuneIcon />}
                  sx={{ alignSelf: 'flex-start' }}
                >
                  Editar reglas de esta división
                </Button>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}
    </Box>
  )
}
