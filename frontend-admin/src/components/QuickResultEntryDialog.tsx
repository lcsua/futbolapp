import { useEffect, useMemo, useRef, useState } from 'react'
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
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { matchesService, type MatchListItem } from '../api/matches'
import type { Division, Season } from '../api/types'

type Draft = { home: string; away: string }
type Step = 'edit' | 'preview'

type ReadyRow = {
  match: MatchListItem
  home: number
  away: number
}

type QuickResultEntryDialogProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  seasons: Season[]
  divisions: Division[]
  initialSeasonId: string
  initialDivisionId: string
  initialRound: string
  onSaved: (summary: string) => void
}

function scoreText(value: number | null): string {
  return value == null ? '' : String(value)
}

function parseScore(raw: string): number | null {
  const text = raw.trim()
  if (!/^\d{1,2}$/.test(text)) return null
  return Number(text)
}

function sameScore(match: MatchListItem, home: number, away: number): boolean {
  return match.homeScore === home && match.awayScore === away
}

export function QuickResultEntryDialog({
  open,
  onClose,
  leagueId,
  seasons,
  divisions,
  initialSeasonId,
  initialDivisionId,
  initialRound,
  onSaved,
}: QuickResultEntryDialogProps) {
  const theme = useTheme()
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm'))
  const queryClient = useQueryClient()
  const inputRefs = useRef<Array<HTMLInputElement | null>>([])
  const [seasonId, setSeasonId] = useState(initialSeasonId)
  const [divisionId, setDivisionId] = useState(initialDivisionId)
  const [round, setRound] = useState(initialRound)
  const [drafts, setDrafts] = useState<Record<string, Draft>>({})
  const [draftKey, setDraftKey] = useState('')
  const [step, setStep] = useState<Step>('edit')
  const [saving, setSaving] = useState(false)
  const [saveProgress, setSaveProgress] = useState('')
  const [saveError, setSaveError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setSeasonId(initialSeasonId)
    setDivisionId(initialDivisionId)
    setRound(initialRound)
    setDraftKey('')
    setStep('edit')
    setSaving(false)
    setSaveProgress('')
    setSaveError(null)
  }, [open, initialSeasonId, initialDivisionId, initialRound])

  const selectedSeason = seasons.find((season) => season.id === seasonId)
  const seasonClosed = !!selectedSeason && selectedSeason.isActive === false

  const { data: matchesData, isLoading, isError } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, divisionId || null],
    queryFn: ({ signal }) =>
      matchesService.getMatches(
        leagueId,
        { seasonId, divisionId: divisionId || undefined },
        signal,
      ),
    enabled: open && !!leagueId && !!seasonId,
  })

  const roundNumbers = useMemo(
    () => [...new Set((matchesData?.rounds ?? []).map((group) => group.roundNumber))].sort((a, b) => a - b),
    [matchesData],
  )

  const groups = useMemo(() => {
    const selected = round === '' ? null : Number(round)
    if (selected == null || Number.isNaN(selected)) return []
    return (matchesData?.rounds ?? [])
      .filter((group) => group.roundNumber === selected)
      .map((group) => ({
        ...group,
        matches: [...group.matches].sort((a, b) => {
          const byTime = (a.kickoffTime || '').localeCompare(b.kickoffTime || '')
          if (byTime !== 0) return byTime
          return a.homeTeamName.localeCompare(b.homeTeamName, 'es')
        }),
      }))
  }, [matchesData, round])

  const matches = useMemo(() => groups.flatMap((group) => group.matches), [groups])

  useEffect(() => {
    if (!open || !round || !matchesData) return
    const key = `${seasonId}|${divisionId}|${round}|${matches.map((match) => match.id).join(',')}`
    if (draftKey === key) return
    const next: Record<string, Draft> = {}
    for (const match of matches) {
      next[match.id] = { home: scoreText(match.homeScore), away: scoreText(match.awayScore) }
    }
    setDrafts(next)
    setDraftKey(key)
    setStep('edit')
  }, [open, seasonId, divisionId, round, matches, matchesData, draftKey])

  const { ready, incomplete } = useMemo(() => {
    const readyRows: ReadyRow[] = []
    const incompleteRows: MatchListItem[] = []
    for (const match of matches) {
      const draft = drafts[match.id] ?? { home: '', away: '' }
      const homeEmpty = draft.home.trim() === ''
      const awayEmpty = draft.away.trim() === ''
      if (homeEmpty && awayEmpty) continue
      const home = parseScore(draft.home)
      const away = parseScore(draft.away)
      if (home == null || away == null) {
        incompleteRows.push(match)
        continue
      }
      if (!sameScore(match, home, away)) {
        readyRows.push({ match, home, away })
      }
    }
    return { ready: readyRows, incomplete: incompleteRows }
  }, [matches, drafts])

  const updateDraft = (matchId: string, side: keyof Draft, value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 2)
    setDrafts((prev) => ({
      ...prev,
      [matchId]: { ...(prev[matchId] ?? { home: '', away: '' }), [side]: digits },
    }))
  }

  const focusAt = (index: number) => {
    const input = inputRefs.current[index]
    if (input?.isConnected) {
      input.focus()
      input.select()
      return
    }
    document.getElementById('quick-results-review')?.focus()
  }

  const handleClose = () => {
    if (saving) return
    onClose()
  }

  const saveAll = async () => {
    if (ready.length === 0 || incomplete.length > 0 || seasonClosed) return
    setSaving(true)
    setSaveError(null)
    let saved = 0
    try {
      for (const row of ready) {
        setSaveProgress(`${saved + 1} de ${ready.length}`)
        await matchesService.updateResult(leagueId, row.match.id, {
          homeScore: row.home,
          awayScore: row.away,
          status: 'COMPLETED',
        })
        saved += 1
      }
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
      const lines = ready.map(
        (row) => `${row.match.divisionName}: ${row.match.homeTeamName} ${row.home}–${row.away} ${row.match.awayTeamName}`,
      )
      onSaved(`Carga rápida: ${saved} resultado(s).\n${lines.join('\n')}`)
      onClose()
    } catch (error) {
      setSaveError(
        error instanceof Error
          ? `Se guardaron ${saved} y falló el siguiente. ${error.message}`
          : `Se guardaron ${saved} y falló el siguiente.`,
      )
    } finally {
      setSaving(false)
      setSaveProgress('')
    }
  }

  let inputIndex = 0

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth fullScreen={fullScreen}>
      <DialogTitle>Carga rápida de resultados</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Elegí temporada y fecha. La división es opcional. Enter pasa al siguiente casillero. Después revisás la
          vista previa y se guardan solo los marcadores que cambiaste.
        </Typography>

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: '1.4fr 1fr 0.7fr' },
            gap: 1.5,
            mb: 2,
          }}
        >
          <FormControl size="small" disabled={saving}>
            <InputLabel id="quick-season">Temporada</InputLabel>
            <Select
              labelId="quick-season"
              label="Temporada"
              value={seasonId}
              onChange={(event: SelectChangeEvent) => {
                setSeasonId(event.target.value)
                setRound('')
              }}
            >
              {seasons.map((season) => (
                <MenuItem key={season.id} value={season.id}>
                  {season.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" disabled={saving || !seasonId}>
            <InputLabel id="quick-division">División</InputLabel>
            <Select
              labelId="quick-division"
              label="División"
              value={divisionId}
              onChange={(event: SelectChangeEvent) => setDivisionId(event.target.value)}
            >
              <MenuItem value="">
                <em>Todas</em>
              </MenuItem>
              {divisions.map((division) => (
                <MenuItem key={division.id} value={division.id}>
                  {division.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" disabled={saving || !seasonId || roundNumbers.length === 0}>
            <InputLabel id="quick-round">Fecha</InputLabel>
            <Select
              labelId="quick-round"
              label="Fecha"
              value={roundNumbers.includes(Number(round)) ? round : ''}
              onChange={(event: SelectChangeEvent) => setRound(event.target.value)}
            >
              <MenuItem value="">
                <em>Elegir</em>
              </MenuItem>
              {roundNumbers.map((value) => (
                <MenuItem key={value} value={String(value)}>
                  {value}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        {seasonClosed && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            Esta temporada está cerrada. No se pueden guardar resultados desde acá.
          </Alert>
        )}

        {isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={24} />
          </Box>
        )}
        {isError && <Alert severity="error">No se pudieron cargar los partidos.</Alert>}
        {!isLoading && seasonId && round === '' && (
          <Typography color="text.secondary">Elegí la fecha para ver los partidos.</Typography>
        )}
        {!isLoading && round !== '' && matches.length === 0 && (
          <Typography color="text.secondary">No hay partidos en esa fecha.</Typography>
        )}

        {step === 'edit' && !isLoading && matches.length > 0 && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {incomplete.length > 0 && (
              <Alert severity="warning">
                Hay partidos con un solo marcador. Completá los dos o dejálos vacíos para no tocarlos.
              </Alert>
            )}
            {groups.map((group) => (
              <Box key={`${group.divisionName}-${group.roundNumber}`}>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Fecha {group.roundNumber} — {group.divisionName}
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {group.matches.map((match) => {
                    const draft = drafts[match.id] ?? { home: '', away: '' }
                    const homeIndex = inputIndex
                    inputIndex += 1
                    const awayIndex = inputIndex
                    inputIndex += 1
                    return (
                      <Box
                        key={match.id}
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: '1fr 72px 16px 72px 1fr',
                            sm: 'minmax(0, 1fr) 64px 12px 64px minmax(0, 1fr)',
                          },
                          gridTemplateAreas: {
                            xs: `"homeName homeName homeName homeName homeName"
                                 ". home dash away ."
                                 "awayName awayName awayName awayName awayName"`,
                            sm: `"homeName home dash away awayName"`,
                          },
                          alignItems: 'center',
                          columnGap: 1,
                          rowGap: 0.5,
                          py: { xs: 1.25, sm: 0 },
                          borderBottom: { xs: '1px solid', sm: 'none' },
                          borderColor: 'divider',
                        }}
                      >
                        <Typography
                          variant="body2"
                          title={match.homeTeamName}
                          sx={{
                            gridArea: 'homeName',
                            fontWeight: { xs: 600, sm: 400 },
                            textAlign: { xs: 'center', sm: 'right' },
                            whiteSpace: { xs: 'normal', sm: 'nowrap' },
                            overflow: { sm: 'hidden' },
                            textOverflow: { sm: 'ellipsis' },
                          }}
                        >
                          {match.homeTeamName}
                        </Typography>
                        <TextField
                          size="small"
                          value={draft.home}
                          placeholder="–"
                          disabled={saving}
                          inputRef={(node) => {
                            inputRefs.current[homeIndex] = node
                          }}
                          onChange={(event) => updateDraft(match.id, 'home', event.target.value)}
                          onKeyDown={(event) => {
                            if (event.key !== 'Enter') return
                            event.preventDefault()
                            focusAt(homeIndex + 1)
                          }}
                          sx={{ gridArea: 'home' }}
                          slotProps={{
                            htmlInput: {
                              inputMode: 'numeric',
                              'aria-label': `Goles de ${match.homeTeamName}`,
                              style: { textAlign: 'center', padding: '8px 0' },
                            },
                          }}
                        />
                        <Typography variant="body2" color="text.secondary" sx={{ gridArea: 'dash', textAlign: 'center' }}>
                          –
                        </Typography>
                        <TextField
                          size="small"
                          value={draft.away}
                          placeholder="–"
                          disabled={saving}
                          inputRef={(node) => {
                            inputRefs.current[awayIndex] = node
                          }}
                          onChange={(event) => updateDraft(match.id, 'away', event.target.value)}
                          onKeyDown={(event) => {
                            if (event.key !== 'Enter') return
                            event.preventDefault()
                            focusAt(awayIndex + 1)
                          }}
                          sx={{ gridArea: 'away' }}
                          slotProps={{
                            htmlInput: {
                              inputMode: 'numeric',
                              'aria-label': `Goles de ${match.awayTeamName}`,
                              style: { textAlign: 'center', padding: '8px 0' },
                            },
                          }}
                        />
                        <Typography
                          variant="body2"
                          title={match.awayTeamName}
                          sx={{
                            gridArea: 'awayName',
                            fontWeight: { xs: 600, sm: 400 },
                            textAlign: { xs: 'center', sm: 'left' },
                            whiteSpace: { xs: 'normal', sm: 'nowrap' },
                            overflow: { sm: 'hidden' },
                            textOverflow: { sm: 'ellipsis' },
                          }}
                        >
                          {match.awayTeamName}
                        </Typography>
                      </Box>
                    )
                  })}
                </Box>
              </Box>
            ))}
          </Box>
        )}

        {step === 'preview' && (
          <Box>
            <Alert severity="info" sx={{ mb: 1.5 }}>
              Se van a guardar {ready.length} resultado(s) como finalizados. Los que no cambiaste quedan igual.
            </Alert>
            {ready.map((row) => {
              const previous =
                row.match.homeScore != null && row.match.awayScore != null
                  ? `antes ${row.match.homeScore}–${row.match.awayScore}`
                  : 'sin resultado'
              return (
                <Typography key={row.match.id} variant="body2" sx={{ py: 0.5 }}>
                  {row.match.divisionName}: {row.match.homeTeamName} {row.home}–{row.away} {row.match.awayTeamName}
                  {' '}
                  <Typography component="span" variant="caption" color="text.secondary">
                    ({previous})
                  </Typography>
                </Typography>
              )
            })}
          </Box>
        )}

        {saveError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {saveError}
          </Alert>
        )}
        {saving && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <CircularProgress size={18} />
            <Typography variant="body2">Guardando {saveProgress}</Typography>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={saving}>
          Cancelar
        </Button>
        {step === 'preview' && (
          <Button onClick={() => setStep('edit')} disabled={saving}>
            Volver a editar
          </Button>
        )}
        {step === 'edit' ? (
          <Button
            id="quick-results-review"
            variant="contained"
            disabled={ready.length === 0 || incomplete.length > 0 || seasonClosed || isLoading}
            onClick={() => setStep('preview')}
          >
            Ver vista previa
          </Button>
        ) : (
          <Button variant="contained" disabled={saving || ready.length === 0 || seasonClosed} onClick={() => void saveAll()}>
            {saving ? 'Guardando…' : 'Guardar resultados'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}
