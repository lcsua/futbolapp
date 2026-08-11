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
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  TextField,
  Typography,
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { fixturesService } from '../api/fixtures'
import { seasonsService, type TeamInSetup } from '../api/seasons'
import { teamNameAliasesService } from '../api/teamNameAliases'
import {
  inferRoundByes,
  parseFixtureCsv,
  rebuildFixtureCsv,
  uniqueFixtureTeamNames,
  type FixtureImportType,
  type ParsedFixtureCsvRow,
} from '../utils/parseFixtureCsv'
import {
  aliasUpsertsFromMappings,
  mappingsNeedReview,
  matchCsvNamesToTeams,
  type TeamCsvRowMapping,
} from '../utils/teamNameMatch'

const CSV_PLACEHOLDER = `round,home_team,away_team
1,TIGRES,LEONES
1,PUMAS,HALCONES`

const MISSING = '__missing__'

interface ImportFixtureModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  divisionId: string
  onSuccess: () => void
}

function displayName(t: TeamInSetup) {
  return t.displayName ?? t.name
}

export function ImportFixtureModal({
  open,
  onClose,
  leagueId,
  seasonId,
  divisionId,
  onSuccess,
}: ImportFixtureModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)

  const [csvText, setCsvText] = useState('')
  const [fileName, setFileName] = useState<string | null>(null)
  const [importType, setImportType] = useState<FixtureImportType | null>(null)
  const [parsedRows, setParsedRows] = useState<ParsedFixtureCsvRow[] | null>(null)
  const [teamMappings, setTeamMappings] = useState<TeamCsvRowMapping[] | null>(null)
  const [previewRows, setPreviewRows] = useState<ParsedFixtureCsvRow[] | null>(null)
  const [parseErrors, setParseErrors] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [analyzing, setAnalyzing] = useState(false)
  const [importing, setImporting] = useState(false)

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

  const divisionTeams = useMemo(() => {
    const div = setupData?.divisions.find((d) => d.divisionId === divisionId)
    return div?.teams ?? []
  }, [setupData, divisionId])

  const divisionName = useMemo(() => {
    return setupData?.divisions.find((d) => d.divisionId === divisionId)?.divisionName ?? ''
  }, [setupData, divisionId])

  useEffect(() => {
    if (!open) return
    setCsvText('')
    setFileName(null)
    setImportType(null)
    setParsedRows(null)
    setTeamMappings(null)
    setPreviewRows(null)
    setParseErrors([])
    setError(null)
    if (fileRef.current) fileRef.current.value = ''
  }, [open, divisionId, seasonId])

  const mappingUnresolved = (m: TeamCsvRowMapping) => m.action !== 'match' || !m.teamId

  // Preview as soon as every CSV name has a selected division team (fuzzy suggestions count).
  useEffect(() => {
    if (!importType || !parsedRows || !teamMappings) {
      setPreviewRows(null)
      return
    }
    if (teamMappings.some(mappingUnresolved)) {
      setPreviewRows(null)
      return
    }
    const rebuilt = rebuildFixtureCsv(importType, parsedRows, (csvName) => {
      const row = teamMappings.find((m) => m.csvName.toLowerCase() === csvName.toLowerCase())
      if (!row || row.action !== 'match' || !row.teamId) return null
      const team = divisionTeams.find((t) => t.id === row.teamId)
      return team ? displayName(team) : null
    })
    setPreviewRows(rebuilt.rows)
  }, [importType, parsedRows, teamMappings, divisionTeams])

  const reset = () => {
    setCsvText('')
    setFileName(null)
    setImportType(null)
    setParsedRows(null)
    setTeamMappings(null)
    setPreviewRows(null)
    setParseErrors([])
    setError(null)
    if (fileRef.current) fileRef.current.value = ''
  }

  const handleClose = () => {
    if (analyzing || importing) return
    reset()
    onClose()
  }

  const analyzeText = (text: string) => {
    setError(null)
    setParseErrors([])
    setPreviewRows(null)
    setTeamMappings(null)
    setParsedRows(null)
    setImportType(null)
    setAnalyzing(true)

    try {
      if (!setupData) {
        setError('Cargando equipos de la división… esperá un momento y reintentá.')
        return
      }
      if (divisionTeams.length === 0) {
        setError(
          divisionName
            ? `La división “${divisionName}” no tiene equipos asignados en esta temporada.`
            : 'La división seleccionada no tiene equipos asignados en esta temporada.',
        )
        return
      }

      const parsed = parseFixtureCsv(text)
      if (parsed.errors.length > 0 && parsed.rows.length === 0) {
        setParseErrors(parsed.errors)
        setError(parsed.errors[0] ?? 'CSV inválido')
        return
      }

      const names = uniqueFixtureTeamNames(parsed.rows)
      const mappings = matchCsvNamesToTeams(names, divisionTeams, { aliasByNormalized }).map((m) =>
        m.action === 'create'
          ? { ...m, needsReview: true }
          : m,
      )

      setImportType(parsed.importType)
      setParsedRows(parsed.rows)
      setTeamMappings(mappings)
      setParseErrors(parsed.errors.filter((e) => !e.includes('equipo')))
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.importModal.errorLoadingPreview'))
    } finally {
      setAnalyzing(false)
    }
  }

  const handleAnalyze = () => {
    if (!csvText.trim()) return
    analyzeText(csvText.trim())
  }

  const handleFile = async (file: File) => {
    setFileName(file.name)
    try {
      const text = await file.text()
      setCsvText(text)
      analyzeText(text)
    } catch {
      setError('No se pudo leer el archivo CSV.')
    }
  }

  const updateMapping = (mappingIndex: number, teamId: string | null) => {
    setTeamMappings((prev) => {
      if (!prev) return prev
      return prev.map((row, j) => {
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
    })
  }

  const confirmSuggestedMappings = () => {
    setTeamMappings((prev) => {
      if (!prev) return prev
      return prev.map((row) =>
        row.action === 'match' && row.teamId ? { ...row, needsReview: false } : row,
      )
    })
  }

  const missingTeamCount = teamMappings?.filter(mappingUnresolved).length ?? 0
  const reviewHintCount =
    teamMappings?.filter((m) => m.needsReview || mappingUnresolved(m)).length ?? 0

  const showReview =
    !!teamMappings &&
    (mappingsNeedReview(teamMappings) || teamMappings.some((m) => m.action === 'create' || !m.teamId))

  const inferredByes = useMemo(() => {
    if (!previewRows || divisionTeams.length === 0) return []
    return inferRoundByes(
      previewRows,
      divisionTeams.map((t) => displayName(t)),
    )
  }, [previewRows, divisionTeams])

  const matchPreviewRows = useMemo(
    () => (previewRows ?? []).filter((r) => !r.isBye),
    [previewRows],
  )

  const canImport =
    !!importType &&
    !!parsedRows &&
    !!teamMappings &&
    matchPreviewRows.length > 0 &&
    missingTeamCount === 0 &&
    !analyzing &&
    !importing

  const handleImport = async () => {
    if (!importType || !parsedRows || !teamMappings) return
    setError(null)
    setImporting(true)
    try {
      const aliasItems = aliasUpsertsFromMappings(teamMappings)
      if (aliasItems.length > 0) {
        await teamNameAliasesService.upsert(leagueId, aliasItems)
        void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'team-name-aliases'] })
      }

      // Keep original CSV names so the API can resolve via aliases and learn them.
      const rebuilt = rebuildFixtureCsv(importType, parsedRows, (csvName) => {
        const row = teamMappings.find((m) => m.csvName.toLowerCase() === csvName.toLowerCase())
        if (!row || row.action !== 'match' || !row.teamId) return null
        return csvName
      })
      if (rebuilt.errors.length > 0 || !rebuilt.csvText.trim()) {
        setError(rebuilt.errors.join('\n') || 'No hay filas válidas para importar.')
        return
      }

      const res = await fixturesService.importFixtures(leagueId, {
        seasonId,
        divisionId,
        csvText: rebuilt.csvText,
      })
      if (res.errors.length > 0) {
        setError(res.errors.join('\n'))
        return
      }
      onSuccess()
      reset()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.importModal.errorImporting'))
    } finally {
      setImporting(false)
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>{t('fixtures.importModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('fixtures.importModal.instructions')}
        </Typography>
        {divisionName && (
          <Typography variant="body2" sx={{ mb: 1.5 }}>
            División: <strong>{divisionName}</strong>
            {divisionTeams.length > 0 ? ` · ${divisionTeams.length} equipos` : ''}
          </Typography>
        )}

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mb: 1.5 }} alignItems={{ sm: 'center' }}>
          <input
            ref={fileRef}
            type="file"
            accept=".csv,text/csv,.txt"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) void handleFile(file)
            }}
          />
          <Button
            variant="outlined"
            startIcon={<UploadFileIcon />}
            disabled={setupLoading || analyzing || importing}
            onClick={() => fileRef.current?.click()}
          >
            {t('fixtures.importModal.uploadButton')}
          </Button>
          {fileName && (
            <Typography variant="caption" color="text.secondary">
              {fileName}
            </Typography>
          )}
        </Stack>

        <TextField
          fullWidth
          multiline
          minRows={5}
          maxRows={10}
          placeholder={CSV_PLACEHOLDER}
          value={csvText}
          onChange={(e) => {
            setCsvText(e.target.value)
            // Invalidate analysis when text changes manually
            setParsedRows(null)
            setTeamMappings(null)
            setPreviewRows(null)
            setImportType(null)
            setParseErrors([])
          }}
          variant="outlined"
          margin="dense"
          sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}
          disabled={analyzing || importing}
        />

        {(setupLoading || analyzing) && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1.5 }}>
            <CircularProgress size={20} />
            <Typography variant="body2">
              {analyzing ? t('fixtures.importModal.analyzing') : t('fixtures.importModal.loadingTeams')}
            </Typography>
          </Box>
        )}

        {error && (
          <Alert severity="error" sx={{ mt: 1.5, whiteSpace: 'pre-wrap' }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {parseErrors.length > 0 && (
          <Alert severity="warning" sx={{ mt: 1.5 }}>
            {parseErrors.map((err, i) => (
              <div key={i}>{err}</div>
            ))}
          </Alert>
        )}

        {showReview && teamMappings && (
          <Box sx={{ mt: 2 }}>
            <Alert severity="info" sx={{ mb: 1.5 }}>
              {t('fixtures.importModal.reviewHint', { count: reviewHintCount })}
            </Alert>
            {missingTeamCount === 0 && (
              <Button size="small" variant="outlined" sx={{ mb: 1.5 }} onClick={confirmSuggestedMappings}>
                {t('fixtures.importModal.confirmSuggestions')}
              </Button>
            )}
            <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 280 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>{t('fixtures.importModal.csvName')}</TableCell>
                    <TableCell>{t('fixtures.importModal.mappedTeam')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {teamMappings.map((row, mappingIndex) => (
                    <TableRow key={`${row.csvName}-${mappingIndex}`} selected={row.needsReview || mappingUnresolved(row)}>
                      <TableCell>
                        <Typography variant="body2">{row.csvName}</Typography>
                        {row.score > 0 && (
                          <Typography variant="caption" color="text.secondary">
                            {(row.score * 100).toFixed(0)}%
                          </Typography>
                        )}
                        {row.needsReview && <Chip size="small" label="Revisar" sx={{ ml: 1 }} />}
                      </TableCell>
                      <TableCell sx={{ minWidth: 240 }}>
                        <FormControl fullWidth size="small">
                          <Select
                            value={row.action === 'match' && row.teamId ? row.teamId : MISSING}
                            onChange={(e) => {
                              const v = e.target.value
                              updateMapping(mappingIndex, v === MISSING ? null : v)
                            }}
                          >
                            <MenuItem value={MISSING}>
                              <em>{t('fixtures.importModal.chooseTeam')}</em>
                            </MenuItem>
                            {(row.candidates.length
                              ? row.candidates
                              : divisionTeams.map((tm) => ({
                                  teamId: tm.id,
                                  label: displayName(tm),
                                  score: 0,
                                }))
                            ).map((c) => (
                              <MenuItem key={c.teamId} value={c.teamId}>
                                {c.label}
                                {c.score > 0 ? ` (${Math.round(c.score * 100)}%)` : ''}
                              </MenuItem>
                            ))}
                            {row.candidates.length > 0 &&
                              divisionTeams
                                .filter((tm) => !row.candidates.some((c) => c.teamId === tm.id))
                                .map((tm) => (
                                  <MenuItem key={tm.id} value={tm.id}>
                                    {displayName(tm)}
                                  </MenuItem>
                                ))}
                          </Select>
                        </FormControl>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        )}

        {previewRows && matchPreviewRows.length > 0 && importType && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" color="text.secondary">
              {t('fixtures.importModal.formatDetected')} {importType} · {matchPreviewRows.length}{' '}
              {t('fixtures.importModal.matches')}
              {inferredByes.length > 0
                ? ` · ${inferredByes.length} ${t('fixtures.importModal.byes')}`
                : ''}
            </Typography>
            <Alert severity="success" sx={{ mt: 1, mb: 1 }}>
              {t('fixtures.importModal.readyHint')}
            </Alert>
            {inferredByes.length > 0 && (
              <Alert severity="info" sx={{ mb: 1, whiteSpace: 'pre-wrap' }}>
                {t('fixtures.importModal.byesHint')}
                {'\n'}
                {inferredByes
                  .slice(0, 30)
                  .map((b) => `Fecha ${b.round}: ${b.teamName}`)
                  .join('\n')}
                {inferredByes.length > 30 ? `\n… y ${inferredByes.length - 30} más.` : ''}
              </Alert>
            )}
            <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 280 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>{t('fixtures.cols.round')}</TableCell>
                    {importType !== 'Simple' && <TableCell>{t('fixtures.cols.date')}</TableCell>}
                    {importType === 'Full' && (
                      <>
                        <TableCell>{t('fixtures.cols.time')}</TableCell>
                        <TableCell>{t('fixtures.cols.field')}</TableCell>
                      </>
                    )}
                    <TableCell>{t('fixtures.cols.home')}</TableCell>
                    <TableCell>{t('fixtures.cols.away')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {matchPreviewRows.map((row, i) => (
                    <TableRow key={i}>
                      <TableCell>{row.round}</TableCell>
                      {importType !== 'Simple' && <TableCell>{row.date ?? '—'}</TableCell>}
                      {importType === 'Full' && (
                        <>
                          <TableCell>{row.time ?? '—'}</TableCell>
                          <TableCell>{row.field ?? '—'}</TableCell>
                        </>
                      )}
                      <TableCell>{row.homeTeam}</TableCell>
                      <TableCell>{row.awayTeam}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        )}

        {teamMappings && !showReview && matchPreviewRows.length === 0 && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            No quedaron partidos válidos para importar.
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={importing}>
          {t('common.cancel')}
        </Button>
        <Button
          onClick={handleAnalyze}
          disabled={!csvText.trim() || analyzing || importing || setupLoading}
          variant="outlined"
        >
          {t('fixtures.importModal.previewButton')}
        </Button>
        <Button onClick={() => void handleImport()} disabled={!canImport} variant="contained">
          {importing ? <CircularProgress size={24} /> : t('fixtures.importModal.importButton')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
