import { useEffect, useMemo, useState } from 'react'
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
  Autocomplete,
  Alert,
} from '@mui/material'
import { useMutation, useQuery } from '@tanstack/react-query'
import { matchesService, INCIDENT_TYPES, INCIDENT_TYPE_LABELS } from '../api/matches'
import type { MatchDetailResponse } from '../api/matches'
import { playersService, type Player } from '../api/players'

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
  const [player, setPlayer] = useState<Player | null>(null)
  const [incidentType, setIncidentType] = useState<string>(INCIDENT_TYPES[0])
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (open) {
      setMinute('')
      setTeamId(match.homeTeamId || '')
      setPlayer(null)
      setIncidentType(INCIDENT_TYPES[0])
      setNotes('')
    }
  }, [open, match.homeTeamId])

  const { data: roster = [], isLoading: rosterLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'teams', 'players', match.homeTeamId, match.awayTeamId],
    queryFn: ({ signal }) => playersService.listByTeamIds(leagueId, [match.homeTeamId, match.awayTeamId], signal),
    enabled: open && !!leagueId && !!match.homeTeamId && !!match.awayTeamId,
  })

  const teamPlayers = useMemo(() => {
    if (!teamId) return []
    return roster
      .filter((p) => p.teamId === teamId && p.isActive)
      .sort((a, b) => a.displayName.localeCompare(b.displayName, 'es'))
  }, [roster, teamId])

  useEffect(() => {
    if (player && teamId && player.teamId !== teamId) {
      setPlayer(null)
    }
  }, [teamId, player])

  const mutation = useMutation({
    mutationFn: () => {
      const minuteValue = minute.trim() === '' ? null : Number.parseInt(minute, 10)
      if (minuteValue != null && (Number.isNaN(minuteValue) || minuteValue < 0)) {
        throw new Error('El minuto debe ser 0 o mayor')
      }
      if (!teamId) throw new Error('Seleccioná un equipo')
      if (!player) throw new Error('Seleccioná un jugador del plantel')

      return matchesService.addIncident(leagueId, match.id, {
        minute: minuteValue,
        teamId,
        playerId: player.id,
        playerName: player.displayName,
        incidentType,
        notes: notes.trim(),
      })
    },
    onSuccess: () => {
      onSaved?.()
      onClose()
    },
  })

  const teams = [
    { id: match.homeTeamId, name: match.homeTeamName },
    { id: match.awayTeamId, name: match.awayTeamName },
  ]

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Agregar incidencia</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Minuto (opcional)"
            type="number"
            inputProps={{ min: 0 }}
            value={minute}
            onChange={(e) => setMinute(e.target.value)}
            size="small"
            fullWidth
            helperText="Podés dejarlo vacío si no aplica"
          />
          <FormControl fullWidth size="small">
            <InputLabel id="team-label">Equipo</InputLabel>
            <Select
              labelId="team-label"
              label="Equipo"
              value={teamId}
              onChange={(e: SelectChangeEvent<string>) => setTeamId(e.target.value)}
            >
              {teams.map((t) => (
                <MenuItem key={t.id} value={t.id}>
                  {t.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Autocomplete
            options={teamPlayers}
            loading={rosterLoading}
            value={player}
            onChange={(_, value) => setPlayer(value)}
            getOptionLabel={(option) => option.displayName}
            isOptionEqualToValue={(a, b) => a.id === b.id}
            noOptionsText={
              rosterLoading
                ? 'Cargando plantel…'
                : teamId
                  ? 'Sin integrantes en este equipo'
                  : 'Seleccioná un equipo'
            }
            renderInput={(params) => (
              <TextField
                {...params}
                label="Jugador"
                size="small"
                placeholder="Buscar por nombre o apodo"
              />
            )}
          />
          {!rosterLoading && teamId && teamPlayers.length === 0 && (
            <Alert severity="info">
              Este equipo no tiene plantel. Cargá integrantes antes de registrar la incidencia.
            </Alert>
          )}
          <FormControl fullWidth size="small">
            <InputLabel id="type-label">Tipo</InputLabel>
            <Select
              labelId="type-label"
              label="Tipo"
              value={incidentType}
              onChange={(e: SelectChangeEvent<string>) => setIncidentType(e.target.value)}
            >
              {INCIDENT_TYPES.map((t) => (
                <MenuItem key={t} value={t}>
                  {INCIDENT_TYPE_LABELS[t] ?? t}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Notas"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            size="small"
            fullWidth
            multiline
            rows={2}
          />
          {mutation.isError && (
            <Typography color="error" variant="body2">
              {mutation.error instanceof Error ? mutation.error.message : 'No se pudo guardar'}
            </Typography>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !player || !teamId}
        >
          {mutation.isPending ? <CircularProgress size={24} /> : 'Guardar'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
