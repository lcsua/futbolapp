import { useState, useEffect } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  Typography,
  CircularProgress,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { matchesService, MATCH_STATUSES } from '../api/matches'
import type { MatchListItem } from '../api/matches'

interface MatchResultModalProps {
  open: boolean
  match: MatchListItem
  leagueId: string
  onClose: () => void
  onSaved?: () => void
}

export function MatchResultModal({ open, match, leagueId, onClose, onSaved }: MatchResultModalProps) {
  const queryClient = useQueryClient()
  const [homeScore, setHomeScore] = useState<string>(String(match.homeScore ?? ''))
  const [awayScore, setAwayScore] = useState<string>(String(match.awayScore ?? ''))
  const [status, setStatus] = useState<string>('COMPLETED')

  useEffect(() => {
    if (open && match) {
      setHomeScore(String(match.homeScore ?? ''))
      setAwayScore(String(match.awayScore ?? ''))
      setStatus('COMPLETED')
    }
  }, [open, match])

  const mutation = useMutation({
    mutationFn: () => {
      const home = parseInt(homeScore, 10)
      const away = parseInt(awayScore, 10)
      if (Number.isNaN(home) || Number.isNaN(away))
        throw new Error('Scores must be numbers')
      if (home < 0 || away < 0) throw new Error('Scores cannot be negative')
      return matchesService.updateResult(leagueId, match.id, {
        homeScore: Number.isNaN(home) ? 0 : home,
        awayScore: Number.isNaN(away) ? 0 : away,
        status,
      })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
      onSaved?.()
      onClose()
    },
  })

  const handleSave = () => mutation.mutate()

  const homeNum = parseInt(homeScore, 10)
  const awayNum = parseInt(awayScore, 10)
  const scoresValid = !Number.isNaN(homeNum) && !Number.isNaN(awayNum) && homeNum >= 0 && awayNum >= 0
  const canSave = scoresValid

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit result</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: '1fr auto auto auto 1fr',
              alignItems: 'center',
              gap: 2,
              minWidth: 0,
            }}
          >
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', textAlign: 'right' }}>
              {match.homeTeamName}
            </Typography>
            <TextField
              type="number"
              inputProps={{ min: 0, style: { fontSize: 26, textAlign: 'center' } }}
              value={homeScore}
              onChange={(e) => setHomeScore(e.target.value)}
              size="small"
              sx={{ width: 70 }}
            />
            <Typography variant="h6" color="text.secondary">—</Typography>
            <TextField
              type="number"
              inputProps={{ min: 0, style: { fontSize: 26, textAlign: 'center' } }}
              value={awayScore}
              onChange={(e) => setAwayScore(e.target.value)}
              size="small"
              sx={{ width: 70 }}
            />
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', textAlign: 'left' }}>
              {match.awayTeamName}
            </Typography>
          </Box>
          <FormControl fullWidth size="small">
            <InputLabel id="result-status-label">Status</InputLabel>
            <Select
              labelId="result-status-label"
              label="Status"
              value={status}
              onChange={(e: SelectChangeEvent<string>) => setStatus(e.target.value)}
            >
              {MATCH_STATUSES.map((s) => (
                <MenuItem key={s} value={s}>
                  {s.replace('_', ' ')}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {mutation.isError && (
            <Typography color="error" variant="body2">
              {mutation.error instanceof Error ? mutation.error.message : 'Save failed'}
            </Typography>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave} disabled={mutation.isPending || !canSave}>
          {mutation.isPending ? <CircularProgress size={24} /> : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
