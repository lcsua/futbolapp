import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  TextField,
  Typography,
} from '@mui/material'
import { matchRulesService } from '../api/matchRules'
import type { MatchRuleFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

const defaultForm: MatchRuleFormData = {
  halfMinutes: 45,
  breakMinutes: 15,
  warmupBufferMinutes: 0,
  slotGranularityMinutes: 5,
  firstMatchToleranceMinutes: 0,
}

export function MatchRulesPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [halfMinutes, setHalfMinutes] = useState(defaultForm.halfMinutes)
  const [breakMinutes, setBreakMinutes] = useState(defaultForm.breakMinutes)
  const [warmupBufferMinutes, setWarmupBufferMinutes] = useState(defaultForm.warmupBufferMinutes)
  const [slotGranularityMinutes, setSlotGranularityMinutes] = useState(defaultForm.slotGranularityMinutes)
  const [firstMatchToleranceMinutes, setFirstMatchToleranceMinutes] = useState(defaultForm.firstMatchToleranceMinutes)
  const [success, setSuccess] = useState(false)

  const { data: rule, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'match-rules'],
    queryFn: async ({ signal }) => {
      try {
        return await matchRulesService.get(leagueId!, undefined, signal)
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
      setHalfMinutes(rule.halfMinutes)
      setBreakMinutes(rule.breakMinutes)
      setWarmupBufferMinutes(rule.warmupBufferMinutes)
      setSlotGranularityMinutes(rule.slotGranularityMinutes)
      setFirstMatchToleranceMinutes(rule.firstMatchToleranceMinutes ?? 0)
    } else if (!isLoading && !isError) {
      setHalfMinutes(defaultForm.halfMinutes)
      setBreakMinutes(defaultForm.breakMinutes)
      setWarmupBufferMinutes(defaultForm.warmupBufferMinutes)
      setSlotGranularityMinutes(defaultForm.slotGranularityMinutes)
      setFirstMatchToleranceMinutes(defaultForm.firstMatchToleranceMinutes)
    }
  }, [rule, isLoading, isError])

  const putMutation = useMutation({
    mutationFn: (data: MatchRuleFormData) =>
      matchRulesService.put(leagueId!, data, undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'match-rules'] })
      setSuccess(true)
      setTimeout(() => setSuccess(false), 3000)
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    putMutation.mutate({
      halfMinutes,
      breakMinutes,
      warmupBufferMinutes,
      slotGranularityMinutes,
      firstMatchToleranceMinutes,
    })
  }

  if (!leagueId) {
    return (
      <Alert severity="info">
        Select a league to configure match rules.
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
        {error instanceof Error ? error.message : 'Failed to load match rules'}
      </Alert>
    )
  }

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Match rules
      </Typography>

      <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 400 }}>
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

        <TextField
          fullWidth
          type="number"
          label="Half (minutes)"
          value={halfMinutes}
          onChange={(e) => setHalfMinutes(Math.max(1, parseInt(e.target.value, 10) || 0))}
          inputProps={{ min: 1 }}
          sx={{ mb: 2 }}
        />
        <TextField
          fullWidth
          type="number"
          label="Break (minutes)"
          value={breakMinutes}
          onChange={(e) => setBreakMinutes(Math.max(0, parseInt(e.target.value, 10) || 0))}
          inputProps={{ min: 0 }}
          sx={{ mb: 2 }}
        />
        <TextField
          fullWidth
          type="number"
          label="Warmup buffer (minutes)"
          value={warmupBufferMinutes}
          onChange={(e) => setWarmupBufferMinutes(Math.max(0, parseInt(e.target.value, 10) || 0))}
          inputProps={{ min: 0 }}
          sx={{ mb: 2 }}
        />
        <TextField
          fullWidth
          type="number"
          label="Slot granularity (minutes)"
          value={slotGranularityMinutes}
          onChange={(e) => setSlotGranularityMinutes(Math.max(1, parseInt(e.target.value, 10) || 1))}
          inputProps={{ min: 1 }}
          sx={{ mb: 2 }}
        />
        <TextField
          fullWidth
          type="number"
          label="First match tolerance (minutes)"
          value={firstMatchToleranceMinutes}
          onChange={(e) => setFirstMatchToleranceMinutes(Math.max(0, parseInt(e.target.value, 10) || 0))}
          inputProps={{ min: 0, step: 5 }}
          sx={{ mb: 3 }}
        />

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
