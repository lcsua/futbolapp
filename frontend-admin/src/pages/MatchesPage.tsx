import React, { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Checkbox,
  FormControl,
  FormControlLabel,
  InputLabel,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Select,
  TextField,
  Typography,
  CircularProgress,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import VisibilityIcon from '@mui/icons-material/Visibility'
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import PlaceIcon from '@mui/icons-material/Place'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink, useParams } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { matchesService, matchStatusLabel, type MatchListItem } from '../api/matches'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { fieldsService } from '../api/fields'
import type { Field } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'
import { MatchResultModal } from '../components/MatchResultModal'
import { ImportFixtureModal } from '../components/ImportFixtureModal'
import { CrestImg } from '../components/CrestImg'
import { ImportMatchResultsModal } from '../components/ImportMatchResultsModal'
import { ImportResultsImageModal } from '../components/ImportResultsImageModal'
import { ImportScheduleModal } from '../components/ImportScheduleModal'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import ScoreboardIcon from '@mui/icons-material/Scoreboard'
import PhotoLibraryIcon from '@mui/icons-material/PhotoLibrary'
import ScheduleIcon from '@mui/icons-material/Schedule'
import MoreVertIcon from '@mui/icons-material/MoreVert'

const MATCH_GRID_SX = {
  display: 'grid',
  gridTemplateColumns: {
    xs: '1fr',
    sm: 'repeat(auto-fill, minmax(min(100%, 340px), 1fr))',
  },
  gap: { xs: 1.5, sm: 2 },
} as const

const FILTER_CONTROL_SX = { minWidth: 0, width: '100%' } as const

export function MatchesPage() {
  const { t } = useTranslation()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const leagueId = useLeagueId()
  const { leagueId: leagueIdInPath } = useParams<{ leagueId?: string }>()
  const [seasonId, setSeasonId] = useState('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [round, setRound] = useState<string>('')
  const [teamId, setTeamId] = useState<string>('')
  const [groupByField, setGroupByField] = useState(false)
  const [actionsAnchor, setActionsAnchor] = useState<HTMLElement | null>(null)
  const [resultModalMatch, setResultModalMatch] = useState<MatchListItem | null>(null)
  const [importModalOpen, setImportModalOpen] = useState(false)
  const [importResultsOpen, setImportResultsOpen] = useState(false)
  const [importImageOpen, setImportImageOpen] = useState(false)
  const [resultsSession, setResultsSession] = useState(0)
  const [seededResultsCsv, setSeededResultsCsv] = useState<string | null>(null)
  const [seededResultsLabel, setSeededResultsLabel] = useState<string | null>(null)
  const [importScheduleOpen, setImportScheduleOpen] = useState(false)
  const [importResultsMsg, setImportResultsMsg] = useState<string | null>(null)
  const [clearError, setClearError] = useState<string | null>(null)
  const [matchToDelete, setMatchToDelete] = useState<MatchListItem | null>(null)
  const [scheduleModalMatch, setScheduleModalMatch] = useState<MatchListItem | null>(null)
  const queryClient = useQueryClient()

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: divisions = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: fields = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'fields'],
    queryFn: ({ signal }) => fieldsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: matchesData, isLoading: matchesLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, divisionId || null],
    queryFn: ({ signal }) =>
      matchesService.getMatches(
        leagueId!,
        {
          seasonId,
          divisionId: divisionId || undefined,
        },
        signal
      ),
    enabled: !!leagueId && !!seasonId,
    staleTime: 0,
    refetchOnMount: 'always',
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => {
    setSeasonId(e.target.value)
    setDivisionId('')
    setRound('')
    setTeamId('')
  }
  const handleDivisionChange = (e: SelectChangeEvent<string>) => {
    setDivisionId(e.target.value)
    setTeamId('')
  }
  const handleRoundChange = (e: SelectChangeEvent<string>) => setRound(e.target.value)
  const handleTeamChange = (e: SelectChangeEvent<string>) => setTeamId(e.target.value)

  const allRounds = matchesData?.rounds ?? []
  const roundNumbers = [...new Set(allRounds.map((r) => r.roundNumber))].sort((a, b) => a - b)
  const selectedSeason = seasons.find((s) => s.id === seasonId)
  const seasonClosed = !!selectedSeason && selectedSeason.isActive === false

  const teams = React.useMemo(() => {
    const seen = new Map<string, string>()
    for (const group of allRounds) {
      for (const m of group.matches) {
        if (!seen.has(m.homeTeamId)) seen.set(m.homeTeamId, m.homeTeamName)
        if (!seen.has(m.awayTeamId)) seen.set(m.awayTeamId, m.awayTeamName)
      }
    }
    return Array.from(seen.entries()).sort((a, b) => a[1].localeCompare(b[1]))
  }, [allRounds])

  const rounds = React.useMemo(() => {
    const selectedRound = round === '' ? null : parseInt(round, 10)
    return allRounds
      .filter((g) => selectedRound == null || g.roundNumber === selectedRound)
      .map((g) => ({
        ...g,
        matches: teamId
          ? g.matches.filter((m) => m.homeTeamId === teamId || m.awayTeamId === teamId)
          : g.matches,
      }))
      .filter((g) => g.matches.length > 0)
  }, [allRounds, round, teamId])

  const matchesWithResults = React.useMemo(
    () =>
      rounds
        .flatMap((g) => g.matches)
        .filter(
          (m) =>
            m.homeScore != null ||
            m.awayScore != null ||
            (m.status && m.status.toUpperCase() !== 'SCHEDULED'),
        ),
    [rounds],
  )

  const displayedMatches = React.useMemo(() => rounds.flatMap((g) => g.matches), [rounds])

  const fieldGroups = React.useMemo(() => {
    const grouped = new Map<string, MatchListItem[]>()
    for (const match of displayedMatches) {
      const fieldName = match.fieldName.trim() || 'Sin cancha'
      const group = grouped.get(fieldName) ?? []
      group.push(match)
      grouped.set(fieldName, group)
    }

    return Array.from(grouped.entries())
      .map(([fieldName, matches]) => ({
        fieldName,
        matches: [...matches].sort((a, b) => {
          const byTime = (a.kickoffTime || '').localeCompare(b.kickoffTime || '')
          if (byTime !== 0) return byTime
          return a.divisionName.localeCompare(b.divisionName)
        }),
      }))
      .sort((a, b) => {
        if (a.fieldName === 'Sin cancha') return 1
        if (b.fieldName === 'Sin cancha') return -1
        return a.fieldName.localeCompare(b.fieldName)
      })
  }, [displayedMatches])

  const showFieldGroups = groupByField && round !== '' && displayedMatches.length > 0

  const canClearRoundResults =
    !!leagueId &&
    !!seasonId &&
    !!divisionId &&
    round !== '' &&
    !seasonClosed &&
    matchesWithResults.length > 0

  const clearRoundMutation = useMutation({
    mutationFn: () =>
      matchesService.clearRoundResults(leagueId!, {
        seasonId,
        divisionId,
        round: parseInt(round, 10),
      }),
    onSuccess: (res) => {
      setClearError(null)
      setImportResultsMsg(
        res.clearedCount > 0
          ? `Se borraron resultados de ${res.clearedCount} partido(s). Los partidos siguen en el fixture.`
          : 'No había resultados para borrar en esa fecha.',
      )
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
    },
    onError: (err) => {
      setClearError(err instanceof Error ? err.message : 'No se pudieron borrar los resultados')
    },
  })

  const handleClearRoundResults = () => {
    if (!canClearRoundResults) return
    const divisionName = divisions.find((d) => d.id === divisionId)?.name ?? 'división'
    if (
      !window.confirm(
        `¿Borrar los resultados cargados de la fecha ${round} (${divisionName})?\nSe quitan marcadores e incidentes; los partidos quedan en el fixture.`,
      )
    ) {
      return
    }
    clearRoundMutation.mutate()
  }

  const deleteMatchMutation = useMutation({
    mutationFn: (matchId: string) => matchesService.deleteMatch(leagueId!, matchId),
    onSuccess: () => {
      setClearError(null)
      setMatchToDelete(null)
      setImportResultsMsg('Partido eliminado del fixture.')
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
    },
    onError: (err) => {
      setClearError(err instanceof Error ? err.message : 'No se pudo eliminar el partido')
    },
  })

  const updateScheduleMutation = useMutation({
    mutationFn: ({
      matchId,
      startTime,
      fieldId,
    }: {
      matchId: string
      startTime: string
      fieldId: string
    }) => matchesService.updateSchedule(leagueId!, matchId, { startTime, fieldId }),
    onSuccess: () => {
      setClearError(null)
      setScheduleModalMatch(null)
      setImportResultsMsg('Horario/cancha actualizado.')
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
    },
    onError: (err) => {
      setClearError(err instanceof Error ? err.message : 'No se pudo actualizar horario/cancha')
    },
  })

  const handleDeleteMatch = (match: MatchListItem) => {
    if (seasonClosed) return
    setMatchToDelete(match)
  }

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">{t('matches.goToLeagues')}</Button>}>
        {t('matches.noLeague')}
      </Alert>
    )
  }

  return (
    <Box sx={{ minWidth: 0, overflowX: 'hidden' }}>
      <Button component={RouterLink} to="/seasons" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        {t('matches.backToSeasons')}
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        {t('matches.title')}
      </Typography>

      {seasonClosed && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {t('seasons.closedWarning')}
        </Alert>
      )}

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, mb: 3, minWidth: 0 }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, minmax(0, 1fr))' },
            gap: 1.5,
          }}
        >
          <FormControl size="small" sx={FILTER_CONTROL_SX} disabled={seasonsLoading}>
            <InputLabel id="season-label">{t('matches.season')}</InputLabel>
            <Select
              labelId="season-label"
              label={t('matches.season')}
              value={seasonId}
              onChange={handleSeasonChange}
            >
              <MenuItem value="">
                <em>{t('matches.selectSeason')}</em>
              </MenuItem>
              {seasons.map((s) => (
                <MenuItem key={s.id} value={s.id}>
                  {s.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={FILTER_CONTROL_SX} disabled={!seasonId}>
            <InputLabel id="division-label">{t('matches.division')}</InputLabel>
            <Select
              labelId="division-label"
              label={t('matches.division')}
              value={divisionId}
              onChange={handleDivisionChange}
            >
              <MenuItem value="">
                <em>{t('matches.all')}</em>
              </MenuItem>
              {divisions.map((d) => (
                <MenuItem key={d.id} value={d.id}>
                  {d.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={FILTER_CONTROL_SX} disabled={!seasonId}>
            <InputLabel id="round-label">{t('matches.round')}</InputLabel>
            <Select
              labelId="round-label"
              label={t('matches.round')}
              value={round}
              onChange={handleRoundChange}
            >
              <MenuItem value="">
                <em>{t('matches.all')}</em>
              </MenuItem>
              {roundNumbers.map((r) => (
                <MenuItem key={r} value={String(r)}>
                  {r}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={FILTER_CONTROL_SX} disabled={!seasonId || allRounds.length === 0}>
            <InputLabel id="team-label">{t('matches.team')}</InputLabel>
            <Select
              labelId="team-label"
              label={t('matches.team')}
              value={teamId}
              onChange={handleTeamChange}
            >
              <MenuItem value="">
                <em>{t('matches.all')}</em>
              </MenuItem>
              {teams.map(([id, name]) => (
                <MenuItem key={id} value={id}>
                  {name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        <FormControlLabel
          sx={{ m: 0, alignSelf: 'flex-start' }}
          control={
            <Checkbox
              checked={groupByField}
              onChange={(e) => setGroupByField(e.target.checked)}
              disabled={!seasonId || round === ''}
            />
          }
          label={t('matches.groupByField')}
        />

        {isMobile ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Button
              variant="contained"
              startIcon={<ScoreboardIcon />}
              onClick={() => {
                setSeededResultsCsv(null)
                setSeededResultsLabel(null)
                setResultsSession((n) => n + 1)
                setImportResultsOpen(true)
              }}
              disabled={seasonClosed || !seasonId}
              fullWidth
            >
              {t('matches.importResultsCsv')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<PhotoLibraryIcon />}
              onClick={() => setImportImageOpen(true)}
              disabled={seasonClosed || !seasonId}
              fullWidth
            >
              {t('matches.importResultsImage')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<MoreVertIcon />}
              onClick={(e) => setActionsAnchor(e.currentTarget)}
              disabled={!seasonId}
              fullWidth
            >
              {t('matches.moreActions')}
            </Button>
            <Menu
              anchorEl={actionsAnchor}
              open={!!actionsAnchor}
              onClose={() => setActionsAnchor(null)}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
            >
              <MenuItem
                disabled={seasonClosed || !seasonId || !divisionId}
                onClick={() => {
                  setActionsAnchor(null)
                  setImportModalOpen(true)
                }}
              >
                <ListItemIcon>
                  <UploadFileIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>{t('matches.importFixture')}</ListItemText>
              </MenuItem>
              <MenuItem
                disabled={seasonClosed || !seasonId}
                onClick={() => {
                  setActionsAnchor(null)
                  setImportScheduleOpen(true)
                }}
              >
                <ListItemIcon>
                  <ScheduleIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>{t('matches.importSchedule')}</ListItemText>
              </MenuItem>
              <MenuItem
                disabled={!canClearRoundResults || clearRoundMutation.isPending}
                onClick={() => {
                  setActionsAnchor(null)
                  handleClearRoundResults()
                }}
              >
                <ListItemIcon>
                  <DeleteSweepIcon fontSize="small" color="error" />
                </ListItemIcon>
                <ListItemText>
                  {clearRoundMutation.isPending
                    ? t('matches.clearingRoundResults')
                    : t('matches.clearRoundResults')}
                </ListItemText>
              </MenuItem>
            </Menu>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
            <Button
              variant="outlined"
              startIcon={<UploadFileIcon />}
              onClick={() => setImportModalOpen(true)}
              disabled={seasonClosed || !seasonId || !divisionId}
            >
              {t('matches.importFixture')}
            </Button>
            <Button
              variant="contained"
              startIcon={<ScoreboardIcon />}
              onClick={() => {
                setSeededResultsCsv(null)
                setSeededResultsLabel(null)
                setResultsSession((n) => n + 1)
                setImportResultsOpen(true)
              }}
              disabled={seasonClosed || !seasonId}
            >
              {t('matches.importResultsCsv')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<PhotoLibraryIcon />}
              onClick={() => setImportImageOpen(true)}
              disabled={seasonClosed || !seasonId}
            >
              {t('matches.importResultsImage')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<ScheduleIcon />}
              onClick={() => setImportScheduleOpen(true)}
              disabled={seasonClosed || !seasonId}
            >
              {t('matches.importSchedule')}
            </Button>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteSweepIcon />}
              onClick={handleClearRoundResults}
              disabled={!canClearRoundResults || clearRoundMutation.isPending}
            >
              {clearRoundMutation.isPending
                ? t('matches.clearingRoundResults')
                : t('matches.clearRoundResults')}
            </Button>
          </Box>
        )}
      </Box>

      {clearError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setClearError(null)}>
          {clearError}
        </Alert>
      )}

      {importResultsMsg && (
        <Alert
          id="import-results-summary"
          severity={importResultsMsg.includes('no creado') ? 'warning' : 'success'}
          sx={{ mb: 2, whiteSpace: 'pre-wrap' }}
          onClose={() => setImportResultsMsg(null)}
        >
          {importResultsMsg}
        </Alert>
      )}

      {matchesLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!matchesLoading && seasonId && rounds.length === 0 && (
        <Typography color="text.secondary">{t('matches.noMatches')}</Typography>
      )}

      {!matchesLoading && rounds.length > 0 && showFieldGroups && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          <Typography variant="subtitle1" fontWeight={600}>
            Fecha {round} por cancha
          </Typography>
          {fieldGroups.map((group) => (
            <Box key={group.fieldName}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 1 }}>
                <PlaceIcon color="primary" fontSize="small" />
                <Typography variant="subtitle2" fontWeight={700}>
                  {group.fieldName}
                </Typography>
                <Chip size="small" label={`${group.matches.length} partido${group.matches.length === 1 ? '' : 's'}`} />
              </Box>
              <Box sx={MATCH_GRID_SX}>
                {group.matches.map((m) => (
                  <MatchCard
                    key={m.id}
                    match={m}
                    matchDetailPath={leagueIdInPath ? `/leagues/${leagueIdInPath}/matches/${m.id}` : `/matches/${m.id}`}
                    onEditResult={() => setResultModalMatch(m)}
                    onEditSchedule={() => setScheduleModalMatch(m)}
                    onDelete={() => handleDeleteMatch(m)}
                    canDelete={!seasonClosed}
                    canEditSchedule={!seasonClosed}
                    isDeleting={deleteMatchMutation.isPending && deleteMatchMutation.variables === m.id}
                    metaLabel={`${m.divisionName} · ${m.kickoffTime || '—'}`}
                  />
                ))}
              </Box>
            </Box>
          ))}
        </Box>
      )}

      {!matchesLoading && rounds.length > 0 && !showFieldGroups && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          {rounds.map((group) => (
            <Box key={`${group.roundNumber}-${group.divisionName}`}>
              <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                {t('matches.roundTitle', { round: group.roundNumber, division: group.divisionName })}
              </Typography>
              <Box sx={MATCH_GRID_SX}>
                {group.matches.map((m) => (
                  <MatchCard
                    key={m.id}
                    match={m}
                    matchDetailPath={leagueIdInPath ? `/leagues/${leagueIdInPath}/matches/${m.id}` : `/matches/${m.id}`}
                    onEditResult={() => setResultModalMatch(m)}
                    onEditSchedule={() => setScheduleModalMatch(m)}
                    onDelete={() => handleDeleteMatch(m)}
                    canDelete={!seasonClosed}
                    canEditSchedule={!seasonClosed}
                    isDeleting={deleteMatchMutation.isPending && deleteMatchMutation.variables === m.id}
                    metaLabel={`${m.fieldName || '—'} · ${m.kickoffTime || '—'}`}
                  />
                ))}
              </Box>
            </Box>
          ))}
        </Box>
      )}

      {resultModalMatch && (
        <MatchResultModal
          open
          match={resultModalMatch}
          leagueId={leagueId!}
          seasonClosed={seasonClosed}
          onClose={() => setResultModalMatch(null)}
          onSaved={() => {
            setResultModalMatch(null)
            void Promise.resolve()
          }}
        />
      )}
      {leagueId && seasonId && divisionId && (
        <ImportFixtureModal
          open={importModalOpen}
          onClose={() => setImportModalOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          divisionId={divisionId}
          onSuccess={() => {
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
          }}
        />
      )}
      {leagueId && seasonId && (
        <>
        <ImportResultsImageModal
          open={importImageOpen}
          onClose={() => setImportImageOpen(false)}
          leagueId={leagueId}
          onCsvReady={(csv, label) => {
            setImportImageOpen(false)
            setSeededResultsCsv(csv)
            setSeededResultsLabel(label)
            setResultsSession((n) => n + 1)
            setImportResultsOpen(true)
          }}
        />
        <ImportMatchResultsModal
          key={resultsSession}
          open={importResultsOpen}
          onClose={() => setImportResultsOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          filterDivisionId={divisionId}
          divisions={divisions}
          seedCsv={seededResultsCsv}
          seedLabel={seededResultsLabel}
          onImported={({ updatedCount, createdCount, skippedCount, notCreatedCount, warnings, lines }) => {
            const parts = [
              updatedCount ? `${updatedCount} actualizado(s)` : null,
              createdCount ? `${createdCount} creado(s)` : null,
              skippedCount ? `${skippedCount} omitido(s) (ya tenían resultado)` : null,
              notCreatedCount ? `${notCreatedCount} no creado(s) (la fecha ya tiene fixture)` : null,
            ].filter(Boolean)
            const outcome = parts.length ? parts.join(', ') : 'sin cambios'
            const detail = lines.length ? `\n${lines.join('\n')}` : ''
            const warn = warnings.length ? `\n${warnings.join('\n')}` : ''
            setImportResultsMsg(`Resultados importados (${outcome}).${detail}${warn}`)
            window.setTimeout(() => {
              document.getElementById('import-results-summary')?.scrollIntoView({ block: 'center' })
            }, 0)
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
          }}
        />
        </>
      )}
      {leagueId && seasonId && (
        <ImportScheduleModal
          open={importScheduleOpen}
          onClose={() => setImportScheduleOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          initialDivisionId={divisionId}
          initialRound={round === '' ? '' : parseInt(round, 10)}
          divisions={divisions}
          rounds={roundNumbers}
          seasonClosed={seasonClosed}
          onImported={({ updatedCount, warnings }) => {
            const warn = warnings.length ? ` Advertencias: ${warnings.length}.` : ''
            setImportResultsMsg(`Horarios/canchas: ${updatedCount} actualizado(s).${warn}`)
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
          }}
        />
      )}
      {scheduleModalMatch && (
        <MatchScheduleModal
          open
          match={scheduleModalMatch}
          fields={fields}
          saving={updateScheduleMutation.isPending}
          onClose={() => {
            if (!updateScheduleMutation.isPending) setScheduleModalMatch(null)
          }}
          onSave={(startTime, fieldId) =>
            updateScheduleMutation.mutate({
              matchId: scheduleModalMatch.id,
              startTime,
              fieldId,
            })
          }
        />
      )}

      <Dialog
        open={!!matchToDelete}
        onClose={() => !deleteMatchMutation.isPending && setMatchToDelete(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>{t('matches.deleteConfirmTitle')}</DialogTitle>
        <DialogContent>
          <Alert severity="warning" sx={{ mb: 2 }}>
            {matchToDelete
              ? t('matches.deleteConfirmMatch', {
                  home: matchToDelete.homeTeamName,
                  away: matchToDelete.awayTeamName,
                  round: matchToDelete.roundNumber,
                })
              : null}
          </Alert>
          <Typography variant="body2" sx={{ mb: 1.5 }}>
            {t('matches.deleteConfirmFixture')}
          </Typography>
          <Typography variant="body2" sx={{ mb: 1.5 }}>
            {t('matches.deleteConfirmNoReadd')}
          </Typography>
          <Typography variant="body2">
            {t('matches.deleteConfirmWrongOption')}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setMatchToDelete(null)} disabled={deleteMatchMutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            color="error"
            variant="contained"
            disabled={deleteMatchMutation.isPending || !matchToDelete}
            onClick={() => {
              if (matchToDelete) deleteMatchMutation.mutate(matchToDelete.id)
            }}
          >
            {deleteMatchMutation.isPending ? t('matches.deleting') : t('matches.deleteConfirmAction')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function MatchCard({
  match,
  matchDetailPath,
  onEditResult,
  onEditSchedule,
  onDelete,
  canDelete,
  canEditSchedule,
  isDeleting,
  metaLabel,
}: {
  match: MatchListItem
  matchDetailPath: string
  onEditResult: () => void
  onEditSchedule: () => void
  onDelete: () => void
  canDelete: boolean
  canEditSchedule: boolean
  isDeleting: boolean
  metaLabel: string
}) {
  const { t } = useTranslation()
  const teamNameSx = {
    overflow: 'hidden',
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    wordBreak: 'break-word',
    lineHeight: 1.25,
  } as const

  const actionBtnSx = {
    justifyContent: 'flex-start',
    minWidth: 0,
    px: { xs: 0.75, sm: 1 },
    whiteSpace: 'nowrap',
    lineHeight: 1.2,
    textAlign: 'left',
    '& .MuiButton-startIcon': {
      display: { xs: 'none', sm: 'inline-flex' },
      mr: { sm: 0.5 },
    },
  } as const

  return (
    <Card
      variant="outlined"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        minWidth: 0,
        minHeight: { xs: 0, sm: 130 },
        padding: { xs: 1.5, sm: 2 },
      }}
    >
      <CardContent sx={{ p: 0, flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'space-between', minWidth: 0, '&:last-child': { pb: 0 } }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: 'minmax(0, 1fr) auto minmax(0, 1fr)', sm: '1fr auto 1fr' },
            alignItems: 'center',
            gap: { xs: 0.75, sm: 1.5 },
            minWidth: 0,
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, minWidth: 0 }}>
            {match.homeTeamLogoUrl && (
              <CrestImg src={match.homeTeamLogoUrl} alt="" size={22} />
            )}
            <Typography variant="body2" fontWeight={600} sx={teamNameSx}>
              {match.homeTeamName}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexShrink: 0 }}>
            {String(match.status || '').toUpperCase() === 'SUSPENDED' ? (
              <Chip label={matchStatusLabel('SUSPENDED')} size="small" color="warning" />
            ) : (
              <>
                <Typography variant="h6" component="span" sx={{ minWidth: 20, textAlign: 'center', fontSize: { xs: '1.1rem', sm: '1.25rem' } }}>
                  {match.homeScore ?? '-'}
                </Typography>
                <Typography color="text.secondary">—</Typography>
                <Typography variant="h6" component="span" sx={{ minWidth: 20, textAlign: 'center', fontSize: { xs: '1.1rem', sm: '1.25rem' } }}>
                  {match.awayScore ?? '-'}
                </Typography>
              </>
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, minWidth: 0, justifyContent: 'flex-end' }}>
            <Typography variant="body2" fontWeight={600} sx={{ ...teamNameSx, textAlign: 'right' }}>
              {match.awayTeamName}
            </Typography>
            {match.awayTeamLogoUrl && (
              <CrestImg src={match.awayTeamLogoUrl} alt="" size={22} />
            )}
          </Box>
        </Box>
        <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.75 }}>
          {metaLabel}
        </Typography>
        <Box
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 0.5,
            mt: 1.25,
            '& .MuiButton-root': {
              flex: { xs: '1 1 calc(50% - 4px)', sm: '0 0 auto' },
            },
          }}
        >
          <Button size="small" startIcon={<EditIcon />} onClick={onEditResult} sx={actionBtnSx}>
            {t('matches.editResult')}
          </Button>
          <Button size="small" startIcon={<ScheduleIcon />} onClick={onEditSchedule} disabled={!canEditSchedule} sx={actionBtnSx}>
            {t('matches.editSchedule')}
          </Button>
          <Button size="small" component={RouterLink} to={matchDetailPath} startIcon={<VisibilityIcon />} sx={actionBtnSx}>
            {t('matches.viewDetails')}
          </Button>
          <Button
            size="small"
            color="error"
            startIcon={<DeleteOutlineIcon />}
            onClick={onDelete}
            disabled={!canDelete || isDeleting}
            sx={actionBtnSx}
          >
            {isDeleting ? t('matches.deleting') : t('matches.deleteMatch')}
          </Button>
        </Box>
      </CardContent>
    </Card>
  )
}

function MatchScheduleModal({
  open,
  match,
  fields,
  saving,
  onClose,
  onSave,
}: {
  open: boolean
  match: MatchListItem
  fields: Field[]
  saving: boolean
  onClose: () => void
  onSave: (startTime: string, fieldId: string) => void
}) {
  const { t } = useTranslation()
  const [startTime, setStartTime] = React.useState(match.kickoffTime || '')
  const [fieldId, setFieldId] = React.useState(match.fieldId ?? '')

  React.useEffect(() => {
    setStartTime(match.kickoffTime || '')
    setFieldId(match.fieldId ?? '')
  }, [match])

  const selectedFieldName = fields.find((f) => f.id === fieldId)?.name ?? ''
  const canSave = startTime.trim().length > 0 && fieldId.length > 0 && !saving

  const handleSave = () => {
    if (!canSave) return

    const ok = window.confirm(
      `¿Guardar horario/cancha para ${match.homeTeamName} vs ${match.awayTeamName}?\n\nHorario: ${startTime}\nCancha: ${selectedFieldName}`,
    )
    if (!ok) return

    onSave(startTime.trim(), fieldId)
  }

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Editar horario/cancha</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        <Typography variant="body2" fontWeight={600}>
          {match.homeTeamName} vs {match.awayTeamName}
        </Typography>
        <TextField
          label="Horario"
          type="time"
          size="small"
          value={startTime}
          onChange={(e) => setStartTime(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          fullWidth
        />
        <FormControl size="small" fullWidth>
          <InputLabel id="match-schedule-field-label">Cancha</InputLabel>
          <Select
            labelId="match-schedule-field-label"
            label="Cancha"
            value={fieldId}
            onChange={(e) => setFieldId(e.target.value)}
          >
            {fields.map((field) => (
              <MenuItem key={field.id} value={field.id}>
                {field.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={saving}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={handleSave} disabled={!canSave}>
          {saving ? 'Guardando…' : 'Guardar'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

