import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { TeamForm } from '../components/TeamForm'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditTeamPage() {
  const params = useParams<{ leagueId?: string; teamId?: string }>()
  const leagueId = useLeagueId()
  const teamId = params.teamId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const teamsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const { data: teams, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const team = teams?.find((t) => t.id === teamId)

  const updateMutation = useMutation({
    mutationFn: (data: TeamFormData) =>
      teamsService.update(leagueId!, teamId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      navigate(teamsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update team')
    },
  })

  const handleSubmit = (data: TeamFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  if (!leagueId || !teamId) {
    return <Alert severity="error">Missing league or team.</Alert>
  }

  if (isLoading || teams === undefined) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'Failed to load team'}
      </Alert>
    )
  }

  if (!team) {
    return <Alert severity="error">Team not found.</Alert>
  }

  const initialValues: TeamFormData = {
    name: team.name,
    shortName: team.shortName ?? '',
    primaryColor: '',
    secondaryColor: '',
    foundedYear: team.foundedYear ?? undefined,
    delegateName: team.delegateName ?? '',
    delegateContact: team.delegateContact ?? '',
    email: team.email ?? '',
    logoUrl: team.logoUrl ?? '',
    photoUrl: team.photoUrl ?? '',
  }

  return (
    <Box>
      <Button component={RouterLink} to={teamsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to teams
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Edit team
      </Typography>
      <TeamForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Save"
        title="Team details"
      />
    </Box>
  )
}
