import { useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  Select,
  Typography,
  CircularProgress,
  Chip,
  OutlinedInput,
} from '@mui/material'
import CheckIcon from '@mui/icons-material/Check'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { teamsService } from '../api/teams'
import { useLeagueId } from '../contexts/LeagueContext'

export function SeasonSetupPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [teamIds, setTeamIds] = useState<string[]>([])
  const [assignError, setAssignError] = useState<string | null>(null)
  const [assignSuccess, setAssignSuccess] = useState<string | null>(null)

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: divisions = [], isLoading: divisionsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: teams = [], isLoading: teamsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const assignMutation = useMutation({
    mutationFn: async () => {
      setAssignError(null)
      setAssignSuccess(null)
      const errors: string[] = []
      let successCount = 0
      for (const teamId of teamIds) {
        try {
          await teamsService.assignTeamToDivisionSeason(leagueId!, seasonId, divisionId, teamId)
          successCount += 1
        } catch (e) {
          const msg = e instanceof Error ? e.message : 'Failed to assign team'
          const team = teams.find((t) => t.id === teamId)
          errors.push(team ? `${team.name}: ${msg}` : msg)
        }
      }
      if (errors.length > 0) throw new Error(errors.join('\n'))
      return successCount
    },
    onSuccess: (count) => {
      setAssignSuccess(`${count} team(s) assigned.`)
      setTeamIds([])
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
    },
    onError: (err) => {
      setAssignError(err instanceof Error ? err.message : 'Assignment failed')
    },
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => setSeasonId(e.target.value)
  const handleDivisionChange = (e: SelectChangeEvent<string>) => setDivisionId(e.target.value)
  const handleTeamsChange = (e: SelectChangeEvent<string[]>) => {
    const value = e.target.value
    setTeamIds(typeof value === 'string' ? value.split(',') : value)
  }

  const canSave = !!leagueId && !!seasonId && !!divisionId && teamIds.length > 0 && !assignMutation.isPending

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
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Season setup — assign teams to division
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Assign teams to a division for a season. Each team can be in only one division per season. Manage teams under Teams.
      </Typography>

      {assignError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setAssignError(null)}>
          {assignError}
        </Alert>
      )}
      {assignSuccess && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setAssignSuccess(null)}>
          {assignSuccess}
        </Alert>
      )}

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 480 }}>
        <FormControl fullWidth disabled={seasonsLoading}>
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
        <FormControl fullWidth disabled={divisionsLoading}>
          <InputLabel id="division-label">Division</InputLabel>
          <Select
            labelId="division-label"
            label="Division"
            value={divisionId}
            onChange={handleDivisionChange}
          >
            <MenuItem value="">
              <em>Select division</em>
            </MenuItem>
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Typography variant="body2" sx={{ mb: 0.5 }}>
          Selected {teamIds.length} teams
        </Typography>
        <FormControl fullWidth disabled={teamsLoading || !seasonId || !divisionId}>
          <InputLabel id="teams-label">Teams</InputLabel>
          <Select
            labelId="teams-label"
            label="Teams"
            multiple
            value={teamIds}
            onChange={handleTeamsChange}
            input={<OutlinedInput label="Teams" />}
            renderValue={(selected) => (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                {selected.map((id) => {
                  const t = teams.find((x) => x.id === id)
                  return <Chip key={id} label={t?.name ?? id} size="small" />
                })}
              </Box>
            )}
          >
            {teams.map((t) => (
              <MenuItem key={t.id} value={t.id}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                  <ListItemText primary={t.name} secondary={t.shortName || undefined} />
                  {teamIds.includes(t.id) && <CheckIcon fontSize="small" color="primary" />}
                </Box>
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Button
          variant="contained"
          disabled={!canSave}
          onClick={() => assignMutation.mutate()}
          sx={{ alignSelf: 'flex-start' }}
        >
          {assignMutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Save assignment'}
        </Button>
      </Box>
    </Box>
  )
}
