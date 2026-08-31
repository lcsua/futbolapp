import { useEffect, useMemo, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { seasonsService } from '../api/seasons'
import { matchesService } from '../api/matches'
import { teamNameAliasesService } from '../api/teamNameAliases'
import type { TeamInSetup } from '../api/seasons'
import {
  mapTeamNamesForDivision,
  matchDivisionName,
  mappingsNeedReview,
  parseMatchResultsCsv,
  type JsonDivisionRoundBlock,
} from '../utils/parseMatchResultsJson'
import { aliasUpsertsFromMappings, type TeamCsvRowMapping } from '../utils/teamNameMatch'

const CREATE_VALUE = '__missing__'
const ALL_DIVISIONS = ''

type DivisionPlan = {
  jsonDivision: string
  divisionId: string | null
  divisionName: string | null
  round: number
  matches: JsonDivisionRoundBlock['matches']
  teamMappings: TeamCsvRowMapping[]
  needsReview: boolean
  error?: string
}

export type ImportMatchResultsModalProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  /** Preselect division filter from Matches page (optional). */
  filterDivisionId?: string
  divisions: Array<{ id: string; name: string }>
  onImported?: (summary: {
    updatedCount: number
    createdCount: number
    skippedCount: number
    notCreatedCount: number
    warnings: string[]
  }) => void
}

function displayName(t: TeamInSetup) {
  return t.displayName ?? t.name
}

function statusLabel(status: string): string {
  const s = status.trim().toLowerCase()
  if (s === 'finished') return 'Finalizado'
  if (s === 'suspended') return 'Suspendido'
  if (s === 'postponed') return 'Pospuesto'
  if (s === 'cancelled') return 'Cancelado'
  return status.trim() || 'Finalizado'
}

export function ImportMatchResultsModal({
  open,
  onClose,
  leagueId,
  seasonId,
  filterDivisionId = '',
  divisions,
  onImported,
}: ImportMatchResultsModalProps) {
  const queryClient = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const [csvText, setCsvText] = useState<string | null>(null)
  const [plans, setPlans] = useState<DivisionPlan[] | null>(null)
  const [showReview, setShowReview] = useState(false)
  const [localError, setLocalError] = useState<string | null>(null)
  const [infoMsg, setInfoMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  /** Empty = import all divisions found in JSON; otherwise only that division. */
  const [scopeDivisionId, setScopeDivisionId] = useState(filterDivisionId)

  useEffect(() => {
    if (open) {
      setScopeDivisionId(filterDivisionId)
    }
  }, [open, filterDivisionId])

  const { data: setupData, isLoading: setupLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId, seasonId, signal),
    enabled: open && !!leagueId && !!seasonId,
  })

  const { data: aliasesData } = useQuery({
    queryKey: ['leagues', leagueId, 'team-name-aliases'],
    queryFn: ({ signal }) => teamNameAliasesService.list(leagueId, signal),
    enabled: open && !!leagueId,
  })

  const aliasByNormalized = useMemo(() => {
    const map = new Map<string, string>()
    for (const a of aliasesData?.items ?? []) {
      map.set(a.normalizedAlias, a.teamId)
    }
    return map
  }, [aliasesData])

  const handleClose = () => {
    if (importMutation.isPending) return
    setSuccessMsg(null)
    setLocalError(null)
    onClose()
  }

  const buildPlans = (
    blocks: JsonDivisionRoundBlock[],
    scopeId: string
  ): { plans: DivisionPlan[]; notes: string[] } => {
    const setupDivisions = setupData?.divisions ?? []
    const result: DivisionPlan[] = []
    const notes: string[] = []

    for (const block of blocks) {
      notes.push(...block.skippedByes, ...block.skippedOther)

      if (block.matches.length === 0) {
        notes.push(`"${block.division}": sin partidos importables (solo libres/omitidos).`)
        continue
      }

      // Always match against ALL league divisions first. Filtering candidates to the scoped
      // division alone lets "45 Zona A" fuzzily attach to "45 Zona B" (~0.89 similarity).
      const matched = matchDivisionName(block.division, divisions)
      if (!matched) {
        if (scopeId) {
          // Other / unknown division while scoped — skip quietly.
          continue
        }
        result.push({
          jsonDivision: block.division,
          divisionId: null,
          divisionName: null,
          round: block.round,
          matches: block.matches,
          teamMappings: [],
          needsReview: true,
          error: `No se encontró la división "${block.division}" en la liga.`,
        })
        continue
      }

      if (scopeId && matched.divisionId !== scopeId) {
        continue
      }

      const setupDiv = setupDivisions.find((d) => d.divisionId === matched.divisionId)
      const teams = setupDiv?.teams ?? []
      if (teams.length === 0) {
        result.push({
          jsonDivision: block.division,
          divisionId: matched.divisionId,
          divisionName: matched.name,
          round: block.round,
          matches: block.matches,
          teamMappings: [],
          needsReview: true,
          error: `La división "${matched.name}" no tiene equipos asignados en esta temporada.`,
        })
        continue
      }

      const names = [...new Set(block.matches.flatMap((m) => [m.homeTeam, m.awayTeam]))]
      const teamMappings = mapTeamNamesForDivision(names, teams, aliasByNormalized)
      result.push({
        jsonDivision: block.division,
        divisionId: matched.divisionId,
        divisionName: matched.name,
        round: block.round,
        matches: block.matches,
        teamMappings,
        needsReview: mappingsNeedReview(teamMappings) || teamMappings.some((m) => m.action === 'create'),
      })
    }

    return { plans: result, notes }
  }

  const nameToTeamId = (plan: DivisionPlan, csvName: string): string | null => {
    const row = plan.teamMappings.find((m) => m.csvName === csvName)
    if (!row || row.action !== 'match' || !row.teamId) return null
    return row.teamId
  }

  const importMutation = useMutation({
    mutationFn: async (current: DivisionPlan[]) => {
      const payloadDivisions = []
      for (const plan of current) {
        if (!plan.divisionId) throw new Error(plan.error || `División no resuelta: ${plan.jsonDivision}`)
        if (plan.error) throw new Error(plan.error)
        if (plan.matches.length === 0) continue
        if (plan.teamMappings.some((m) => m.action !== 'match' || !m.teamId)) {
          throw new Error(
            `Hay equipos sin mapear en "${plan.divisionName ?? plan.jsonDivision}". Confirmá el matching.`
          )
        }

        const matches = []
        for (const m of plan.matches) {
          const homeTeamId = nameToTeamId(plan, m.homeTeam)
          const awayTeamId = nameToTeamId(plan, m.awayTeam)
          if (!homeTeamId || !awayTeamId) {
            throw new Error(`No se pudo resolver ${m.homeTeam} vs ${m.awayTeam}`)
          }
          matches.push({
            homeTeamId,
            awayTeamId,
            homeScore: m.homeScore,
            awayScore: m.awayScore,
            status: m.status,
            homeCsvName: m.homeTeam,
            awayCsvName: m.awayTeam,
          })
        }
        payloadDivisions.push({ divisionId: plan.divisionId, round: plan.round, matches })
      }

      if (payloadDivisions.length === 0) {
        throw new Error('No hay divisiones/partidos para importar (revisá el filtro de división).')
      }

      const aliasItems = aliasUpsertsFromMappings(
        current.flatMap((p) => p.teamMappings)
      )
      if (aliasItems.length > 0) {
        await teamNameAliasesService.upsert(leagueId, aliasItems)
      }

      return matchesService.importResults(leagueId, {
        seasonId,
        divisions: payloadDivisions,
      })
    },
    onSuccess: (res) => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'team-name-aliases'] })
      onImported?.({
        updatedCount: res.updatedCount,
        createdCount: res.createdCount,
        skippedCount: res.skippedCount ?? 0,
        notCreatedCount: res.notCreatedCount ?? 0,
        warnings: [...(res.warnings ?? []), ...(infoMsg ? [infoMsg] : [])],
      })
      const parts = [
        res.updatedCount ? `${res.updatedCount} actualizado(s)` : null,
        res.createdCount ? `${res.createdCount} creado(s)` : null,
        res.skippedCount ? `${res.skippedCount} omitido(s) (ya tenían resultado)` : null,
        res.notCreatedCount ? `${res.notCreatedCount} no creado(s) (la fecha ya tiene fixture)` : null,
      ].filter(Boolean)
      setLocalError(null)
      setSuccessMsg(
        parts.length
          ? `Importado: ${parts.join(', ')}. Podés cambiar la división e importar de nuevo sin volver a subir el archivo.`
          : 'Importación OK. Podés cambiar la división e importar de nuevo sin volver a subir el archivo.'
      )
    },
    onError: (err) => {
      setLocalError(err instanceof Error ? err.message : 'Import failed')
      setShowReview(true)
    },
  })

  const applyCsvText = (text: string, scopeId: string) => {
    setLocalError(null)
    setInfoMsg(null)
    try {
      if (!setupData) {
        setLocalError('Cargando setup de temporada… esperá un segundo y reintentá.')
        return
      }
      const blocks = parseMatchResultsCsv(text)
      const { plans: next, notes } = buildPlans(blocks, scopeId)
      if (notes.length > 0) {
        setInfoMsg(
          [
            'Se omitieron libres / filas incompletas (equipos impares o bye):',
            ...notes.slice(0, 12),
            notes.length > 12 ? `… y ${notes.length - 12} más.` : null,
          ]
            .filter(Boolean)
            .join('\n')
        )
      }
      if (next.length === 0) {
        setLocalError(
          scopeId
            ? 'El CSV no tiene partidos para la división elegida (o el nombre de división en el CSV no coincide exactamente con esa división). Los bloques de otras divisiones (p. ej. Zona A vs Zona B) se omiten.'
            : 'El CSV no tiene bloques importables.'
        )
        setPlans(null)
        setShowReview(false)
        return
      }
      setPlans(next)
      setShowReview(true)
    } catch (e) {
      setLocalError(e instanceof Error ? e.message : 'CSV inválido')
      setPlans(null)
      setShowReview(false)
    }
  }

  const handleFile = async (file: File) => {
    setFileName(file.name)
    setSuccessMsg(null)
    try {
      const text = await file.text()
      setCsvText(text)
      applyCsvText(text, scopeDivisionId)
    } catch (e) {
      setCsvText(null)
      setLocalError(e instanceof Error ? e.message : 'No se pudo leer el archivo CSV.')
      setPlans(null)
      setShowReview(false)
    }
  }

  const handleScopeDivisionChange = (nextScopeId: string) => {
    setScopeDivisionId(nextScopeId)
    if (csvText) applyCsvText(csvText, nextScopeId)
  }

  useEffect(() => {
    if (!open || !csvText || !setupData) return
    applyCsvText(csvText, scopeDivisionId)
    // Re-apply stored CSV when reopening the modal or when setup finishes loading.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- applyCsvText is recreated each render
  }, [open, csvText, setupData, scopeDivisionId])

  const updateMapping = (planIndex: number, mappingIndex: number, teamId: string | null) => {
    setPlans((prev) => {
      if (!prev) return prev
      return prev.map((plan, i) => {
        if (i !== planIndex) return plan
        const teamMappings = plan.teamMappings.map((row, j) => {
          if (j !== mappingIndex) return row
          if (!teamId) {
            return { ...row, action: 'create' as const, teamId: null, needsReview: true }
          }
          return {
            ...row,
            action: 'match' as const,
            teamId,
            needsReview: false,
            reason: row.reason === 'none' ? ('fuzzy' as const) : row.reason,
          }
        })
        return {
          ...plan,
          teamMappings,
          needsReview: mappingsNeedReview(teamMappings) || teamMappings.some((m) => m.action === 'create'),
          error: plan.error,
        }
      })
    })
  }

  const updateDivisionId = (planIndex: number, divisionId: string) => {
    const div = divisions.find((d) => d.id === divisionId)
    const setupDiv = setupData?.divisions.find((d) => d.divisionId === divisionId)
    const teams = setupDiv?.teams ?? []
    setPlans((prev) => {
      if (!prev) return prev
      return prev.map((plan, i) => {
        if (i !== planIndex) return plan
        const names = [...new Set(plan.matches.flatMap((m) => [m.homeTeam, m.awayTeam]))]
        const teamMappings = mapTeamNamesForDivision(names, teams, aliasByNormalized)
        return {
          ...plan,
          divisionId,
          divisionName: div?.name ?? null,
          teamMappings,
          needsReview:
            mappingsNeedReview(teamMappings) || teamMappings.some((m) => m.action === 'create') || teams.length === 0,
          error: teams.length === 0 ? `Sin equipos en "${div?.name}".` : undefined,
        }
      })
    })
  }

  const reviewRows = useMemo(
    () => plans?.flatMap((p) => p.teamMappings.filter((m) => m.needsReview || m.action === 'create')) ?? [],
    [plans]
  )

  const hasMappingIssues = !!plans && plans.some((p) => p.needsReview || !!p.error)

  const mappedTeamLabel = (plan: DivisionPlan, csvName: string): string => {
    const row = plan.teamMappings.find((m) => m.csvName === csvName)
    if (!row || row.action !== 'match' || !row.teamId) return csvName
    const setupTeams = setupData?.divisions.find((d) => d.divisionId === plan.divisionId)?.teams ?? []
    const team = setupTeams.find((t) => t.id === row.teamId)
    return team ? displayName(team) : csvName
  }

  const canConfirm =
    !!plans &&
    plans.length > 0 &&
    plans.every(
      (p) =>
        !!p.divisionId &&
        !p.error &&
        p.matches.length > 0 &&
        p.teamMappings.every((m) => m.action === 'match' && !!m.teamId)
    )

  return (
    <Dialog open={open} onClose={handleClose} maxWidth={showReview ? 'md' : 'sm'} fullWidth>
      <DialogTitle>Importar resultados (CSV)</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Subí un CSV con columnas: <strong>fecha, division, Equipo 1, goles equipo 1, equipo 2, goles equipo 2,
          estado</strong>. Estados: Finalizado, Partido Suspendido, Libre. Los libres se omiten; los suspendidos se
          guardan como Suspendido (no suman en la tabla) hasta que se resuelvan a mano. En una fecha que ya tiene
          fixture, solo se cargan partidos <strong>pendientes</strong>; si ya tienen resultado no se pisan, y no se
          crean cruces nuevos. Si la fecha todavía no tiene partidos, se crea el fixture.
        </Typography>

        <FormControl fullWidth size="small" sx={{ mb: csvText ? 1 : 2 }} disabled={importMutation.isPending}>
          <InputLabel id="import-scope-division">División a importar</InputLabel>
          <Select
            labelId="import-scope-division"
            label="División a importar"
            value={scopeDivisionId}
            onChange={(e) => handleScopeDivisionChange(e.target.value)}
          >
            <MenuItem value={ALL_DIVISIONS}>
              <em>Todas las del CSV</em>
            </MenuItem>
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                Solo: {d.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        {csvText && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
            El archivo se mantiene: podés cambiar la división y se actualiza la vista previa.
          </Typography>
        )}

        <input
          ref={fileRef}
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
          disabled={setupLoading || importMutation.isPending || !seasonId}
          onClick={() => fileRef.current?.click()}
        >
          {fileName ? 'Cambiar CSV' : 'Elegir CSV'}
        </Button>
        {fileName && (
          <Typography variant="caption" sx={{ ml: 1.5 }} color="text.secondary">
            {fileName}
          </Typography>
        )}

        {(setupLoading || importMutation.isPending) && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <CircularProgress size={20} />
            <Typography variant="body2">
              {importMutation.isPending ? 'Importando…' : 'Cargando equipos de la temporada…'}
            </Typography>
          </Box>
        )}

        {successMsg && (
          <Alert severity="success" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setSuccessMsg(null)}>
            {successMsg}
          </Alert>
        )}

        {infoMsg && (
          <Alert severity="info" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setInfoMsg(null)}>
            {infoMsg}
          </Alert>
        )}

        {localError && (
          <Alert severity="error" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setLocalError(null)}>
            {localError}
          </Alert>
        )}

        {showReview && plans && (
          <Box sx={{ mt: 2 }}>
            {hasMappingIssues ? (
              <Alert severity="info" sx={{ mb: 1.5 }}>
                Revisá mapeos dudosos o divisiones sin match. No se crean equipos nuevos: hay que elegir uno
                existente de la división.
                {reviewRows.length > 0 ? ` (${reviewRows.length} nombre(s) a revisar)` : ''}
              </Alert>
            ) : (
              <Alert severity={successMsg ? 'info' : 'success'} sx={{ mb: 1.5 }}>
                {successMsg
                  ? 'El archivo sigue cargado. Cambiá la división si querés importar otro bloque, o confirmá de nuevo.'
                  : 'Vista previa lista. Confirmá para importar estos resultados.'}
              </Alert>
            )}

            {plans.map((plan, planIndex) => (
              <Box key={`${plan.jsonDivision}-${planIndex}`} sx={{ mb: 2.5 }}>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center', mb: 1 }}>
                  <Typography variant="subtitle2">
                    CSV: {plan.jsonDivision} (fecha {plan.round}, {plan.matches.length} partidos)
                  </Typography>
                  {plan.divisionName && (
                    <Chip size="small" label={plan.divisionName} />
                  )}
                  {plan.error && <Chip size="small" color="error" label="Error" />}
                </Box>
                {(plan.needsReview || !!plan.error || !plan.divisionId) && (
                  <FormControl fullWidth size="small" sx={{ mb: 1, maxWidth: 360 }}>
                    <InputLabel>División destino</InputLabel>
                    <Select
                      label="División destino"
                      value={plan.divisionId ?? ''}
                      onChange={(e) => updateDivisionId(planIndex, e.target.value)}
                    >
                      <MenuItem value="">
                        <em>Seleccionar</em>
                      </MenuItem>
                      {divisions.map((d) => (
                        <MenuItem key={d.id} value={d.id}>
                          {d.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
                {plan.error && (
                  <Alert severity="warning" sx={{ mb: 1 }}>
                    {plan.error}
                  </Alert>
                )}
                {(plan.needsReview || !!plan.error) && plan.teamMappings.length > 0 && (
                  <Table size="small" sx={{ mb: 1.5 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>Nombre en CSV</TableCell>
                        <TableCell>Equipo en división</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {plan.teamMappings.map((row, mappingIndex) => {
                        const setupTeams =
                          setupData?.divisions.find((d) => d.divisionId === plan.divisionId)?.teams ?? []
                        return (
                          <TableRow
                            key={`${row.csvName}-${mappingIndex}`}
                            selected={row.needsReview || row.action === 'create'}
                          >
                            <TableCell>
                              <Typography variant="body2">{row.csvName}</Typography>
                              {row.score > 0 && (
                                <Typography variant="caption" color="text.secondary">
                                  {(row.score * 100).toFixed(0)}%
                                </Typography>
                              )}
                            </TableCell>
                            <TableCell sx={{ minWidth: 220 }}>
                              <FormControl fullWidth size="small">
                                <Select
                                  value={row.action === 'match' && row.teamId ? row.teamId : CREATE_VALUE}
                                  onChange={(e) => {
                                    const v = e.target.value
                                    updateMapping(planIndex, mappingIndex, v === CREATE_VALUE ? null : v)
                                  }}
                                >
                                  <MenuItem value={CREATE_VALUE}>
                                    <em>Elegir equipo…</em>
                                  </MenuItem>
                                  {(row.candidates.length
                                    ? row.candidates
                                    : setupTeams.map((t) => ({
                                        teamId: t.id,
                                        label: displayName(t),
                                        score: 0,
                                      }))
                                  ).map((c) => (
                                    <MenuItem key={c.teamId} value={c.teamId}>
                                      {c.label}
                                      {c.score > 0 ? ` (${Math.round(c.score * 100)}%)` : ''}
                                    </MenuItem>
                                  ))}
                                </Select>
                              </FormControl>
                            </TableCell>
                          </TableRow>
                        )
                      })}
                    </TableBody>
                  </Table>
                )}

                {plan.matches.length > 0 && (
                  <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 320 }}>
                    <Table size="small" stickyHeader>
                      <TableHead>
                        <TableRow>
                          <TableCell>Local</TableCell>
                          <TableCell align="center">Goles</TableCell>
                          <TableCell>Visitante</TableCell>
                          <TableCell align="center">Goles</TableCell>
                          <TableCell>Estado</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {plan.matches.map((m, i) => (
                          <TableRow key={`${m.homeTeam}-${m.awayTeam}-${i}`}>
                            <TableCell>{mappedTeamLabel(plan, m.homeTeam)}</TableCell>
                            <TableCell align="center">{m.homeScore ?? '—'}</TableCell>
                            <TableCell>{mappedTeamLabel(plan, m.awayTeam)}</TableCell>
                            <TableCell align="center">{m.awayScore ?? '—'}</TableCell>
                            <TableCell>{statusLabel(m.status)}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </Box>
            ))}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={importMutation.isPending}>
          {csvText ? 'Cerrar' : 'Cancelar'}
        </Button>
        {showReview && plans && (
          <Button
            variant="contained"
            disabled={!canConfirm || importMutation.isPending}
            onClick={() => importMutation.mutate(plans)}
          >
            {importMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Confirmar e importar'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}
