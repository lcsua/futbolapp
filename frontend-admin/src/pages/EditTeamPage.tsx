import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import GroupsIcon from '@mui/icons-material/Groups'
import DomainAddIcon from '@mui/icons-material/DomainAdd'
import { Link as RouterLink } from 'react-router-dom'
import { TeamForm } from '../components/TeamForm'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'
import { CreateClubDialog } from '../components/CreateClubDialog'

export function EditTeamPage() {
  const params = useParams<{ leagueId?: string; teamId?: string }>()
  const leagueId = useLeagueId()
  const teamId = params.teamId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [clubDialogOpen, setClubDialogOpen] = useState(false)
  const teamsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const { data: teams, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: clubs = [], isLoading: clubsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'clubs'],
    queryFn: ({ signal }) => teamsService.getClubsByLeague(leagueId!, signal),
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
    suffix: team.suffix ?? '',
    clubId: team.clubId ?? undefined,
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
        Volver a equipos
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Editar equipo
      </Typography>
      <Stack direction="row" gap={1} flexWrap="wrap" sx={{ mb: 2 }}>
        <Button variant="outlined" size="small" startIcon={<GroupsIcon />} component={RouterLink} to={`${teamsBase}/${teamId}/players`}>
          Integrantes / plantel
        </Button>
        <Button
          variant="outlined"
          size="small"
          startIcon={<DomainAddIcon />}
          onClick={() => setClubDialogOpen(true)}
        >
          Crear club
        </Button>
      </Stack>
      <TeamForm
        initialValues={initialValues}
        clubs={clubs}
        clubsLoading={clubsLoading}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Guardar"
        title="Datos del equipo"
      />
      <CreateClubDialog open={clubDialogOpen} leagueId={leagueId} onClose={() => setClubDialogOpen(false)} />
    </Box>
  )
}
