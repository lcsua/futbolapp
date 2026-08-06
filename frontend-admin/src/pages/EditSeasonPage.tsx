import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import LockIcon from '@mui/icons-material/Lock'
import LockOpenIcon from '@mui/icons-material/LockOpen'
import { Link as RouterLink } from 'react-router-dom'
import { SeasonForm } from '../components/SeasonForm'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { teamsService } from '../api/teams'
import { matchesService } from '../api/matches'
import type { SeasonFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

function countPendingResults(
  rounds: { matches: { status: string }[] }[] | undefined
): number {
  if (!rounds) return 0
  return rounds.reduce(
    (acc, g) =>
      acc +
      g.matches.filter(
        (m) => m.status !== 'COMPLETED' && m.status !== 'PLAYED'
      ).length,
    0
  )
}

export function EditSeasonPage() {
  const params = useParams<{ leagueId?: string; seasonId?: string }>()
  const leagueId = useLeagueId()
  const seasonId = params.seasonId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [closeDialogOpen, setCloseDialogOpen] = useState(false)
  const [pendingCount, setPendingCount] = useState<number | null>(null)
  const [pendingLoading, setPendingLoading] = useState(false)
  const seasonsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'

  const { data: seasons, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const season = seasons?.find((s) => s.id === seasonId)
  const isClosed = season != null && season.isActive === false

  const { data: divisions } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const assignDivisionMutation = useMutation({
    mutationFn: (divisionId: string) =>
      teamsService.assignDivisionToSeason(leagueId!, seasonId!, divisionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId] })
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: SeasonFormData) =>
      seasonsService.update(leagueId!, seasonId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      navigate(seasonsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update season')
    },
  })

  const closeMutation = useMutation({
    mutationFn: () => seasonsService.close(leagueId!, seasonId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      setCloseDialogOpen(false)
      setError(null)
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to close season')
      setCloseDialogOpen(false)
    },
  })

  const reopenMutation = useMutation({
    mutationFn: () => seasonsService.reopen(leagueId!, seasonId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      setError(null)
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to reopen season')
    },
  })

  const handleSubmit = (data: SeasonFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  const openCloseDialog = async () => {
    setPendingLoading(true)
    setPendingCount(null)
    setCloseDialogOpen(true)
    try {
      const data = await matchesService.getMatches(leagueId!, { seasonId: seasonId! })
      setPendingCount(countPendingResults(data.rounds))
    } catch {
      setPendingCount(null)
    } finally {
      setPendingLoading(false)
    }
  }

  if (!leagueId || !seasonId) {
    return <Alert severity="error">Missing league or season.</Alert>
  }

  if (isLoading || !seasons) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'Failed to load season'}
      </Alert>
    )
  }

  if (!season) {
    return <Alert severity="error">Season not found.</Alert>
  }

  const initialValues: SeasonFormData = {
    name: season.name,
    startDate: season.startDate,
    endDate: season.endDate ?? '',
  }

  return (
    <Box>
      <Button component={RouterLink} to={seasonsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to seasons
      </Button>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          Edit season
        </Typography>
        <Chip
          size="small"
          color={isClosed ? 'default' : 'success'}
          label={isClosed ? 'Closed' : 'Open'}
        />
      </Box>

      {isClosed && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          This season is closed. You cannot change setup, divisions, teams or fixtures.
          Match results can still be edited (e.g. after a disciplinary ruling).
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <SeasonForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={null}
        submitLabel="Save"
        title="Season details"
      />

      <Box sx={{ mt: 3, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {!isClosed ? (
          <Button
            variant="outlined"
            color="warning"
            startIcon={<LockIcon />}
            onClick={() => void openCloseDialog()}
            disabled={closeMutation.isPending}
          >
            Close season
          </Button>
        ) : (
          <Button
            variant="outlined"
            color="primary"
            startIcon={<LockOpenIcon />}
            onClick={() => reopenMutation.mutate()}
            disabled={reopenMutation.isPending}
          >
            {reopenMutation.isPending ? <CircularProgress size={22} /> : 'Reopen season'}
          </Button>
        )}
      </Box>

      {divisions && divisions.length > 0 && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
            Assign divisions to this season
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Assign league divisions to this season. Each division can be used in the season once assigned.
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {divisions.map((d) => (
              <Button
                key={d.id}
                size="small"
                variant="outlined"
                disabled={isClosed || assignDivisionMutation.isPending}
                onClick={() => assignDivisionMutation.mutate(d.id)}
              >
                Assign {d.name}
              </Button>
            ))}
          </Box>
        </Box>
      )}

      <Dialog open={closeDialogOpen} onClose={() => setCloseDialogOpen(false)}>
        <DialogTitle>Close season?</DialogTitle>
        <DialogContent>
          {pendingLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
              <CircularProgress size={28} />
            </Box>
          ) : (
            <DialogContentText component="div">
              <Typography variant="body1" sx={{ mb: 1 }}>
                Closing locks setup, divisions, team assignments and fixtures. Match results stay editable.
              </Typography>
              {pendingCount != null && pendingCount > 0 ? (
                <Alert severity="warning" sx={{ mt: 1 }}>
                  There {pendingCount === 1 ? 'is' : 'are'}{' '}
                  <strong>{pendingCount}</strong> match
                  {pendingCount === 1 ? '' : 'es'} without a final result. You can still close.
                </Alert>
              ) : pendingCount === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  All matches already have a final result.
                </Typography>
              ) : null}
            </DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCloseDialogOpen(false)} disabled={closeMutation.isPending}>
            Cancel
          </Button>
          <Button
            color="warning"
            variant="contained"
            onClick={() => closeMutation.mutate()}
            disabled={pendingLoading || closeMutation.isPending}
          >
            {closeMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Close anyway'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
