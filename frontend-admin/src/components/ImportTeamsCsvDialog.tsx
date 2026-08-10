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
  FormControl,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
  Chip,
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'
import type { Team } from '../api/types'
import { parseTeamNamesFromCsv } from '../utils/parseTeamCsv'
import {
  mappingsNeedReview,
  matchCsvNamesToTeams,
  type TeamCsvRowMapping,
  type TeamMatchAction,
} from '../utils/teamNameMatch'

const CREATE_VALUE = '__create__'

export type ImportTeamsCsvDialogProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  divisionId: string
  divisionName: string
  teams: Team[]
  assignedTeamIds: string[]
  onImported?: (summary: { matched: number; created: number; skipped: number }) => void
}

function reasonChip(row: TeamCsvRowMapping) {
  if (row.action === 'create' && !row.needsReview) {
    return <Chip size="small" label="Crear nuevo" color="info" variant="outlined" />
  }
  switch (row.reason) {
    case 'exact':
      return <Chip size="small" label="Exacto" color="success" variant="outlined" />
    case 'high':
      return <Chip size="small" label="Match alto" color="success" variant="outlined" />
    case 'fuzzy':
      return <Chip size="small" label="Revisar" color="warning" />
    case 'ambiguous':
      return <Chip size="small" label="Ambiguo" color="warning" />
    default:
      return <Chip size="small" label="Sin match" color="default" variant="outlined" />
  }
}

export function ImportTeamsCsvDialog({
  open,
  onClose,
  leagueId,
  seasonId,
  divisionId,
  divisionName,
  teams,
  assignedTeamIds,
  onImported,
}: ImportTeamsCsvDialogProps) {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const [rows, setRows] = useState<TeamCsvRowMapping[] | null>(null)
  const [showReview, setShowReview] = useState(false)
  const [localError, setLocalError] = useState<string | null>(null)

  const assignedSet = useMemo(() => new Set(assignedTeamIds), [assignedTeamIds])

  const reset = () => {
    setFileName(null)
    setRows(null)
    setShowReview(false)
    setLocalError(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleClose = () => {
    if (importMutation.isPending) return
    reset()
    onClose()
  }

  const importMutation = useMutation({
    mutationFn: async (mappings: TeamCsvRowMapping[]) => {
      const seen = new Set<string>()
      let matched = 0
      let created = 0
      let skipped = 0
      const errors: string[] = []

      for (const row of mappings) {
        try {
          if (row.action === 'match' && row.teamId) {
            if (assignedSet.has(row.teamId)) {
              skipped += 1
              continue
            }
            if (seen.has(row.teamId)) {
              errors.push(`${row.csvName}: el mismo equipo ya fue elegido en otra fila`)
              continue
            }
            seen.add(row.teamId)
            await teamsService.assignTeamToDivisionSeason(leagueId, seasonId, divisionId, row.teamId)
            matched += 1
          } else {
            const createdTeam = await teamsService.create(leagueId, {
              name: row.csvName,
              seasonId,
              divisionId,
            })
            await teamsService.assignTeamToDivisionSeason(leagueId, seasonId, divisionId, createdTeam.id)
            created += 1
          }
        } catch (e) {
          errors.push(`${row.csvName}: ${e instanceof Error ? e.message : 'Error'}`)
        }
      }

      if (errors.length > 0) {
        throw new Error(
          `Importados ${matched + created} (matched ${matched}, created ${created}). Errores:\n${errors.join('\n')}`
        )
      }
      return { matched, created, skipped }
    },
    onSuccess: (summary) => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] })
      void queryClient.invalidateQueries({
        queryKey: ['leagues', leagueId, 'seasons', seasonId, 'assigned-team-ids'],
      })
      onImported?.(summary)
      reset()
      onClose()
    },
    onError: (err) => {
      setLocalError(err instanceof Error ? err.message : 'Import failed')
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      void queryClient.invalidateQueries({
        queryKey: ['leagues', leagueId, 'seasons', seasonId, 'assigned-team-ids'],
      })
    },
  })

  const applyMappings = (mappings: TeamCsvRowMapping[]) => {
    setLocalError(null)
    const duplicateIds = new Map<string, string[]>()
    for (const row of mappings) {
      if (row.action !== 'match' || !row.teamId) continue
      const list = duplicateIds.get(row.teamId) ?? []
      list.push(row.csvName)
      duplicateIds.set(row.teamId, list)
    }
    for (const [, names] of duplicateIds) {
      if (names.length > 1) {
        setLocalError(`Varias filas apuntan al mismo equipo: ${names.join(', ')}`)
        setShowReview(true)
        setRows(mappings)
        return
      }
    }
    importMutation.mutate(mappings)
  }

  const handleFile = async (file: File) => {
    setLocalError(null)
    setFileName(file.name)
    const text = await file.text()
    const names = parseTeamNamesFromCsv(text)
    if (names.length === 0) {
      setLocalError('El CSV no tiene nombres de equipo. Esperamos una columna "Equipo" (o la primera columna).')
      setRows(null)
      setShowReview(false)
      return
    }

    const mapped = matchCsvNamesToTeams(names, teams, { alreadyAssignedIds: assignedSet })
    setRows(mapped)

    if (mappingsNeedReview(mapped)) {
      setShowReview(true)
      return
    }
    // Sin ambigüedades: exactos/altos + crear nuevos → aplicar directo.
    applyMappings(mapped)
  }

  const updateRow = (index: number, action: TeamMatchAction, teamId: string | null) => {
    setRows((prev) => {
      if (!prev) return prev
      return prev.map((row, i) =>
        i === index
          ? {
              ...row,
              action,
              teamId,
              needsReview: false,
              reason: action === 'create' ? 'none' : row.reason === 'none' ? 'fuzzy' : row.reason,
            }
          : row
      )
    })
  }

  const reviewCount = rows?.filter((r) => r.needsReview).length ?? 0
  const createCount = rows?.filter((r) => r.action === 'create').length ?? 0
  const matchCount = rows?.filter((r) => r.action === 'match').length ?? 0

  return (
    <Dialog open={open} onClose={handleClose} maxWidth={showReview ? 'md' : 'sm'} fullWidth>
      <DialogTitle>Importar equipos — {divisionName}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Subí un CSV con una columna <strong>Equipo</strong> (una fila por nombre). Si el nombre coincide con un
          equipo existente se asigna; si no, se puede crear. Solo pedimos confirmación cuando el match es dudoso.
        </Typography>

        <input
          ref={fileInputRef}
          type="file"
          accept=".csv,text/csv"
          hidden
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) void handleFile(file)
          }}
        />

        <Button
          variant="outlined"
          startIcon={<UploadFileIcon />}
          onClick={() => fileInputRef.current?.click()}
          disabled={importMutation.isPending}
        >
          Elegir CSV
        </Button>
        {fileName && (
          <Typography variant="caption" sx={{ ml: 1.5 }} color="text.secondary">
            {fileName}
          </Typography>
        )}

        {importMutation.isPending && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <CircularProgress size={20} />
            <Typography variant="body2">Importando equipos…</Typography>
          </Box>
        )}

        {localError && (
          <Alert severity="error" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setLocalError(null)}>
            {localError}
          </Alert>
        )}

        {showReview && rows && (
          <Box sx={{ mt: 2 }}>
            <Alert severity="info" sx={{ mb: 1.5 }}>
              Revisá {reviewCount} fila(s) dudosas. Resumen: {matchCount} match, {createCount} a crear.
            </Alert>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>CSV</TableCell>
                  <TableCell>Estado</TableCell>
                  <TableCell>Asignar / crear</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row, index) => (
                  <TableRow key={`${row.csvName}-${index}`} selected={row.needsReview}>
                    <TableCell>
                      <Typography variant="body2">{row.csvName}</Typography>
                      {row.score > 0 && row.action === 'match' && (
                        <Typography variant="caption" color="text.secondary">
                          similitud {(row.score * 100).toFixed(0)}%
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>{reasonChip(row)}</TableCell>
                    <TableCell sx={{ minWidth: 220 }}>
                      <FormControl fullWidth size="small">
                        <Select
                          value={row.action === 'create' ? CREATE_VALUE : (row.teamId ?? CREATE_VALUE)}
                          onChange={(e) => {
                            const v = e.target.value
                            if (v === CREATE_VALUE) updateRow(index, 'create', null)
                            else updateRow(index, 'match', v)
                          }}
                        >
                          <MenuItem value={CREATE_VALUE}>
                            <em>Crear nuevo: {row.csvName}</em>
                          </MenuItem>
                          {(row.candidates.length > 0
                            ? row.candidates
                            : teams.map((t) => ({
                                teamId: t.id,
                                label: t.displayName || t.name,
                                score: 0,
                              }))
                          ).map((c) => (
                            <MenuItem key={c.teamId} value={c.teamId}>
                              {c.label}
                              {c.score > 0 ? ` (${Math.round(c.score * 100)}%)` : ''}
                              {assignedSet.has(c.teamId) ? ' — ya asignado' : ''}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={importMutation.isPending}>
          Cancelar
        </Button>
        {showReview && rows && (
          <Button
            variant="contained"
            onClick={() => applyMappings(rows)}
            disabled={importMutation.isPending || rows.length === 0}
          >
            {importMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Confirmar e importar'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}
