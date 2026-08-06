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
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DeleteForeverIcon from '@mui/icons-material/DeleteForever'
import LockIcon from '@mui/icons-material/Lock'
import LockOpenIcon from '@mui/icons-material/LockOpen'
import { Link as RouterLink } from 'react-router-dom'
import { SeasonForm } from '../components/SeasonForm'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { teamsService } from '../api/teams'
import { matchesService } from '../api/matches'
import type { Season, SeasonFormData } from '../api/types'
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
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [deleteConfirmName, setDeleteConfirmName] = useState('')
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
    onSuccess: async (_void, data) => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      queryClient.setQueryData<Season[]>(
        ['leagues', leagueId, 'seasons'],
        (prev) =>
          prev?.map((s) =>
            s.id === seasonId
              ? {
                  ...s,
                  name: data.name,
                  startDate: data.startDate,
                  endDate: data.endDate?.trim() ? data.endDate : null,
                  isPublic: !!data.isPublic,
                }
              : s
          ) ?? prev
      )
      navigate(seasonsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'No se pudo actualizar la temporada')
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
      setError(err instanceof Error ? err.message : 'No se pudo cerrar la temporada')
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
      setError(err instanceof Error ? err.message : 'No se pudo reabrir la temporada')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => seasonsService.delete(leagueId!, seasonId!),
    onSuccess: async () => {
      setDeleteDialogOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      queryClient.setQueryData<Season[]>(
        ['leagues', leagueId, 'seasons'],
        (prev) => prev?.filter((s) => s.id !== seasonId) ?? prev
      )
      navigate(seasonsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'No se pudo eliminar la temporada')
      setDeleteDialogOpen(false)
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
    return <Alert severity="error">Falta la liga o la temporada.</Alert>
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
        {queryError instanceof Error ? queryError.message : 'No se pudo cargar la temporada'}
      </Alert>
    )
  }

  if (!season) {
    return <Alert severity="error">Temporada no encontrada.</Alert>
  }

  const initialValues: SeasonFormData = {
    name: season.name,
    startDate: season.startDate,
    endDate: season.endDate ?? '',
    isPublic: season.isPublic === true,
  }

  const canConfirmDelete =
    !!season && deleteConfirmName.trim().localeCompare(season.name.trim(), undefined, { sensitivity: 'accent' }) === 0

  return (
    <Box>
      <Button component={RouterLink} to={seasonsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a temporadas
      </Button>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          Editar temporada
        </Typography>
        <Chip
          size="small"
          color={isClosed ? 'default' : 'success'}
          label={isClosed ? 'Cerrada' : 'Abierta'}
        />
        <Chip
          size="small"
          color={season.isPublic ? 'info' : 'default'}
          variant={season.isPublic ? 'filled' : 'outlined'}
          label={season.isPublic ? 'Visible en web pública' : 'Oculta en web pública'}
        />
      </Box>

      {isClosed && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          Esta temporada está cerrada. No se puede cambiar el setup, divisiones, equipos ni fixtures.
          Los resultados de partidos se pueden seguir editando (por ejemplo por una resolución).
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
        submitLabel="Guardar"
        title="Datos de la temporada"
      />

      <Box sx={{ mt: 3, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {!isClosed ? (
          <Button
            variant="outlined"
            color="warning"
            startIcon={<LockIcon />}
            onClick={() => void openCloseDialog()}
            disabled={closeMutation.isPending || deleteMutation.isPending}
          >
            Cerrar temporada
          </Button>
        ) : (
          <Button
            variant="outlined"
            color="primary"
            startIcon={<LockOpenIcon />}
            onClick={() => reopenMutation.mutate()}
            disabled={reopenMutation.isPending || deleteMutation.isPending}
          >
            {reopenMutation.isPending ? <CircularProgress size={22} /> : 'Reabrir temporada'}
          </Button>
        )}
        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteForeverIcon />}
          onClick={() => {
            setDeleteConfirmName('')
            setDeleteDialogOpen(true)
          }}
          disabled={deleteMutation.isPending}
        >
          Eliminar temporada
        </Button>
      </Box>

      {divisions && divisions.length > 0 && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
            Asignar divisiones a esta temporada
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Asigná divisiones de la liga a esta temporada. Cada división se puede usar una vez asignada.
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
                Asignar {d.name}
              </Button>
            ))}
          </Box>
        </Box>
      )}

      <Dialog open={closeDialogOpen} onClose={() => setCloseDialogOpen(false)}>
        <DialogTitle>¿Cerrar temporada?</DialogTitle>
        <DialogContent>
          {pendingLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
              <CircularProgress size={28} />
            </Box>
          ) : (
            <DialogContentText component="div">
              <Typography variant="body1" sx={{ mb: 1 }}>
                Al cerrarla se bloquean el setup, divisiones, asignaciones de equipos y fixtures.
                Los resultados de partidos siguen siendo editables.
              </Typography>
              {pendingCount != null && pendingCount > 0 ? (
                <Alert severity="warning" sx={{ mt: 1 }}>
                  Hay <strong>{pendingCount}</strong> partido
                  {pendingCount === 1 ? '' : 's'} sin resultado final. Igual podés cerrar.
                </Alert>
              ) : pendingCount === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  Todos los partidos ya tienen resultado final.
                </Typography>
              ) : null}
            </DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCloseDialogOpen(false)} disabled={closeMutation.isPending}>
            Cancelar
          </Button>
          <Button
            color="warning"
            variant="contained"
            onClick={() => closeMutation.mutate()}
            disabled={pendingLoading || closeMutation.isPending}
          >
            {closeMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Cerrar de todos modos'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={deleteDialogOpen}
        onClose={() => !deleteMutation.isPending && setDeleteDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Eliminar temporada de forma permanente</DialogTitle>
        <DialogContent>
          <Alert severity="error" sx={{ mb: 2 }}>
            Esta acción <strong>no se puede deshacer</strong>. Se borrará por completo la temporada
            &quot;{season.name}&quot; y todo lo que depende de ella: divisiones asignadas, equipos,
            fixtures, resultados, incidentes y reglas propias de la temporada.
          </Alert>
          <DialogContentText sx={{ mb: 2 }}>
            Para confirmar, escribí el nombre exacto de la temporada:
          </DialogContentText>
          <TextField
            autoFocus
            fullWidth
            label="Nombre de la temporada"
            placeholder={season.name}
            value={deleteConfirmName}
            onChange={(e) => setDeleteConfirmName(e.target.value)}
            disabled={deleteMutation.isPending}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleteMutation.isPending}>
            Cancelar
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() => deleteMutation.mutate()}
            disabled={!canConfirmDelete || deleteMutation.isPending}
          >
            {deleteMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Eliminar definitivamente'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
