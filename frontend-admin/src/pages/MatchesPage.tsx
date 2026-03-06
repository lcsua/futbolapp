import { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
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
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { matchesService, type MatchListItem } from '../api/matches'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { useLeagueId } from '../contexts/LeagueContext'
import { MatchResultModal } from '../components/MatchResultModal'

export function MatchesPage() {
  const leagueId = useLeagueId()
  const { leagueId: leagueIdInPath } = useParams<{ leagueId?: string }>()
  const navigate = useNavigate()
  const [seasonId, setSeasonId] = useState('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [round, setRound] = useState<string>('')
  const [resultModalMatch, setResultModalMatch] = useState<MatchListItem | null>(null)

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
  }
  const handleDivisionChange = (e: SelectChangeEvent<string>) => setDivisionId(e.target.value)
  const handleRoundChange = (e: SelectChangeEvent<string>) => setRound(e.target.value)

  const rounds = matchesData?.rounds ?? []
  const roundNumbers = [...new Set(rounds.flatMap((r) => r.matches.map((m) => m.roundNumber)))].sort((a, b) => a - b)

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        No league selected.
      </Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to="/seasons" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to seasons
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Matches
      </Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 200 }} disabled={seasonsLoading}>
          <InputLabel id="season-label">Season</InputLabel>
          <Select
            labelId="season-label"
            label="Season"
            value={seasonId}
            onChange={handleSeasonChange}
          >
            <MenuItem value="">
              <em>Select season</em>
            </MenuItem>
            {seasons.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 180 }} disabled={!seasonId}>
          <InputLabel id="division-label">Division</InputLabel>
          <Select
            labelId="division-label"
            label="Division"
            value={divisionId}
            onChange={handleDivisionChange}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }} disabled={!seasonId}>
          <InputLabel id="round-label">Round</InputLabel>
          <Select
            labelId="round-label"
            label="Round"
            value={round}
            onChange={handleRoundChange}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {roundNumbers.map((r) => (
              <MenuItem key={r} value={String(r)}>
                {r}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {matchesLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!matchesLoading && seasonId && rounds.length === 0 && (
        <Typography color="text.secondary">No matches found. Generate fixtures first.</Typography>
      )}

      {!matchesLoading && rounds.length > 0 && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          {rounds.map((group) => (
            <Box key={`${group.roundNumber}-${group.divisionName}`}>
              <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                Round {group.roundNumber} — {group.divisionName}
              </Typography>
              <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(420px, 1fr))', gap: 2 }}>
                {group.matches.map((m) => (
                  <MatchCard
                    key={m.id}
                    match={m}
                    leagueId={leagueId!}
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
    </Box>
  )
}

function MatchCard({
  match,
  leagueId,
  matchDetailPath,
  onEditResult,
}: {
  match: MatchListItem
  leagueId: string
  matchDetailPath: string
  onEditResult: () => void
}) {
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
          {match.fieldName} · {match.kickoffTime}
        </Typography>
        <Box sx={{ display: 'flex', gap: 1, mt: 1.5 }}>
          <Button size="small" startIcon={<EditIcon />} onClick={onEditResult}>
            Edit Result
          </Button>
          <Button size="small" component={RouterLink} to={matchDetailPath} startIcon={<VisibilityIcon />}>
            View Details
          </Button>
        </Box>
      </CardContent>
    </Card>
  )
}
