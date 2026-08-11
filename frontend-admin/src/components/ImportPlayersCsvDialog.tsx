import { useMemo, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
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
  Typography,
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { PLAYER_POSITIONS, playersService, type ImportPlayerItem } from '../api/players'
import { parsePlayersFromCsv } from '../utils/parsePlayersCsv'

type Props = {
  open: boolean
  onClose: () => void
  leagueId: string
  teamId: string
}

function positionLabel(value?: string) {
  return PLAYER_POSITIONS.find((p) => p.value === value)?.label ?? value ?? '—'
}

export function ImportPlayersCsvDialog({ open, onClose, leagueId, teamId }: Props) {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const [rows, setRows] = useState<ImportPlayerItem[] | null>(null)
  const [parseError, setParseError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => playersService.import(leagueId, teamId, rows ?? []),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams', teamId, 'players'] })
      handleClose()
    },
  })

  const handleClose = () => {
    setFileName(null)
    setRows(null)
    setParseError(null)
    mutation.reset()
    onClose()
  }

  const onFile = async (file: File | null) => {
    if (!file) return
    setParseError(null)
    setFileName(file.name)
    try {
      const text = await file.text()
      const parsed = parsePlayersFromCsv(text)
      if (parsed.length === 0) {
        setRows(null)
        setParseError('No se encontraron filas válidas. Usá columnas nombre,apellido[,apodo][,dni][,posicion].')
        return
      }
      setRows(parsed)
    } catch (err) {
      setRows(null)
      setParseError(err instanceof Error ? err.message : 'No se pudo leer el CSV')
    }
  }

  const preview = useMemo(() => (rows ?? []).slice(0, 50), [rows])

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Importar integrantes (CSV)</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Formato: <code>nombre,apellido</code> y opcionales <code>apodo,dni,posicion</code>. La primera fila puede ser encabezado.
        </Typography>
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv,text/csv"
          hidden
          onChange={(e) => void onFile(e.target.files?.[0] ?? null)}
        />
        <Button
          variant="outlined"
          startIcon={<UploadFileIcon />}
          onClick={() => fileInputRef.current?.click()}
          sx={{ mb: 2 }}
        >
          Elegir archivo CSV
        </Button>
        {fileName && (
          <Typography variant="body2" sx={{ mb: 1 }}>
            Archivo: {fileName} {rows ? `· ${rows.length} jugadores` : ''}
          </Typography>
        )}
        {parseError && <Alert severity="warning" sx={{ mb: 2 }}>{parseError}</Alert>}
        {mutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {mutation.error instanceof Error ? mutation.error.message : 'Error al importar'}
          </Alert>
        )}
        {preview.length > 0 && (
          <Box sx={{ maxHeight: 360, overflow: 'auto', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>Nombre</TableCell>
                  <TableCell>Apellido</TableCell>
                  <TableCell>Apodo</TableCell>
                  <TableCell>DNI</TableCell>
                  <TableCell>Posición</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {preview.map((row, idx) => (
                  <TableRow key={`${row.firstName}-${row.lastName}-${idx}`}>
                    <TableCell>{row.firstName}</TableCell>
                    <TableCell>{row.lastName}</TableCell>
                    <TableCell>{row.nickname || '—'}</TableCell>
                    <TableCell>{row.document || '—'}</TableCell>
                    <TableCell>{positionLabel(row.position)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancelar</Button>
        <Button
          variant="contained"
          disabled={!rows?.length || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? <CircularProgress size={22} /> : `Importar ${rows?.length ?? 0}`}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
