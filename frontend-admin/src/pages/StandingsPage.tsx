import { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  CircularProgress,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useQuery } from '@tanstack/react-query'
import { standingsService } from '../api/standings'
import { seasonsService } from '../api/seasons'
import { useLeagueId } from '../contexts/LeagueContext'

export function StandingsPage() {
  const leagueId = useLeagueId()
  const [seasonId, setSeasonId] = useState('')

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: divisions = [], isLoading: standingsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'standings', seasonId],
    queryFn: ({ signal }) => standingsService.get(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => setSeasonId(e.target.value)

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
        Standings
      </Typography>

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

      {standingsLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!standingsLoading && seasonId && divisions.length === 0 && (
        <Typography color="text.secondary" sx={{ mt: 3 }}>
          No standings available yet.
        </Typography>
      )}

      {!standingsLoading && divisions.length > 0 && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 3 }}>
          {divisions.map((div) => (
            <Box key={div.divisionId}>
              <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                {div.divisionName}
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 600, width: 48 }} align="center">POS</TableCell>
                      <TableCell sx={{ fontWeight: 600, minWidth: 180 }}>TEAM</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 48 }} align="center">PTS</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">PJ</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">PG</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">PE</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">PP</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">GF</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 40 }} align="center">GC</TableCell>
                      <TableCell sx={{ fontWeight: 600, width: 48 }} align="center">DIF</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {div.standings.map((row) => (
                      <TableRow key={row.teamId}>
                        <TableCell align="center">{row.position}</TableCell>
                        <TableCell>{row.teamName}</TableCell>
                        <TableCell align="center">{row.points}</TableCell>
                        <TableCell align="center">{row.played}</TableCell>
                        <TableCell align="center">{row.wins}</TableCell>
                        <TableCell align="center">{row.draws}</TableCell>
                        <TableCell align="center">{row.losses}</TableCell>
                        <TableCell align="center">{row.goalsFor}</TableCell>
                        <TableCell align="center">{row.goalsAgainst}</TableCell>
                        <TableCell align="center">{row.goalDifference >= 0 ? `+${row.goalDifference}` : row.goalDifference}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          ))}
        </Box>
      )}
    </Box>
  )
}
