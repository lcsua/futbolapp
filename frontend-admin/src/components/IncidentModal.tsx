import { useState, useEffect } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Box,
  Typography,
  CircularProgress,
} from '@mui/material'
import { useMutation } from '@tanstack/react-query'
import { matchesService, INCIDENT_TYPES } from '../api/matches'
import type { MatchDetailResponse } from '../api/matches'

interface IncidentModalProps {
  open: boolean
  match: MatchDetailResponse
  leagueId: string
  onClose: () => void
  onSaved?: () => void
}

export function IncidentModal({ open, match, leagueId, onClose, onSaved }: IncidentModalProps) {
  const [minute, setMinute] = useState<string>('')
  const [teamId, setTeamId] = useState<string>('')
  const [playerName, setPlayerName] = useState('')
  const [incidentType, setIncidentType] = useState<string>(INCIDENT_TYPES[0])
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (open) {
      setMinute('')
      setTeamId('')
      setPlayerName('')
      setIncidentType(INCIDENT_TYPES[0])
      setNotes('')
    }
  }, [open])

  const mutation = useMutation({
    mutationFn: () => {
      const min = parseInt(minute, 10)
      if (Number.isNaN(min) || min < 0) throw new Error('Minute must be >= 0')
      return matchesService.addIncident(leagueId, match.id, {
        minute: min,
        teamId: teamId || null,
        playerName: playerName.trim() || 'Unknown',
        incidentType,
        notes: notes.trim(),
      })
    },
    onSuccess: () => {
      onSaved?.()
      onClose()
    },
  })

  const handleSave = () => {
    mutation.mutate()
  }

  const teams = [
    { id: match.homeTeamId, name: match.homeTeamName },
    { id: match.awayTeamId, name: match.awayTeamName },
  ]

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add incident</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Minute"
            type="number"
            inputProps={{ min: 0 }}
            value={minute}
            onChange={(e) => setMinute(e.target.value)}
            size="small"
            fullWidth
          />
          <FormControl fullWidth size="small">
            <InputLabel id="team-label">Team</InputLabel>
            <Select
              labelId="team-label"
              label="Team"
              value={teamId}
              onChange={(e: SelectChangeEvent<string>) => setTeamId(e.target.value)}
            >
              <MenuItem value="">
                <em>—</em>
              </MenuItem>
              {teams.map((t) => (
                <MenuItem key={t.id} value={t.id}>
                  {t.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Player name"
            value={playerName}
            onChange={(e) => setPlayerName(e.target.value)}
            size="small"
            fullWidth
          />
          <FormControl fullWidth size="small">
            <InputLabel id="type-label">Incident type</InputLabel>
            <Select
              labelId="type-label"
              label="Incident type"
              value={incidentType}
              onChange={(e: SelectChangeEvent<string>) => setIncidentType(e.target.value)}
            >
              {INCIDENT_TYPES.map((t) => (
                <MenuItem key={t} value={t}>
                  {t}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            size="small"
            fullWidth
            multiline
            rows={2}
          />
          {mutation.isError && (
            <Typography color="error" variant="body2">
              {mutation.error instanceof Error ? mutation.error.message : 'Save failed'}
            </Typography>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave} disabled={mutation.isPending}>
          {mutation.isPending ? <CircularProgress size={24} /> : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
