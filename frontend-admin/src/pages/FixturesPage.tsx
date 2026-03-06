import { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import RefreshIcon from '@mui/icons-material/Refresh'
import SaveIcon from '@mui/icons-material/Save'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { fixturesService } from '../api/fixtures'
import { seasonsService } from '../api/seasons'
import { useLeagueId } from '../contexts/LeagueContext'

function formatTime(timeStr: string): string {
  try {
    const parts = timeStr.split(':')
    return `${parts[0]}:${parts[1] ?? '00'}`
  } catch {
    return timeStr
  }
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  } catch {
    return dateStr
  }
}

export function FixturesPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: fixturesData, isLoading: fixturesLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'],
    queryFn: ({ signal }) => fixturesService.get(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
    retry: false,
  })

  const generateMutation = useMutation({
    mutationFn: () => fixturesService.generate(leagueId!, seasonId),
    onSuccess: () => {
      setSnackbar({ message: 'Fixture draft generated.', severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : 'Generate failed', severity: 'error' })
    },
  })

  const commitMutation = useMutation({
    mutationFn: () => fixturesService.commit(leagueId!, seasonId),
    onSuccess: () => {
      setSnackbar({ message: 'Fixtures saved.', severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : 'Save failed', severity: 'error' })
    },
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => setSeasonId(e.target.value)

  const handleGenerate = () => generateMutation.mutate()
  const handleRegenerate = () => generateMutation.mutate()
  const handleSave = () => commitMutation.mutate()

  const fixtures = fixturesData?.fixtures
  const isDraft = fixturesData?.isDraft ?? false
  const hasFixtures = fixtures && fixtures.rounds.length > 0

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        No league selected. Choose a league from the selector.
      </Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to="/seasons" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to seasons
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Fixtures
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Generate fixtures for a season, preview them, then save when ready. Once saved, the season is locked (team assignments cannot be changed).
      </Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 220 }} disabled={seasonsLoading}>
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
        {!!seasonId && (
          <>
            <Button
              variant="outlined"
              startIcon={hasFixtures ? <RefreshIcon /> : <AutoFixHighIcon />}
              onClick={hasFixtures ? handleRegenerate : handleGenerate}
              disabled={generateMutation.isPending}
            >
              {hasFixtures ? 'Regenerate' : 'Generate'} Fixture
            </Button>
            {hasFixtures && (
              <Button
                variant="contained"
                startIcon={commitMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
                onClick={handleSave}
                disabled={commitMutation.isPending || !isDraft}
              >
                Save Fixture
              </Button>
            )}
            {hasFixtures && isDraft && (
              <Chip label="Draft (not saved)" color="warning" size="small" />
            )}
            {hasFixtures && !isDraft && (
              <Chip label="Committed" color="success" size="small" />
            )}
          </>
        )}
      </Box>

      {snackbar && (
        <Snackbar
          open={!!snackbar}
          autoHideDuration={5000}
          onClose={() => setSnackbar(null)}
          message={snackbar.message}
          ContentProps={{
            sx: {
              bgcolor: snackbar.severity === 'error' ? 'error.main' : 'success.main',
              color: 'white',
            },
          }}
        />
      )}

      {fixturesLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!fixturesLoading && seasonId && !hasFixtures && (
        <Typography color="text.secondary">
          Select a season and click Generate Fixture to create a draft.
        </Typography>
      )}

      {!fixturesLoading && hasFixtures && (
        <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell><strong>Round</strong></TableCell>
                <TableCell><strong>Date</strong></TableCell>
                <TableCell><strong>Division</strong></TableCell>
                <TableCell><strong>Field</strong></TableCell>
                <TableCell><strong>Time</strong></TableCell>
                <TableCell><strong>Home</strong></TableCell>
                <TableCell><strong>Away</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {fixtures!.rounds.map((round) =>
                round.matches.map((m, idx) => (
                  <TableRow key={`${round.roundNumber}-${idx}-${m.homeTeamDivisionSeasonId}-${m.awayTeamDivisionSeasonId}`}>
                    <TableCell>{idx === 0 ? round.roundNumber : ''}</TableCell>
                    <TableCell>{idx === 0 ? formatDate(round.matchDate) : ''}</TableCell>
                    <TableCell>{m.divisionName}</TableCell>
                    <TableCell>{m.fieldName}</TableCell>
                    <TableCell>{formatTime(m.kickoffTime)}</TableCell>
                    <TableCell>{m.homeTeamName}</TableCell>
                    <TableCell>{m.awayTeamName}</TableCell>
                  </TableRow>
                )),
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  )
}
