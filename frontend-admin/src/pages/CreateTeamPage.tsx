import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { TeamForm } from '../components/TeamForm'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function CreateTeamPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const teamsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const createMutation = useMutation({
    mutationFn: async (data: TeamFormData) => {
      const created = await teamsService.create(leagueId!, {
        name: data.name,
        shortName: data.shortName ?? undefined,
        email: data.email ?? undefined,
      })
      const hasDetails =
        data.primaryColor ||
        data.secondaryColor ||
        data.logoUrl ||
        data.photoUrl ||
        data.foundedYear != null ||
        data.delegateName ||
        data.delegateContact
      if (hasDetails) {
        await teamsService.update(leagueId!, created.id, data)
      }
      return created.id
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      navigate(teamsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create team')
    },
  })

  const handleSubmit = (data: TeamFormData) => {
    setError(null)
    createMutation.mutate(data)
  }

  if (!leagueId) {
    return <Alert severity="error">Missing league.</Alert>
  }

  return (
    <Box>
      <Button component={RouterLink} to={teamsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to teams
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create team
      </Typography>
      <TeamForm
        onSubmit={handleSubmit}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="Team details"
      />
    </Box>
  )
}
