import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
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
import { Link as RouterLink } from 'react-router-dom'
import { DivisionForm } from '../components/DivisionForm'
import { divisionsService } from '../api/divisions'
import type { Division, DivisionFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditDivisionPage() {
  const params = useParams<{ leagueId?: string; divisionId?: string }>()
  const leagueId = useLeagueId()
  const divisionId = params.divisionId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [deleteConfirmName, setDeleteConfirmName] = useState('')
  const divisionsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/divisions` : '/divisions'

  const { data: divisions, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const division = divisions?.find((d) => d.id === divisionId)

  const updateMutation = useMutation({
    mutationFn: (data: DivisionFormData) =>
      divisionsService.update(leagueId!, divisionId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'divisions'] })
      navigate(divisionsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'No se pudo actualizar la división')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => divisionsService.delete(leagueId!, divisionId!),
    onSuccess: async () => {
      setDeleteDialogOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'divisions'] })
      queryClient.setQueryData<Division[]>(
        ['leagues', leagueId, 'divisions'],
        (prev) => prev?.filter((d) => d.id !== divisionId) ?? prev
      )
      navigate(divisionsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'No se pudo eliminar la división')
      setDeleteDialogOpen(false)
    },
  })

  const handleSubmit = (data: DivisionFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  if (!leagueId || !divisionId) {
    return <Alert severity="error">Falta la liga o la división.</Alert>
  }

  if (isLoading || divisions === undefined) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'No se pudo cargar la división'}
      </Alert>
    )
  }

  if (!division) {
    return <Alert severity="error">División no encontrada.</Alert>
  }

  const initialValues: DivisionFormData = {
    name: division.name,
    description: division.description ?? '',
    kickoffRestrictionEnabled: division.kickoffRestrictionEnabled ?? false,
    kickoffRestrictionStart: division.kickoffRestrictionStart ?? null,
    kickoffRestrictionEnd: division.kickoffRestrictionEnd ?? null,
  }

  const canConfirmDelete =
    deleteConfirmName.trim().localeCompare(division.name.trim(), undefined, { sensitivity: 'accent' }) === 0

  return (
    <Box>
      <Button component={RouterLink} to={divisionsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Volver a divisiones
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Editar división
      </Typography>
      <Alert severity="info" sx={{ mb: 3 }}>
        Las reglas de partido por categoría (duración, horarios, campos) se configuran <strong>por temporada</strong>: ve a{' '}
        <Button component={RouterLink} to="/seasons" size="small">
          Temporadas
        </Button>{' '}
        → elegí una temporada → <strong>Reglas por división</strong>.
      </Alert>
      <DivisionForm
        key={`${division.id}-${division.kickoffRestrictionEnabled}-${division.kickoffRestrictionStart ?? ''}-${division.kickoffRestrictionEnd ?? ''}`}
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Guardar"
        title="Datos de la división"
      />

      <Box sx={{ mt: 3 }}>
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
          Eliminar división
        </Button>
      </Box>

      <Dialog
        open={deleteDialogOpen}
        onClose={() => !deleteMutation.isPending && setDeleteDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Eliminar división de forma permanente</DialogTitle>
        <DialogContent>
          <Alert severity="error" sx={{ mb: 2 }}>
            Esta acción <strong>no se puede deshacer</strong>. Se borrará por completo la división
            &quot;{division.name}&quot; y todo lo que depende de ella en <strong>todas las temporadas</strong>:
            asignaciones de equipos, fixtures, resultados, incidentes y reglas de scheduling de esa categoría.
          </Alert>
          <DialogContentText sx={{ mb: 2 }}>
            Para confirmar, escribí el nombre exacto de la división:
          </DialogContentText>
          <TextField
            autoFocus
            fullWidth
            label="Nombre de la división"
            placeholder={division.name}
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
