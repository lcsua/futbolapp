import { useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Typography,
} from '@mui/material'
import PhotoLibraryIcon from '@mui/icons-material/PhotoLibrary'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { matchesService } from '../api/matches'

const MAX_BYTES = 10 * 1024 * 1024
const ACCEPT = 'image/jpeg,image/png,image/webp,image/gif,.jpg,.jpeg,.png,.webp,.gif'

export type ImportResultsImageModalProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  onCsvReady: (csv: string, label: string) => void
}

function mergeResultCsvs(chunks: string[]): string {
  const rows: string[] = []
  for (const chunk of chunks) {
    const lines = chunk
      .replace(/^\uFEFF/, '')
      .replace(/\r\n/g, '\n')
      .replace(/\r/g, '\n')
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0)
    for (const line of lines) {
      const key = line.toLowerCase().replace(/\s+/g, ' ')
      if (key.startsWith('fecha,') && key.includes('division') && key.includes('estado')) continue
      rows.push(line)
    }
  }
  return ['fecha,division,Equipo 1,goles equipo 1,equipo 2,goles equipo 2,estado', ...rows].join('\n')
}

export function ImportResultsImageModal({ open, onClose, leagueId, onCsvReady }: ImportResultsImageModalProps) {
  const fileRef = useRef<HTMLInputElement>(null)
  const [files, setFiles] = useState<File[]>([])
  const [reading, setReading] = useState(false)
  const [progress, setProgress] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const abortRef = useRef<AbortController | null>(null)

  const reset = () => {
    setFiles([])
    setReading(false)
    setProgress(null)
    setError(null)
    abortRef.current = null
  }

  const handleClose = () => {
    if (reading) abortRef.current?.abort()
    reset()
    onClose()
  }

  const addFiles = (list: FileList | null) => {
    if (!list?.length) return
    const next = [...files]
    const problems: string[] = []
    for (const file of Array.from(list)) {
      if (file.size > MAX_BYTES) {
        problems.push(`${file.name} supera los 10 MB.`)
        continue
      }
      if (next.some((f) => f.name === file.name && f.size === file.size)) continue
      next.push(file)
    }
    setFiles(next)
    setError(problems.length ? problems.join('\n') : null)
  }

  const readSheets = async () => {
    if (files.length === 0 || reading) return
    const controller = new AbortController()
    abortRef.current = controller
    setReading(true)
    setError(null)
    const chunks: string[] = []
    try {
      for (let i = 0; i < files.length; i++) {
        const file = files[i]
        setProgress(`Leyendo ${file.name} (${i + 1} de ${files.length})…`)
        const result = await matchesService.processResultImage(leagueId, file, controller.signal)
        if (!result.csv?.trim()) {
          throw new Error(`${file.name} no devolvió partidos.`)
        }
        chunks.push(result.csv)
      }
      const csv = mergeResultCsvs(chunks)
      const label = files.length === 1 ? files[0].name : `${files.length} planillas`
      reset()
      onCsvReady(csv, label)
    } catch (e) {
      if (controller.signal.aborted) return
      setError(e instanceof Error ? e.message : 'No se pudo leer la planilla.')
      setReading(false)
      setProgress(null)
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Importar resultados desde imagen</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Subí una o más fotos de la planilla. Las leemos y abrimos la misma vista previa del CSV para que confirmes
          equipos y divisiones antes de cargar los resultados.
        </Typography>

        <input
          ref={fileRef}
          type="file"
          accept={ACCEPT}
          multiple
          hidden
          onChange={(e) => {
            addFiles(e.target.files)
            e.target.value = ''
          }}
        />

        <Button
          variant="outlined"
          startIcon={<PhotoLibraryIcon />}
          disabled={reading}
          onClick={() => fileRef.current?.click()}
        >
          Elegir imágenes
        </Button>

        {files.length > 0 && (
          <List dense sx={{ mt: 1 }}>
            {files.map((file, index) => (
              <ListItem
                key={`${file.name}-${file.size}-${index}`}
                secondaryAction={
                  <IconButton
                    edge="end"
                    aria-label={`Quitar ${file.name}`}
                    disabled={reading}
                    onClick={() => setFiles((prev) => prev.filter((_, i) => i !== index))}
                  >
                    <DeleteOutlineIcon />
                  </IconButton>
                }
              >
                <ListItemText primary={file.name} secondary={`${Math.max(1, Math.round(file.size / 1024))} KB`} />
              </ListItem>
            ))}
          </List>
        )}

        {reading && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <CircularProgress size={20} />
            <Typography variant="body2">{progress ?? 'Leyendo la planilla…'}</Typography>
          </Box>
        )}

        {error && (
          <Alert severity="error" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancelar</Button>
        <Button variant="contained" onClick={() => void readSheets()} disabled={reading || files.length === 0}>
          Leer planillas
        </Button>
      </DialogActions>
    </Dialog>
  )
}
