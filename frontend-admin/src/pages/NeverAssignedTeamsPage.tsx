import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'
import { useLeagueId } from '../contexts/LeagueContext'
import {
  DeleteTeamsConfirmationBody,
  getDeleteTeamsConfirmExpected,
  getTeamDisplayName,
} from '../components/DeleteTeamsConfirmation'

export function NeverAssignedTeamsPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const fromParams = !!params.leagueId
  const teamsBase = fromParams && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const [searchTerm, setSearchTerm] = useState('')
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [confirmStep, setConfirmStep] = useState(false)
  const [confirmText, setConfirmText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const { data: teams = [], isLoading, isError, error: loadError } = useQuery({
    queryKey: ['leagues', leagueId, 'teams', 'never-assigned'],
    queryFn: ({ signal }) => teamsService.getNeverAssigned(leagueId!, signal),
    enabled: !!leagueId,
  })

  const filtered = useMemo(() => {
    const term = searchTerm.trim().toLowerCase()
    const list = !term
      ? teams
      : teams.filter(
          (t) =>
            getTeamDisplayName(t).toLowerCase().includes(term) ||
            (t.clubName ?? '').toLowerCase().includes(term)
        )
    return [...list].sort((a, b) => {
      const base = a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
      if (base !== 0) return base
      return (a.suffix ?? '').localeCompare(b.suffix ?? '', undefined, { sensitivity: 'base' })
    })
  }, [teams, searchTerm])

  const selectedTeams = useMemo(
    () => teams.filter((t) => selectedIds.includes(t.id)),
    [teams, selectedIds]
  )

  const allFilteredSelected =
    filtered.length > 0 && filtered.every((t) => selectedIds.includes(t.id))

  const expectedConfirm = getDeleteTeamsConfirmExpected(selectedTeams)
  const canConfirmDelete =
    selectedTeams.length > 0 && confirmText.trim() === expectedConfirm

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!leagueId || selectedIds.length === 0) return { deletedCount: 0 }
      return teamsService.deleteNeverAssigned(leagueId, selectedIds)
    },
    onSuccess: (res) => {
      setConfirmStep(false)
      setConfirmText('')
      setSelectedIds([])
      setSuccess(`${res.deletedCount} equipo(s) eliminado(s).`)
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'No se pudieron eliminar los equipos')
    },
  })

  const openConfirm = (ids: string[]) => {
    if (ids.length === 0) return
    setSelectedIds(ids)
    setConfirmText('')
    setError(null)
    setSuccess(null)
    setConfirmStep(true)
  }

  const toggleOne = (id: string) => {
    setSelectedIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]))
  }

  const toggleAllFiltered = () => {
    if (allFilteredSelected) {
      const filteredSet = new Set(filtered.map((t) => t.id))
      setSelectedIds((prev) => prev.filter((id) => !filteredSet.has(id)))
    } else {
      setSelectedIds((prev) => [...new Set([...prev, ...filtered.map((t) => t.id)])])
    }
  }

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>Go to Leagues</Button>}>
        No league selected.
      </Alert>
    )
  }

  if (confirmStep) {
    return (
      <Box>
        <Button
          size="small"
          startIcon={<ArrowBackIcon />}
          sx={{ mb: 2 }}
          disabled={deleteMutation.isPending}
          onClick={() => {
            setConfirmStep(false)
            setConfirmText('')
          }}
        >
          Volver a la selección
        </Button>
        <Typography variant="h5" component="h1" fontWeight={600} sx={{ mb: 1 }}>
          {selectedTeams.length === 1
            ? 'Eliminar equipo de forma permanente'
            : `Eliminar ${selectedTeams.length} equipos de forma permanente`}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Revisá la lista y confirmá escribiendo el texto pedido. Hasta entonces no se borra nada.
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {selectedTeams.length === 0 ? (
          <Alert severity="warning">No quedó ningún equipo seleccionado.</Alert>
        ) : (
          <Box sx={{ maxWidth: 720 }}>
            <DeleteTeamsConfirmationBody
              teams={selectedTeams}
              confirmText={confirmText}
              onConfirmTextChange={setConfirmText}
              disabled={deleteMutation.isPending}
            />
            <Box sx={{ display: 'flex', gap: 1, mt: 3 }}>
              <Button
                onClick={() => {
                  setConfirmStep(false)
                  setConfirmText('')
                }}
                disabled={deleteMutation.isPending}
              >
                Cancelar
              </Button>
              <Button
                color="error"
                variant="contained"
                onClick={() => deleteMutation.mutate()}
                disabled={!canConfirmDelete || deleteMutation.isPending}
              >
                {deleteMutation.isPending ? (
                  <CircularProgress size={22} color="inherit" />
                ) : (
                  'Eliminar definitivamente'
                )}
              </Button>
            </Box>
          </Box>
        )}
      </Box>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to={teamsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a equipos
      </Button>
      <Typography variant="h5" component="h1" fontWeight={600} sx={{ mb: 1 }}>
        Eliminar equipos no asignados
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Solo aparecen equipos de la liga que nunca estuvieron en una temporada, división ni fixture.
        Podés borrar varios a la vez o uno por uno. Después hay una pantalla de confirmación.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}
      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {loadError instanceof Error ? loadError.message : 'Failed to load'}
        </Alert>
      )}

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'center', mb: 2 }}>
        <TextField
          size="small"
          label="Buscar"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          sx={{ minWidth: 240, flex: 1 }}
        />
        <Button
          variant="outlined"
          onClick={toggleAllFiltered}
          disabled={filtered.length === 0 || deleteMutation.isPending}
        >
          {allFilteredSelected ? 'Deseleccionar filtrados' : 'Seleccionar filtrados'}
        </Button>
        <Button
          variant="contained"
          color="error"
          startIcon={<DeleteOutlineIcon />}
          disabled={selectedIds.length === 0 || deleteMutation.isPending}
          onClick={() => openConfirm(selectedIds)}
        >
          Borrar seleccionados ({selectedIds.length})
        </Button>
      </Box>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {filtered.length} de {teams.length} equipo(s)
      </Typography>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : teams.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 3 }}>
          No hay equipos sin historial de asignación. Todo limpio.
        </Typography>
      ) : filtered.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 3 }}>
          Ningún equipo coincide con la búsqueda.
        </Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox">
                <Checkbox
                  checked={allFilteredSelected}
                  indeterminate={selectedIds.length > 0 && !allFilteredSelected}
                  onChange={toggleAllFiltered}
                />
              </TableCell>
              <TableCell>Equipo</TableCell>
              <TableCell>Club</TableCell>
              <TableCell align="right">Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filtered.map((team) => (
              <TableRow key={team.id} hover selected={selectedIds.includes(team.id)}>
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={selectedIds.includes(team.id)}
                    onChange={() => toggleOne(team.id)}
                  />
                </TableCell>
                <TableCell>{getTeamDisplayName(team)}</TableCell>
                <TableCell>{team.clubName || '—'}</TableCell>
                <TableCell align="right">
                  <Tooltip title="Eliminar este equipo">
                    <IconButton
                      size="small"
                      color="error"
                      aria-label={`Eliminar ${getTeamDisplayName(team)}`}
                      onClick={() => openConfirm([team.id])}
                    >
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  )
}
