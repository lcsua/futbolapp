import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'
import { useLeagueId } from '../contexts/LeagueContext'
import type { Team } from '../api/types'

function getTeamDisplayName(team: Team) {
  return team.displayName ?? team.name
}

export function NeverAssignedTeamsPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const fromParams = !!params.leagueId
  const teamsBase = fromParams && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const [searchTerm, setSearchTerm] = useState('')
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [confirmOpen, setConfirmOpen] = useState(false)
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

  const allFilteredSelected =
    filtered.length > 0 && filtered.every((t) => selectedIds.includes(t.id))

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!leagueId || selectedIds.length === 0) return { deletedCount: 0 }
      return teamsService.deleteNeverAssigned(leagueId, selectedIds)
    },
    onSuccess: (res) => {
      setConfirmOpen(false)
      setSelectedIds([])
      setSuccess(`${res.deletedCount} equipo(s) eliminado(s).`)
      setError(null)
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
    },
    onError: (err) => {
      setConfirmOpen(false)
      setError(err instanceof Error ? err.message : 'No se pudieron eliminar los equipos')
    },
  })

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

  return (
    <Box>
      <Button component={RouterLink} to={teamsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a equipos
      </Button>
      <Typography variant="h5" component="h1" fontWeight={600} sx={{ mb: 1 }}>
        Equipos nunca asignados
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Equipos de la liga que no estuvieron en ninguna temporada. Podés seleccionarlos y borrarlos.
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
          startIcon={
            deleteMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <DeleteOutlineIcon />
          }
          disabled={selectedIds.length === 0 || deleteMutation.isPending}
          onClick={() => setConfirmOpen(true)}
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
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={confirmOpen} onClose={() => !deleteMutation.isPending && setConfirmOpen(false)}>
        <DialogTitle>¿Borrar {selectedIds.length} equipo(s)?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            Esta acción es permanente. Solo se permiten equipos que nunca se asignaron a una temporada.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmOpen(false)} disabled={deleteMutation.isPending}>
            Cancelar
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() => deleteMutation.mutate()}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'Borrar'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
