import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import GroupsIcon from '@mui/icons-material/Groups'
import DomainAddIcon from '@mui/icons-material/DomainAdd'
import DeleteForeverIcon from '@mui/icons-material/DeleteForever'
import { Link as RouterLink } from 'react-router-dom'
import { TeamForm } from '../components/TeamForm'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'
import { CreateClubDialog } from '../components/CreateClubDialog'
import { ConfirmDeleteTeamsDialog } from '../components/DeleteTeamsConfirmation'

export function EditTeamPage() {
  const params = useParams<{ leagueId?: string; teamId?: string }>()
  const leagueId = useLeagueId()
  const teamId = params.teamId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [clubDialogOpen, setClubDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
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
  const { data: neverAssignedTeams, isLoading: neverAssignedLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'teams', 'never-assigned'],
    queryFn: ({ signal }) => teamsService.getNeverAssigned(leagueId!, signal),
    enabled: !!leagueId,
  })
  const team = teams?.find((t) => t.id === teamId)
  const canDelete = !!team && (neverAssignedTeams?.some((t) => t.id === team.id) ?? false)

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

  const deleteMutation = useMutation({
    mutationFn: () => teamsService.deleteOne(leagueId!, teamId!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      navigate(teamsBase, { replace: true })
    },
    onError: (err) => {
      setDeleteDialogOpen(false)
      setError(err instanceof Error ? err.message : 'No se pudo eliminar el equipo')
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
        leagueId={leagueId}
        initialValues={initialValues}
        clubs={clubs}
        clubsLoading={clubsLoading}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Guardar"
        title="Datos del equipo"
      />
      <Box sx={{ mt: 3 }}>
        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteForeverIcon />}
          onClick={() => {
            setError(null)
            setDeleteDialogOpen(true)
          }}
          disabled={!canDelete || deleteMutation.isPending || updateMutation.isPending || neverAssignedLoading}
        >
          Eliminar equipo
        </Button>
        {!neverAssignedLoading && !canDelete && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Este equipo no se puede eliminar porque ya estuvo asignado a una temporada o división.
          </Typography>
        )}
      </Box>
      <CreateClubDialog open={clubDialogOpen} leagueId={leagueId} onClose={() => setClubDialogOpen(false)} />
      <ConfirmDeleteTeamsDialog
        open={deleteDialogOpen}
        teams={team ? [team] : []}
        loading={deleteMutation.isPending}
        onClose={() => !deleteMutation.isPending && setDeleteDialogOpen(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </Box>
  )
}
