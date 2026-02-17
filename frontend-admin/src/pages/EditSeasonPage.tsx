import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { SeasonForm } from '../components/SeasonForm'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { teamsService } from '../api/teams'
import type { SeasonFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditSeasonPage() {
  const params = useParams<{ leagueId?: string; seasonId?: string }>()
  const leagueId = useLeagueId()
  const seasonId = params.seasonId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const seasonsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'

  const { data: seasons, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const season = seasons?.find((s) => s.id === seasonId)

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

  const handleSubmit = (data: SeasonFormData) => {
    setError(null)
    updateMutation.mutate(data)
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
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Edit season
      </Typography>
      <SeasonForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Save"
        title="Season details"
      />
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
                disabled={assignDivisionMutation.isPending}
                onClick={() => assignDivisionMutation.mutate(d.id)}
              >
                Assign {d.name}
              </Button>
            ))}
          </Box>
        </Box>
      )}
    </Box>
  )
}
