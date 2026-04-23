import React, { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
  CircularProgress,
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink, useParams } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { matchesService, type MatchListItem } from '../api/matches'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { useLeagueId } from '../contexts/LeagueContext'
import { MatchResultModal } from '../components/MatchResultModal'
import { ImportFixtureModal } from '../components/ImportFixtureModal'
import { useQueryClient } from '@tanstack/react-query'
import UploadFileIcon from '@mui/icons-material/UploadFile'

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
          disabled={!seasonId || !divisionId}
        >
          {t('matches.importFixture')}
        </Button>
      </Box>

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
            <Typography variant="h6" component="span" sx={{ minWidth: 24, textAlign: 'center' }}>
              {match.homeScore ?? '-'}
            </Typography>
            <Typography color="text.secondary">—</Typography>
            <Typography variant="h6" component="span" sx={{ minWidth: 24, textAlign: 'center' }}>
              {match.awayScore ?? '-'}
            </Typography>
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
