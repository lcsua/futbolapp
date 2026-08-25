import { useState } from 'react'
import { useParams, Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
  CircularProgress,
  IconButton,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import AddIcon from '@mui/icons-material/Add'
import DeleteIcon from '@mui/icons-material/Delete'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  matchesService,
  incidentTypeLabel,
  type MatchDetailResponse,
  type MatchIncidentDto,
} from '../api/matches'
import { useLeagueId } from '../contexts/LeagueContext'
import { IncidentModal } from '../components/IncidentModal'

const INCIDENT_CHIP_COLOR: Record<string, 'success' | 'warning' | 'error' | 'default' | 'info'> = {
  Goal: 'success',
  YellowCard: 'warning',
  RedCard: 'error',
  Injury: 'info',
  Substitution: 'default',
  Other: 'default',
}

export function MatchDetailPage() {
  const { t } = useTranslation()
  const { matchId, leagueId: leagueIdInPath } = useParams<{ matchId: string; leagueId?: string }>()
  const leagueIdFromContext = useLeagueId()
  const leagueId = leagueIdInPath ?? leagueIdFromContext
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [incidentModalOpen, setIncidentModalOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const backPath = leagueIdInPath ? `/leagues/${leagueIdInPath}/matches` : '/matches'

  const { data: match, isLoading, error } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', matchId],
    queryFn: ({ signal }) => matchesService.getById(leagueId!, matchId!, signal),
    enabled: !!leagueId && !!matchId,
  })

  const deleteIncidentMutation = useMutation({
    mutationFn: (incidentId: string) =>
      matchesService.deleteIncident(leagueId!, incidentId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches', matchId] })
    },
  })

  const deleteMatchMutation = useMutation({
    mutationFn: () => matchesService.deleteMatch(leagueId!, matchId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
      navigate(backPath)
    },
  })

  if (!leagueId || !matchId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Ir a ligas</Button>}>
        Falta la liga o el partido.
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

  if (error || !match) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to={backPath}>Volver a partidos</Button>}>
        No se pudo cargar el partido.
      </Alert>
    )
  }

  const incidentsSorted = [...(match.incidents ?? [])].sort(
    (a, b) => (a.minute ?? Number.MAX_SAFE_INTEGER) - (b.minute ?? Number.MAX_SAFE_INTEGER)
  )

  return (
    <Box>
      <Button component={RouterLink} to={backPath} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a partidos
      </Button>

      <MatchHeader match={match} />

      {!match.seasonIsActive && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Esta temporada está cerrada. Igual podés editar el resultado o las incidencias si un fallo lo requiere.
        </Alert>
      )}

      <Typography variant="h6" sx={{ mt: 3, mb: 1 }}>
        Incidencias
      </Typography>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {incidentsSorted.length === 0 ? (
          <Typography color="text.secondary">No hay incidencias registradas.</Typography>
        ) : (
          incidentsSorted.map((inc) => (
            <IncidentRow
              key={inc.id}
              incident={inc}
              onDelete={() => deleteIncidentMutation.mutate(inc.id)}
              isDeleting={deleteIncidentMutation.isPending && deleteIncidentMutation.variables === inc.id}
            />
          ))
        )}
      </Box>

      <Button
        variant="outlined"
        startIcon={<AddIcon />}
        onClick={() => setIncidentModalOpen(true)}
        sx={{ mt: 2 }}
      >
        Agregar incidencia
      </Button>

      <Button
        variant="outlined"
        color="error"
        startIcon={<DeleteIcon />}
        disabled={!match.seasonIsActive || deleteMatchMutation.isPending}
        onClick={() => setDeleteDialogOpen(true)}
        sx={{ mt: 2, ml: 1 }}
      >
        {deleteMatchMutation.isPending ? t('matches.deleting') : t('matches.deleteMatch')}
      </Button>

      <Dialog
        open={deleteDialogOpen}
        onClose={() => !deleteMatchMutation.isPending && setDeleteDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>{t('matches.deleteConfirmTitle')}</DialogTitle>
        <DialogContent>
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('matches.deleteConfirmMatch', {
              home: match.homeTeamName,
              away: match.awayTeamName,
              round: match.roundNumber,
            })}
          </Alert>
          <Typography variant="body2" sx={{ mb: 1.5 }}>
            {t('matches.deleteConfirmFixture')}
          </Typography>
          <Typography variant="body2" sx={{ mb: 1.5 }}>
            {t('matches.deleteConfirmNoReadd')}
          </Typography>
          <Typography variant="body2">
            {t('matches.deleteConfirmWrongOption')}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleteMatchMutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            color="error"
            variant="contained"
            disabled={deleteMatchMutation.isPending}
            onClick={() => deleteMatchMutation.mutate()}
          >
            {deleteMatchMutation.isPending ? t('matches.deleting') : t('matches.deleteConfirmAction')}
          </Button>
        </DialogActions>
      </Dialog>

      {deleteMatchMutation.isError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {deleteMatchMutation.error instanceof Error
            ? deleteMatchMutation.error.message
            : 'No se pudo eliminar el partido'}
        </Alert>
      )}

      <IncidentModal
        open={incidentModalOpen}
        match={match}
        leagueId={leagueId}
        onClose={() => setIncidentModalOpen(false)}
        onSaved={() => {
          setIncidentModalOpen(false)
          void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches', matchId] })
        }}
      />
    </Box>
  )
}

function MatchHeader({ match }: { match: MatchDetailResponse }) {
  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1, minWidth: 0 }}>
            {match.homeTeamLogoUrl && (
              <Box component="img" src={match.homeTeamLogoUrl} alt="" sx={{ width: 32, height: 32, objectFit: 'contain' }} />
            )}
            <Typography variant="h6" component="span">
              {match.homeTeamName}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexShrink: 0 }}>
            <Typography variant="h5" component="span" sx={{ minWidth: 32, textAlign: 'center' }}>
              {match.homeScore ?? '-'}
            </Typography>
            <Typography color="text.secondary">—</Typography>
            <Typography variant="h5" component="span" sx={{ minWidth: 32, textAlign: 'center' }}>
              {match.awayScore ?? '-'}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1, minWidth: 0, justifyContent: 'flex-end' }}>
            <Typography variant="h6" component="span">
              {match.awayTeamName}
            </Typography>
            {match.awayTeamLogoUrl && (
              <Box component="img" src={match.awayTeamLogoUrl} alt="" sx={{ width: 32, height: 32, objectFit: 'contain' }} />
            )}
          </Box>
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          {match.fieldName} · {match.matchDate} · {match.kickoffTime}
        </Typography>
        <Typography variant="caption" color="text.secondary" display="block">
          Fecha {match.roundNumber} — {match.divisionName}
        </Typography>
      </CardContent>
    </Card>
  )
}

function IncidentRow({
  incident,
  onDelete,
  isDeleting,
}: {
  incident: MatchIncidentDto
  onDelete: () => void
  isDeleting: boolean
}) {
  const color = INCIDENT_CHIP_COLOR[incident.incidentType] ?? 'default'
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
      <Typography variant="body2" sx={{ minWidth: 40 }}>
        {incident.minute != null ? `${incident.minute}'` : '—'}
      </Typography>
      <Chip label={incidentTypeLabel(incident.incidentType)} color={color} size="small" />
      <Typography variant="body2">
        — {incident.teamName ?? 'Sin equipo'} — {incident.playerName || 'Sin jugador'}
      </Typography>
      {incident.notes && (
        <Typography variant="caption" color="text.secondary">
          {incident.notes}
        </Typography>
      )}
      <IconButton size="small" onClick={onDelete} disabled={isDeleting} aria-label="Eliminar incidencia">
        <DeleteIcon fontSize="small" />
      </IconButton>
    </Box>
  )
}
