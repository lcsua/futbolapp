import { useState } from 'react'
import { useParams, Link as RouterLink } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Typography,
  CircularProgress,
  IconButton,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import AddIcon from '@mui/icons-material/Add'
import DeleteIcon from '@mui/icons-material/Delete'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { matchesService, type MatchDetailResponse, type MatchIncidentDto } from '../api/matches'
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
  const { matchId, leagueId: leagueIdInPath } = useParams<{ matchId: string; leagueId?: string }>()
  const leagueIdFromContext = useLeagueId()
  const leagueId = leagueIdInPath ?? leagueIdFromContext
  const queryClient = useQueryClient()
  const [incidentModalOpen, setIncidentModalOpen] = useState(false)

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

  const backPath = leagueIdInPath ? `/leagues/${leagueIdInPath}/matches` : '/matches'

  if (!leagueId || !matchId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        Missing league or match.
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
      <Alert severity="error" action={<Button component={RouterLink} to={backPath}>Back to matches</Button>}>
        Failed to load match.
      </Alert>
    )
  }

  const incidentsSorted = [...(match.incidents ?? [])].sort((a, b) => a.minute - b.minute)

  return (
    <Box>
      <Button component={RouterLink} to={backPath} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to matches
      </Button>

      <MatchHeader match={match} />

      <Typography variant="h6" sx={{ mt: 3, mb: 1 }}>
        Incident timeline
      </Typography>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {incidentsSorted.length === 0 ? (
          <Typography color="text.secondary">No incidents recorded.</Typography>
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
        Add incident
      </Button>

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
          Round {match.roundNumber} — {match.divisionName}
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
      <Typography variant="body2" sx={{ minWidth: 32 }}>
        {incident.minute}'
      </Typography>
      <Chip label={incident.incidentType} color={color} size="small" />
      <Typography variant="body2">
        — {incident.teamName ?? 'N/A'} — {incident.playerName}
      </Typography>
      {incident.notes && (
        <Typography variant="caption" color="text.secondary">
          {incident.notes}
        </Typography>
      )}
      <IconButton size="small" onClick={onDelete} disabled={isDeleting} aria-label="Delete incident">
        <DeleteIcon fontSize="small" />
      </IconButton>
    </Box>
  )
}
