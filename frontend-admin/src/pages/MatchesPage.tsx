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
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
  CircularProgress,
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import VisibilityIcon from '@mui/icons-material/Visibility'
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink, useParams } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { matchesService, matchStatusLabel, type MatchListItem } from '../api/matches'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { useLeagueId } from '../contexts/LeagueContext'
import { MatchResultModal } from '../components/MatchResultModal'
import { ImportFixtureModal } from '../components/ImportFixtureModal'
import { ImportMatchResultsModal } from '../components/ImportMatchResultsModal'
import { ImportScheduleModal } from '../components/ImportScheduleModal'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import ScoreboardIcon from '@mui/icons-material/Scoreboard'
import ScheduleIcon from '@mui/icons-material/Schedule'

export function MatchesPage() {
  const { t } = useTranslation()
  const leagueId = useLeagueId()
  const { leagueId: leagueIdInPath } = useParams<{ leagueId?: string }>()
  const [seasonId, setSeasonId] = useState('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [round, setRound] = useState<string>('')
  const [teamId, setTeamId] = useState<string>('')
  const [resultModalMatch, setResultModalMatch] = useState<MatchListItem | null>(null)
  const [importModalOpen, setImportModalOpen] = useState(false)
  const [importResultsOpen, setImportResultsOpen] = useState(false)
  const [importScheduleOpen, setImportScheduleOpen] = useState(false)
  const [importResultsMsg, setImportResultsMsg] = useState<string | null>(null)
  const [clearError, setClearError] = useState<string | null>(null)
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

  const { data: matchesData, isLoading: matchesLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, divisionId || null, round || null],
    queryFn: ({ signal }) =>
      matchesService.getMatches(
        leagueId!,
        {
          seasonId,
          divisionId: divisionId || undefined,
          round: round === '' ? undefined : parseInt(round, 10),
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
  const roundNumbers = [...new Set(allRounds.flatMap((r) => r.matches.map((m) => m.roundNumber)))].sort((a, b) => a - b)
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
    if (!teamId) return allRounds
    return allRounds
      .map((g) => ({
        ...g,
        matches: g.matches.filter((m) => m.homeTeamId === teamId || m.awayTeamId === teamId),
      }))
      .filter((g) => g.matches.length > 0)
  }, [allRounds, teamId])

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

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">{t('matches.goToLeagues')}</Button>}>
        {t('matches.noLeague')}
      </Alert>
    )
  }

  return (
    <Box>
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

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 200 }} disabled={seasonsLoading}>
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
        <FormControl size="small" sx={{ minWidth: 180 }} disabled={!seasonId}>
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
        <FormControl size="small" sx={{ minWidth: 120 }} disabled={!seasonId}>
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
        <FormControl size="small" sx={{ minWidth: 200 }} disabled={!seasonId || allRounds.length === 0}>
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
          onClick={() => setImportResultsOpen(true)}
          disabled={seasonClosed || !seasonId}
        >
          Importar resultados CSV
        </Button>
        <Button
          variant="outlined"
          startIcon={<ScheduleIcon />}
          onClick={() => setImportScheduleOpen(true)}
          disabled={seasonClosed || !seasonId}
        >
          Importar horarios/canchas
        </Button>
        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteSweepIcon />}
          onClick={handleClearRoundResults}
          disabled={!canClearRoundResults || clearRoundMutation.isPending}
        >
          {clearRoundMutation.isPending ? 'Borrando…' : 'Borrar resultados de la fecha'}
        </Button>
      </Box>

      {clearError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setClearError(null)}>
          {clearError}
        </Alert>
      )}

      {importResultsMsg && (
        <Alert
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

      {!matchesLoading && rounds.length > 0 && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          {rounds.map((group) => (
            <Box key={`${group.roundNumber}-${group.divisionName}`}>
              <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                {t('matches.roundTitle', { round: group.roundNumber, division: group.divisionName })}
              </Typography>
              <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(420px, 1fr))', gap: 2 }}>
                {group.matches.map((m) => (
                  <MatchCard
                    key={m.id}
                    match={m}
                    matchDetailPath={leagueIdInPath ? `/leagues/${leagueIdInPath}/matches/${m.id}` : `/matches/${m.id}`}
                    onEditResult={() => setResultModalMatch(m)}
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
        <ImportMatchResultsModal
          open={importResultsOpen}
          onClose={() => setImportResultsOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          filterDivisionId={divisionId}
          divisions={divisions}
          onImported={({ updatedCount, createdCount, skippedCount, notCreatedCount, warnings }) => {
            const parts = [
              updatedCount ? `${updatedCount} actualizado(s)` : null,
              createdCount ? `${createdCount} creado(s)` : null,
              skippedCount ? `${skippedCount} omitido(s) (ya tenían resultado)` : null,
              notCreatedCount ? `${notCreatedCount} no creado(s) (la fecha ya tiene fixture)` : null,
            ].filter(Boolean)
            const warn = warnings.length ? `\n${warnings.join('\n')}` : ''
            setImportResultsMsg(
              parts.length ? `Resultados: ${parts.join(', ')}.${warn}` : `Import OK.${warn}`
            )
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
          }}
        />
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
    </Box>
  )
}

function MatchCard({
  match,
  matchDetailPath,
  onEditResult,
}: {
  match: MatchListItem
  matchDetailPath: string
  onEditResult: () => void
}) {
  const { t } = useTranslation()
  return (
    <Card
      variant="outlined"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        height: 130,
        padding: 2,
      }}
    >
      <CardContent sx={{ p: 0, flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'space-between', '&:last-child': { pb: 0 } }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: '1fr auto 1fr',
            alignItems: 'center',
            gap: 1.5,
            minWidth: 0,
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 0 }}>
            {match.homeTeamLogoUrl && (
              <Box component="img" src={match.homeTeamLogoUrl} alt="" sx={{ width: 24, height: 24, flexShrink: 0, objectFit: 'contain' }} />
            )}
            <Typography variant="body2" fontWeight={600} sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {match.homeTeamName}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexShrink: 0 }}>
            {String(match.status || '').toUpperCase() === 'SUSPENDED' ? (
              <Chip label={matchStatusLabel('SUSPENDED')} size="small" color="warning" />
            ) : (
              <>
                <Typography variant="h6" component="span" sx={{ minWidth: 24, textAlign: 'center' }}>
                  {match.homeScore ?? '-'}
                </Typography>
                <Typography color="text.secondary">—</Typography>
                <Typography variant="h6" component="span" sx={{ minWidth: 24, textAlign: 'center' }}>
                  {match.awayScore ?? '-'}
                </Typography>
              </>
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 0, justifyContent: 'flex-end' }}>
            <Typography variant="body2" fontWeight={600} sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {match.awayTeamName}
            </Typography>
            {match.awayTeamLogoUrl && (
              <Box component="img" src={match.awayTeamLogoUrl} alt="" sx={{ width: 24, height: 24, flexShrink: 0, objectFit: 'contain' }} />
            )}
          </Box>
        </Box>
        <Typography variant="caption" color="text.secondary" display="block">
          {(match.fieldName || '—')} · {(match.kickoffTime || '—')}
        </Typography>
        <Box sx={{ display: 'flex', gap: 1, mt: 1.5 }}>
          <Button size="small" startIcon={<EditIcon />} onClick={onEditResult}>
            {t('matches.editResult')}
          </Button>
          <Button size="small" component={RouterLink} to={matchDetailPath} startIcon={<VisibilityIcon />}>
            {t('matches.viewDetails')}
          </Button>
        </Box>
      </CardContent>
    </Card>
  )
}
