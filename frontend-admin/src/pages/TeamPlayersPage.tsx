import { useMemo, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import PersonAddIcon from '@mui/icons-material/PersonAdd'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'
import {
  PLAYER_POSITIONS,
  playersService,
  type Player,
  type PlayerPosition,
  type PlayerWriteBody,
} from '../api/players'
import { useLeagueId } from '../contexts/LeagueContext'
import { ImportPlayersCsvDialog } from '../components/ImportPlayersCsvDialog'

type FormState = {
  firstName: string
  lastName: string
  nickname: string
  document: string
  position: PlayerPosition | ''
}

const emptyForm: FormState = {
  firstName: '',
  lastName: '',
  nickname: '',
  document: '',
  position: '',
}

function positionLabel(value?: string | null) {
  return PLAYER_POSITIONS.find((p) => p.value === value)?.label ?? '—'
}

export function TeamPlayersPage() {
  const params = useParams<{ leagueId?: string; teamId?: string }>()
  const leagueId = useLeagueId()
  const teamId = params.teamId
  const queryClient = useQueryClient()
  const teamsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/teams` : '/teams'

  const [formOpen, setFormOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [editing, setEditing] = useState<Player | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: teams } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const team = teams?.find((t) => t.id === teamId)

  const { data: players, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'teams', teamId, 'players'],
    queryFn: ({ signal }) => playersService.listByTeam(leagueId!, teamId!, signal),
    enabled: !!leagueId && !!teamId,
  })

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!leagueId || !teamId) throw new Error('Falta liga o equipo')
      const body: PlayerWriteBody = {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        nickname: form.nickname.trim() || undefined,
        document: form.document.trim() || undefined,
        position: form.position || null,
      }
      if (!body.firstName || !body.lastName) throw new Error('Nombre y apellido son obligatorios')
      if (editing) {
        await playersService.update(leagueId, teamId, editing.id, { ...body, isActive: editing.isActive })
      } else {
        await playersService.create(leagueId, teamId, body)
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams', teamId, 'players'] })
      closeForm()
    },
    onError: (err) => setFormError(err instanceof Error ? err.message : 'No se pudo guardar'),
  })

  const deleteMutation = useMutation({
    mutationFn: (playerId: string) => playersService.remove(leagueId!, teamId!, playerId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams', teamId, 'players'] })
    },
  })

  const sortedPlayers = useMemo(() => {
    return [...(players ?? [])].sort((a, b) =>
      (a.displayName || a.lastName).localeCompare(b.displayName || b.lastName, 'es')
    )
  }, [players])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setFormError(null)
    setFormOpen(true)
  }

  const openEdit = (player: Player) => {
    setEditing(player)
    setForm({
      firstName: player.firstName,
      lastName: player.lastName,
      nickname: player.nickname || '',
      document: player.document || '',
      position: (player.position as PlayerPosition) || '',
    })
    setFormError(null)
    setFormOpen(true)
  }

  const closeForm = () => {
    setFormOpen(false)
    setEditing(null)
    setForm(emptyForm)
    setFormError(null)
    saveMutation.reset()
  }

  if (!leagueId || !teamId) {
    return <Alert severity="error">Falta la liga o el equipo.</Alert>
  }

  return (
    <Box>
      <Button component={RouterLink} to={`${teamsBase}/${teamId}/edit`} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver al equipo
      </Button>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} gap={2} sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
            Plantel
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {team?.displayName ?? team?.name ?? 'Equipo'}
          </Typography>
        </Box>
        <Stack direction="row" gap={1} flexWrap="wrap">
          <Button variant="outlined" startIcon={<UploadFileIcon />} onClick={() => setImportOpen(true)}>
            Importar CSV
          </Button>
          <Button variant="contained" startIcon={<PersonAddIcon />} onClick={openCreate}>
            Agregar integrante
          </Button>
        </Stack>
      </Stack>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {isError && (
        <Alert severity="error">{error instanceof Error ? error.message : 'No se pudo cargar el plantel'}</Alert>
      )}
      {!isLoading && !isError && sortedPlayers.length === 0 && (
        <Alert severity="info">Todavía no hay integrantes. Agregá uno o importá un CSV.</Alert>
      )}
      {!isLoading && sortedPlayers.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Nombre</TableCell>
              <TableCell>Apellido</TableCell>
              <TableCell>Apodo</TableCell>
              <TableCell>DNI</TableCell>
              <TableCell>Posición</TableCell>
              <TableCell align="right">Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedPlayers.map((player) => (
              <TableRow key={player.id} hover>
                <TableCell>{player.firstName}</TableCell>
                <TableCell>{player.lastName}</TableCell>
                <TableCell>{player.nickname || '—'}</TableCell>
                <TableCell>{player.document || '—'}</TableCell>
                <TableCell>{positionLabel(player.position)}</TableCell>
                <TableCell align="right">
                  <IconButton size="small" aria-label="Editar" onClick={() => openEdit(player)}>
                    <EditIcon fontSize="small" />
                  </IconButton>
                  <IconButton
                    size="small"
                    aria-label="Eliminar"
                    color="error"
                    disabled={deleteMutation.isPending}
                    onClick={() => {
                      if (window.confirm(`¿Eliminar a ${player.displayName}?`)) {
                        deleteMutation.mutate(player.id)
                      }
                    }}
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={formOpen} onClose={closeForm} maxWidth="sm" fullWidth>
        <DialogTitle>{editing ? 'Editar integrante' : 'Nuevo integrante'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField
              label="Nombre"
              required
              value={form.firstName}
              onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
              fullWidth
            />
            <TextField
              label="Apellido"
              required
              value={form.lastName}
              onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
              fullWidth
            />
            <TextField
              label="Apodo / nickname"
              value={form.nickname}
              onChange={(e) => setForm((f) => ({ ...f, nickname: e.target.value }))}
              fullWidth
            />
            <TextField
              label="DNI (opcional)"
              value={form.document}
              onChange={(e) => setForm((f) => ({ ...f, document: e.target.value }))}
              fullWidth
            />
            <FormControl fullWidth>
              <InputLabel id="player-position-label">Posición (opcional)</InputLabel>
              <Select
                labelId="player-position-label"
                label="Posición (opcional)"
                value={form.position}
                onChange={(e) => setForm((f) => ({ ...f, position: e.target.value as PlayerPosition | '' }))}
              >
                <MenuItem value="">Sin definir</MenuItem>
                {PLAYER_POSITIONS.map((p) => (
                  <MenuItem key={p.value} value={p.value}>{p.label}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeForm}>Cancelar</Button>
          <Button variant="contained" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
            {saveMutation.isPending ? <CircularProgress size={22} /> : 'Guardar'}
          </Button>
        </DialogActions>
      </Dialog>

      <ImportPlayersCsvDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        leagueId={leagueId}
        teamId={teamId}
      />
    </Box>
  )
}
