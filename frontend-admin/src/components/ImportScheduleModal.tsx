import { useEffect, useMemo, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { useMutation, useQuery } from '@tanstack/react-query'
import { fieldsService } from '../api/fields'
import { matchesService, type MatchListItem } from '../api/matches'
import { seasonsService, type TeamInSetup } from '../api/seasons'
import { parseScheduleCsv, type ScheduleCsvRow } from '../utils/parseScheduleCsv'
import {
  matchCsvNamesToTeams,
  type TeamCsvRowMapping,
  type TeamMatchCandidate,
} from '../utils/teamNameMatch'

export type ImportScheduleModalProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  initialDivisionId?: string
  initialRound?: number | ''
  divisions: Array<{ id: string; name: string }>
  rounds: number[]
  seasonClosed?: boolean
  onImported?: (summary: { updatedCount: number; warnings: string[] }) => void
}

type RowStatus =
  | 'ready'
  | 'review_teams'
  | 'inverted'
  | 'no_fixture'
  | 'bad_field'
  | 'out_of_division'

type PreparedRow = {
  key: string
  csv: ScheduleCsvRow
  homeMapping: TeamCsvRowMapping
  awayMapping: TeamCsvRowMapping
  fieldOk: boolean
  fixture: MatchListItem | null
  inverted: boolean
  allowInverted: boolean
  status: RowStatus
}

function displayName(t: TeamInSetup) {
  return t.displayName ?? t.name
}

function toCandidates(teams: TeamInSetup[]): TeamMatchCandidate[] {
  return teams.map((t) => ({
    id: t.id,
    name: t.name,
    displayName: t.displayName,
    shortName: t.shortName,
    suffix: t.suffix,
  }))
}

function isMatchedToDivision(m: TeamCsvRowMapping): boolean {
  return m.action === 'match' && !!m.teamId && m.score >= 0.72
}

function statusLabel(s: RowStatus): string {
  switch (s) {
    case 'ready':
      return 'Listo'
    case 'review_teams':
      return 'Revisar / duda'
    case 'inverted':
      return 'Localía invertida'
    case 'no_fixture':
      return 'Sin fixture en fecha'
    case 'bad_field':
      return 'Cancha desconocida'
    case 'out_of_division':
      return 'Otra división'
    default:
      return s
  }
}

export function ImportScheduleModal({
  open,
  onClose,
  leagueId,
  seasonId,
  initialDivisionId = '',
  initialRound = '',
  divisions,
  rounds,
  seasonClosed = false,
  onImported,
}: ImportScheduleModalProps) {
  const fileRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const [csvRows, setCsvRows] = useState<ScheduleCsvRow[] | null>(null)
  const [divisionId, setDivisionId] = useState(initialDivisionId)
  const [round, setRound] = useState<number | ''>(initialRound)
  const [localError, setLocalError] = useState<string | null>(null)
  const [prepared, setPrepared] = useState<PreparedRow[] | null>(null)
  const [teamOverride, setTeamOverride] = useState<Record<string, string>>({})
  /** Rows discarded by the user (typically other-division doubts). */
  const [discardedKeys, setDiscardedKeys] = useState<Set<string>>(() => new Set())

  useEffect(() => {
    if (!open) {
      setFileName(null)
      setCsvRows(null)
      setPrepared(null)
      setTeamOverride({})
      setDiscardedKeys(new Set())
      setLocalError(null)
      if (fileRef.current) fileRef.current.value = ''
      return
    }
    setDivisionId(initialDivisionId)
    setRound(initialRound)
    setLocalError(null)
  }, [open, initialDivisionId, initialRound])

  // Changing division resets discards/overrides for clean scoping.
  useEffect(() => {
    setDiscardedKeys(new Set())
    setTeamOverride({})
  }, [divisionId])

  const { data: setupData, isLoading: setupLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId, seasonId, signal),
    enabled: open && !!leagueId && !!seasonId,
  })

  const { data: fields = [], isLoading: fieldsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'fields'],
    queryFn: ({ signal }) => fieldsService.getByLeagueId(leagueId, signal),
    enabled: open && !!leagueId,
  })

  const { data: matchesData, isLoading: matchesLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, divisionId, round, 'schedule-import'],
    queryFn: ({ signal }) =>
      matchesService.getMatches(
        leagueId,
        { seasonId, divisionId, round: typeof round === 'number' ? round : undefined },
        signal
      ),
    enabled: open && !!leagueId && !!seasonId && !!divisionId && typeof round === 'number',
  })

  const divisionTeams = useMemo(() => {
    if (!setupData || !divisionId) return [] as TeamInSetup[]
    return setupData.divisions.find((d) => d.divisionId === divisionId)?.teams ?? []
  }, [setupData, divisionId])

  const fieldNames = useMemo(
    () => new Set(fields.map((f) => f.name.trim().toLowerCase())),
    [fields]
  )

  const roundFixtures = useMemo(() => {
    const list: MatchListItem[] = []
    for (const g of matchesData?.rounds ?? []) {
      if (typeof round === 'number' && g.roundNumber !== round) continue
      list.push(...g.matches)
    }
    return list
  }, [matchesData, round])

  useEffect(() => {
    if (!csvRows || !divisionId || typeof round !== 'number') {
      setPrepared(null)
      return
    }
    if (setupLoading || matchesLoading || fieldsLoading) return

    const candidates = toCandidates(divisionTeams)

    const next: PreparedRow[] = csvRows.map((csv, idx) => {
      // Match only this pair against the selected division (avoids cross-row stealing).
      const pairMaps = matchCsvNamesToTeams([csv.homeTeam, csv.awayTeam], candidates)
      const byCsv = new Map(pairMaps.map((m) => [m.csvName, m]))

      const applyOverride = (name: string, fallback: TeamCsvRowMapping): TeamCsvRowMapping => {
        const base = byCsv.get(name) ?? fallback
        const ov = teamOverride[name]
        if (!ov) return base
        return {
          ...base,
          action: 'match',
          teamId: ov,
          needsReview: false,
          reason: 'exact',
          score: 1,
        }
      }

      const emptyMap = (name: string): TeamCsvRowMapping => ({
        csvName: name,
        normalizedCsv: name,
        action: 'create',
        teamId: null,
        score: 0,
        candidates: [],
        needsReview: false,
        reason: 'none',
      })

      const homeMapping = applyOverride(csv.homeTeam, emptyMap(csv.homeTeam))
      const awayMapping = applyOverride(csv.awayTeam, emptyMap(csv.awayTeam))

      const homeInDiv = isMatchedToDivision(homeMapping)
      const awayInDiv = isMatchedToDivision(awayMapping)
      const fieldOk = !!csv.fieldName && fieldNames.has(csv.fieldName.trim().toLowerCase())
      const homeId = homeInDiv ? homeMapping.teamId : null
      const awayId = awayInDiv ? awayMapping.teamId : null

      let fixture: MatchListItem | null = null
      let inverted = false
      if (homeId && awayId) {
        const direct = roundFixtures.find((f) => f.homeTeamId === homeId && f.awayTeamId === awayId)
        const swapped = roundFixtures.find((f) => f.homeTeamId === awayId && f.awayTeamId === homeId)
        if (direct) fixture = direct
        else if (swapped) {
          fixture = swapped
          inverted = true
        }
      }

      let status: RowStatus
      if (!homeInDiv && !awayInDiv) {
        // Neither side looks like a team in this division → other division (auto-hide).
        status = 'out_of_division'
      } else if (
        !homeInDiv ||
        !awayInDiv ||
        homeMapping.needsReview ||
        awayMapping.needsReview ||
        !homeId ||
        !awayId
      ) {
        // Partial match or ambiguous names → resolve or discard.
        status = 'review_teams'
      } else if (!fieldOk) {
        status = 'bad_field'
      } else if (!fixture) {
        status = 'no_fixture'
      } else if (inverted) {
        status = 'inverted'
      } else {
        status = 'ready'
      }

      return {
        key: `${idx}-${csv.homeTeam}-${csv.awayTeam}`,
        csv,
        homeMapping,
        awayMapping,
        fieldOk,
        fixture,
        inverted,
        allowInverted: false,
        status,
      }
    })

    setPrepared((prev) => {
      if (!prev) return next
      const allowMap = new Map(prev.map((p) => [p.key, p.allowInverted]))
      return next.map((row) => {
        const allow = allowMap.get(row.key) ?? false
        return {
          ...row,
          allowInverted: allow,
          status: row.status === 'inverted' && allow ? 'ready' : row.status,
        }
      })
    })
  }, [
    csvRows,
    divisionId,
    round,
    divisionTeams,
    roundFixtures,
    fieldNames,
    setupLoading,
    matchesLoading,
    fieldsLoading,
    teamOverride,
  ])

  const visibleRows = useMemo(() => {
    if (!prepared) return []
    return prepared.filter((p) => p.status !== 'out_of_division' && !discardedKeys.has(p.key))
  }, [prepared, discardedKeys])

  const hiddenOtherCount = prepared?.filter((p) => p.status === 'out_of_division').length ?? 0
  const discardedCount = discardedKeys.size

  const importMutation = useMutation({
    mutationFn: async () => {
      if (!prepared || !divisionId || typeof round !== 'number') {
        throw new Error('Seleccioná división y fecha.')
      }
      const rows = prepared
        .filter((p) => !discardedKeys.has(p.key))
        .filter((p) => p.status !== 'out_of_division')
        .filter((p) => p.status === 'ready' || (p.status === 'inverted' && p.allowInverted))
        .filter((p) => p.homeMapping.teamId && p.awayMapping.teamId && p.fieldOk)
        .map((p) => ({
          homeTeamId: p.homeMapping.teamId!,
          awayTeamId: p.awayMapping.teamId!,
          startTime: p.csv.startTime,
          fieldName: p.csv.fieldName.trim(),
          allowInverted: p.inverted && p.allowInverted,
        }))

      if (rows.length === 0) {
        throw new Error('No hay filas confirmadas para aplicar en esta división/fecha.')
      }

      return matchesService.importSchedule(leagueId, {
        seasonId,
        divisionId,
        round,
        rows,
      })
    },
    onSuccess: (res) => {
      onImported?.(res)
      setLocalError(null)
    },
    onError: (err) => {
      setLocalError(err instanceof Error ? err.message : 'No se pudo importar el cronograma.')
    },
  })

  const handleFile = async (file: File | null) => {
    if (!file) return
    setLocalError(null)
    try {
      const text = await file.text()
      const rows = parseScheduleCsv(text)
      setCsvRows(rows)
      setFileName(file.name)
      setTeamOverride({})
      setDiscardedKeys(new Set())
      setPrepared(null)
    } catch (e) {
      setCsvRows(null)
      setFileName(null)
      setLocalError(e instanceof Error ? e.message : 'CSV inválido')
    }
  }

  const discardRow = (key: string) => {
    setDiscardedKeys((prev) => {
      const next = new Set(prev)
      next.add(key)
      return next
    })
  }

  const discardAllDoubts = () => {
    if (!prepared) return
    setDiscardedKeys((prev) => {
      const next = new Set(prev)
      for (const p of prepared) {
        if (
          p.status === 'review_teams' ||
          p.status === 'no_fixture' ||
          (p.status === 'inverted' && !p.allowInverted)
        ) {
          next.add(p.key)
        }
      }
      return next
    })
  }

  const readyCount = visibleRows.filter(
    (p) => p.status === 'ready' || (p.status === 'inverted' && p.allowInverted)
  ).length
  const reviewNeeded = visibleRows.some(
    (p) => p.status === 'review_teams' || (p.status === 'inverted' && !p.allowInverted)
  )
  const canApply =
    !seasonClosed &&
    !!csvRows &&
    !!divisionId &&
    typeof round === 'number' &&
    readyCount > 0 &&
    !importMutation.isPending

  const divisionName = divisions.find((d) => d.id === divisionId)?.name ?? 'la división'

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth>
      <DialogTitle>Importar horarios y canchas</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          El CSV puede traer <strong>todas las divisiones</strong> (sin columna de división). Al elegir una
          división, se dejan solo partidos cuyos equipos matchean con esa división; el resto se oculta.
          Las dudas quedan para resolver o <strong>descartar</strong> si son de otra división. El archivo
          permanece cargado para repetir en otras fechas/divisiones.
        </Typography>

        {seasonClosed && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            Temporada cerrada: no se puede importar cronograma.
          </Alert>
        )}

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2, alignItems: 'center' }}>
          <Button component="label" variant="outlined" startIcon={<UploadFileIcon />} disabled={seasonClosed}>
            {fileName ? 'Cambiar CSV' : 'Subir CSV'}
            <input
              ref={fileRef}
              hidden
              type="file"
              accept=".csv,text/csv"
              onChange={(e) => void handleFile(e.target.files?.[0] ?? null)}
            />
          </Button>
          {fileName && (
            <Chip
              label={`${fileName} · ${csvRows?.length ?? 0} filas totales`}
              onDelete={() => {
                setFileName(null)
                setCsvRows(null)
                setPrepared(null)
                setTeamOverride({})
                setDiscardedKeys(new Set())
                if (fileRef.current) fileRef.current.value = ''
              }}
            />
          )}
          <FormControl size="small" sx={{ minWidth: 180 }} disabled={!csvRows || seasonClosed}>
            <InputLabel id="sched-div">División</InputLabel>
            <Select
              labelId="sched-div"
              label="División"
              value={divisionId}
              onChange={(e) => setDivisionId(e.target.value)}
            >
              {divisions.map((d) => (
                <MenuItem key={d.id} value={d.id}>
                  {d.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 120 }} disabled={!csvRows || seasonClosed}>
            <InputLabel id="sched-round">Fecha</InputLabel>
            <Select
              labelId="sched-round"
              label="Fecha"
              value={round === '' ? '' : String(round)}
              onChange={(e) => setRound(e.target.value === '' ? '' : Number(e.target.value))}
            >
              {rounds.map((r) => (
                <MenuItem key={r} value={String(r)}>
                  {r}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        {(setupLoading || fieldsLoading || matchesLoading) && csvRows && divisionId && round !== '' && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
            <CircularProgress size={28} />
          </Box>
        )}

        {localError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setLocalError(null)}>
            {localError}
          </Alert>
        )}

        {importMutation.isSuccess && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Se actualizaron {importMutation.data.updatedCount} partidos.
            {importMutation.data.warnings?.length
              ? ` Avisos: ${importMutation.data.warnings.length}.`
              : ''}{' '}
            Podés cambiar división/fecha y aplicar de nuevo con el mismo CSV.
          </Alert>
        )}

        {importMutation.isSuccess && (importMutation.data.warnings?.length ?? 0) > 0 && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            <Typography variant="body2" component="div">
              {(importMutation.data.warnings ?? []).slice(0, 8).map((w) => (
                <div key={w}>{w}</div>
              ))}
              {(importMutation.data.warnings?.length ?? 0) > 8 && (
                <div>…y {(importMutation.data.warnings?.length ?? 0) - 8} más</div>
              )}
            </Typography>
          </Alert>
        )}

        {prepared && (
          <>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center', mb: 1 }}>
              <Typography variant="body2">
                Mostrando <strong>{visibleRows.length}</strong> de {prepared.length} para{' '}
                <strong>{divisionName}</strong>
                {hiddenOtherCount > 0 ? ` · ${hiddenOtherCount} ocultas (otra división)` : ''}
                {discardedCount > 0 ? ` · ${discardedCount} descartadas` : ''}
                {' · '}Listos: <strong>{readyCount}</strong>
                {reviewNeeded ? ' · hay dudas por resolver' : ''}
              </Typography>
              {reviewNeeded && (
                <Button size="small" color="warning" onClick={discardAllDoubts}>
                  Descartar todas las dudas
                </Button>
              )}
            </Box>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Cancha</TableCell>
                  <TableCell>Local (CSV)</TableCell>
                  <TableCell>Visitante (CSV)</TableCell>
                  <TableCell>Hora</TableCell>
                  <TableCell>Estado</TableCell>
                  <TableCell>Acción</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {visibleRows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6}>
                      <Typography variant="body2" color="text.secondary">
                        No hay partidos candidatos para esta división/fecha (o fueron todos descartados).
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
                {visibleRows.map((row) => (
                  <TableRow key={row.key}>
                    <TableCell>
                      {row.csv.fieldName}
                      {!row.fieldOk && row.status !== 'review_teams' && (
                        <Typography variant="caption" color="error" display="block">
                          no existe
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{row.csv.homeTeam}</Typography>
                      {row.status === 'review_teams' && (
                        <FormControl size="small" fullWidth sx={{ mt: 0.5, minWidth: 140 }}>
                          <Select
                            displayEmpty
                            value={row.homeMapping.teamId ?? ''}
                            onChange={(e) =>
                              setTeamOverride((prev) => ({
                                ...prev,
                                [row.csv.homeTeam]: e.target.value,
                              }))
                            }
                          >
                            <MenuItem value="">
                              <em>Elegir equipo / vacío = no es de esta división</em>
                            </MenuItem>
                            {divisionTeams.map((t) => (
                              <MenuItem key={t.id} value={t.id}>
                                {displayName(t)}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      )}
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{row.csv.awayTeam}</Typography>
                      {row.status === 'review_teams' && (
                        <FormControl size="small" fullWidth sx={{ mt: 0.5, minWidth: 140 }}>
                          <Select
                            displayEmpty
                            value={row.awayMapping.teamId ?? ''}
                            onChange={(e) =>
                              setTeamOverride((prev) => ({
                                ...prev,
                                [row.csv.awayTeam]: e.target.value,
                              }))
                            }
                          >
                            <MenuItem value="">
                              <em>Elegir equipo / vacío = no es de esta división</em>
                            </MenuItem>
                            {divisionTeams.map((t) => (
                              <MenuItem key={t.id} value={t.id}>
                                {displayName(t)}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      )}
                    </TableCell>
                    <TableCell>{row.csv.startTime}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={statusLabel(
                          row.status === 'inverted' && row.allowInverted ? 'ready' : row.status
                        )}
                        color={
                          row.status === 'ready' || (row.status === 'inverted' && row.allowInverted)
                            ? 'success'
                            : 'warning'
                        }
                      />
                      {row.fixture && (
                        <Typography variant="caption" display="block" color="text.secondary">
                          Fixture: {row.fixture.homeTeamName} vs {row.fixture.awayTeamName}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                        {row.status === 'inverted' && (
                          <FormControlLabel
                            control={
                              <Checkbox
                                checked={row.allowInverted}
                                onChange={(e) =>
                                  setPrepared((prev) =>
                                    (prev ?? []).map((p) =>
                                      p.key === row.key
                                        ? {
                                            ...p,
                                            allowInverted: e.target.checked,
                                            status: e.target.checked ? 'ready' : 'inverted',
                                          }
                                        : p
                                    )
                                  )
                                }
                              />
                            }
                            label="Confirmar localía invertida"
                          />
                        )}
                        {(row.status === 'review_teams' ||
                          row.status === 'no_fixture' ||
                          row.status === 'bad_field' ||
                          (row.status === 'inverted' && !row.allowInverted)) && (
                          <Button
                            size="small"
                            color="inherit"
                            startIcon={<DeleteOutlineIcon />}
                            onClick={() => discardRow(row.key)}
                          >
                            Descartar (otra división)
                          </Button>
                        )}
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cerrar</Button>
        <Button variant="contained" onClick={() => importMutation.mutate()} disabled={!canApply}>
          {importMutation.isPending ? <CircularProgress size={22} /> : `Aplicar (${readyCount})`}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
