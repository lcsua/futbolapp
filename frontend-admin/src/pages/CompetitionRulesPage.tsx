import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { competitionRulesService } from '../api/competitionRules'
import { seasonsService } from '../api/seasons'
import type { CompetitionRuleFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

const defaultForm: CompetitionRuleFormData = {
  matchesPerWeek: 1,
  isHomeAway: false,
  matchDays: [],
}

export function CompetitionRulesPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string | null>(null)
  const [matchesPerWeek, setMatchesPerWeek] = useState(defaultForm.matchesPerWeek)
  const [isHomeAway, setIsHomeAway] = useState(defaultForm.isHomeAway)
  const [matchDays, setMatchDays] = useState<number[]>(defaultForm.matchDays)
  const [success, setSuccess] = useState(false)

  const { data: seasons } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: rule, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'competition-rules', seasonId],
    queryFn: async ({ signal }) => {
      try {
        return await competitionRulesService.get(leagueId!, seasonId ?? undefined, signal)
      } catch (e) {
        if (e instanceof Error && (e.message.includes('Not Found') || e.message.includes('404')))
          return null
        throw e
      }
    },
    enabled: !!leagueId,
  })

  useEffect(() => {
    if (rule) {
      setMatchesPerWeek(rule.matchesPerWeek)
      setIsHomeAway(rule.isHomeAway)
      setMatchDays(rule.matchDays ?? [])
    } else if (!isLoading && !isError) {
      setMatchesPerWeek(defaultForm.matchesPerWeek)
      setIsHomeAway(defaultForm.isHomeAway)
      setMatchDays(defaultForm.matchDays)
    }
  }, [rule, isLoading, isError])

  const putMutation = useMutation({
    mutationFn: (data: CompetitionRuleFormData) =>
      competitionRulesService.put(leagueId!, { ...data, seasonId: seasonId ?? undefined }, undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'competition-rules'] })
      setSuccess(true)
      setTimeout(() => setSuccess(false), 3000)
    },
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => {
    const v = e.target.value
    setSeasonId(v === '' ? null : v)
  }

  const handleDayToggle = (day: number) => {
    setMatchDays((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day].sort((a, b) => a - b)
    )
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    putMutation.mutate({
      seasonId: seasonId ?? null,
      matchesPerWeek,
      isHomeAway,
      matchDays,
    })
  }

  if (!leagueId) {
    return (
      <Alert severity="info">
        Select a league to configure competition rules.
      </Alert>
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError && !(error instanceof Error && error.message.includes('404'))) {
    return (
      <Alert severity="error">
        {error instanceof Error ? error.message : 'Failed to load competition rules'}
      </Alert>
    )
  }

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Competition rules
      </Typography>

      <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 560 }}>
        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Rules saved.
          </Alert>
        )}
        {putMutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {putMutation.error instanceof Error ? putMutation.error.message : 'Failed to save'}
          </Alert>
        )}

        <FormControl fullWidth sx={{ mb: 2 }}>
          <InputLabel id="season-label">Season</InputLabel>
          <Select
            labelId="season-label"
            label="Season"
            value={seasonId ?? ''}
            onChange={handleSeasonChange}
          >
            <MenuItem value="">League default</MenuItem>
            {seasons?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <TextField
          fullWidth
          type="number"
          label="Matches per week"
          value={matchesPerWeek}
          onChange={(e) => setMatchesPerWeek(Math.max(1, parseInt(e.target.value, 10) || 1))}
          inputProps={{ min: 1 }}
          sx={{ mb: 2 }}
        />

        <FormControlLabel
          control={
            <Switch
              checked={isHomeAway}
              onChange={(e) => setIsHomeAway(e.target.checked)}
              color="primary"
            />
          }
          label="Home & away"
          sx={{ mb: 2, display: 'block' }}
        />

        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
          Match days
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 3 }}>
          {DAY_NAMES.map((name, day) => (
            <Button
              key={day}
              variant={matchDays.includes(day) ? 'contained' : 'outlined'}
              size="small"
              onClick={() => handleDayToggle(day)}
            >
              {name}
            </Button>
          ))}
        </Box>

        <Button
          type="submit"
          variant="contained"
          disabled={putMutation.isPending}
          sx={{ minWidth: 120 }}
        >
          {putMutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Save'}
        </Button>
      </Box>
    </Box>
  )
}
