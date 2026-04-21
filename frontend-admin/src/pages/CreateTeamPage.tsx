import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DomainAddIcon from '@mui/icons-material/DomainAdd'
import { Link as RouterLink } from 'react-router-dom'
import { TeamForm } from '../components/TeamForm'
import { CreateClubDialog } from '../components/CreateClubDialog'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function CreateTeamPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [clubDialogOpen, setClubDialogOpen] = useState(false)
  const teamsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const createMutation = useMutation({
    mutationFn: async (data: TeamFormData) => {
      const created = await teamsService.create(leagueId!, {
        name: data.name,
        suffix: data.suffix ?? undefined,
        clubId: data.clubId ?? undefined,
        shortName: data.shortName ?? undefined,
        email: data.email ?? undefined,
        seasonId: data.seasonId ?? undefined,
        divisionId: data.divisionId ?? undefined,
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

  const { data: clubs = [], isLoading: clubsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'clubs'],
    queryFn: ({ signal }) => teamsService.getClubsByLeague(leagueId!, signal),
    enabled: !!leagueId,
  })

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
      <Button
        variant="outlined"
        size="small"
        startIcon={<DomainAddIcon />}
        onClick={() => setClubDialogOpen(true)}
        sx={{ mb: 2 }}
      >
        Create club
      </Button>
      <TeamForm
        onSubmit={handleSubmit}
        clubs={clubs}
        clubsLoading={clubsLoading}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="Team details"
      />
      <CreateClubDialog open={clubDialogOpen} leagueId={leagueId} onClose={() => setClubDialogOpen(false)} />
    </Box>
  )
}
