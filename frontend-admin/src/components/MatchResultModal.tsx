import { useEffect, useMemo, useState } from 'react'
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
  Alert,
  Checkbox,
  FormControlLabel,
  Divider,
  Stack,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { matchesService, MATCH_STATUSES, type MatchGoalAttribution } from '../api/matches'
import type { MatchListItem } from '../api/matches'
import { playersService, type Player } from '../api/players'

interface MatchResultModalProps {
  open: boolean
  match: MatchListItem
  leagueId: string
  seasonClosed?: boolean
  onClose: () => void
  onSaved?: () => void
}

type GoalDraft = {
  teamId: string
  scorerPlayerId: string
  againstGoalkeeperPlayerId: string
  minute: string
}

function buildGoalSlots(homeTeamId: string, awayTeamId: string, home: number, away: number, prev: GoalDraft[]): GoalDraft[] {
  const total = Math.max(0, home) + Math.max(0, away)
  const next: GoalDraft[] = []
  for (let i = 0; i < home; i++) {
    next.push(prev[i] && prev[i].teamId === homeTeamId
      ? prev[i]
      : { teamId: homeTeamId, scorerPlayerId: '', againstGoalkeeperPlayerId: '', minute: '' })
  }
  for (let i = 0; i < away; i++) {
    const idx = home + i
    next.push(prev[idx] && prev[idx].teamId === awayTeamId
      ? prev[idx]
      : { teamId: awayTeamId, scorerPlayerId: '', againstGoalkeeperPlayerId: '', minute: '' })
  }
  // keep length exact
  return next.slice(0, total)
}

export function MatchResultModal({ open, match, leagueId, seasonClosed = false, onClose, onSaved }: MatchResultModalProps) {
  const queryClient = useQueryClient()
  const [homeScore, setHomeScore] = useState<string>(String(match.homeScore ?? ''))
  const [awayScore, setAwayScore] = useState<string>(String(match.awayScore ?? ''))
  const [status, setStatus] = useState<string>('COMPLETED')
  const [trackGoals, setTrackGoals] = useState(false)
  const [goals, setGoals] = useState<GoalDraft[]>([])

  useEffect(() => {
    if (open && match) {
      setHomeScore(String(match.homeScore ?? ''))
      setAwayScore(String(match.awayScore ?? ''))
      setStatus('COMPLETED')
      setTrackGoals(false)
      setGoals([])
    }
  }, [open, match])

  const homeNum = parseInt(homeScore, 10)
  const awayNum = parseInt(awayScore, 10)
  const scoresValid = !Number.isNaN(homeNum) && !Number.isNaN(awayNum) && homeNum >= 0 && awayNum >= 0

  useEffect(() => {
    if (!trackGoals || !scoresValid) return
    setGoals((prev) => buildGoalSlots(match.homeTeamId, match.awayTeamId, homeNum, awayNum, prev))
  }, [trackGoals, scoresValid, homeNum, awayNum, match.homeTeamId, match.awayTeamId])

  const { data: roster = [], isLoading: rosterLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'teams', 'players', match.homeTeamId, match.awayTeamId],
    queryFn: ({ signal }) => playersService.listByTeamIds(leagueId, [match.homeTeamId, match.awayTeamId], signal),
    enabled: open && trackGoals && !!leagueId,
  })

  const playersByTeam = useMemo(() => {
    const map = new Map<string, Player[]>()
    for (const p of roster) {
      if (!p.isActive) continue
      const list = map.get(p.teamId) ?? []
      list.push(p)
      map.set(p.teamId, list)
    }
    for (const [key, list] of map) {
      map.set(
        key,
        [...list].sort((a, b) => a.displayName.localeCompare(b.displayName, 'es'))
      )
    }
    return map
  }, [roster])

  const mutation = useMutation({
    mutationFn: () => {
      if (!scoresValid) throw new Error('Los marcadores deben ser números válidos')
      const goalsPayload: MatchGoalAttribution[] | undefined = trackGoals
        ? goals.map((g) => ({
            teamId: g.teamId,
            scorerPlayerId: g.scorerPlayerId || null,
            againstGoalkeeperPlayerId: g.againstGoalkeeperPlayerId || null,
            minute: g.minute.trim() ? Number.parseInt(g.minute, 10) : null,
          }))
        : undefined

      return matchesService.updateResult(leagueId, match.id, {
        homeScore: homeNum,
        awayScore: awayNum,
        status,
        goals: goalsPayload,
      })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] })
      onSaved?.()
      onClose()
    },
  })

  const teamName = (teamId: string) =>
    teamId === match.homeTeamId ? match.homeTeamName : match.awayTeamName

  const opposingTeamId = (scoringTeamId: string) =>
    scoringTeamId === match.homeTeamId ? match.awayTeamId : match.homeTeamId

  const goalkeepers = (teamId: string) => {
    const all = playersByTeam.get(teamId) ?? []
    const gks = all.filter((p) => p.position === 'GK')
    return gks.length > 0 ? gks : all
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Editar resultado</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          {seasonClosed && (
            <Alert severity="warning">
              Esta temporada está cerrada. Editá el resultado solo si un fallo lo requiere.
            </Alert>
          )}
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
            <InputLabel id="result-status-label">Estado</InputLabel>
            <Select
              labelId="result-status-label"
              label="Estado"
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

          <Divider />
          <FormControlLabel
            control={
              <Checkbox
                checked={trackGoals}
                onChange={(e) => setTrackGoals(e.target.checked)}
                disabled={!scoresValid || homeNum + awayNum === 0}
              />
            }
            label="Registrar goleadores y arqueros (opcional)"
          />

          {trackGoals && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              {rosterLoading && (
                <Box sx={{ display: 'flex', justifyContent: 'center', py: 1 }}>
                  <CircularProgress size={22} />
                </Box>
              )}
              {!rosterLoading && (playersByTeam.get(match.homeTeamId)?.length ?? 0) === 0 && (playersByTeam.get(match.awayTeamId)?.length ?? 0) === 0 && (
                <Alert severity="info">
                  Estos equipos todavía no tienen plantel cargado. Podés guardar el marcador igual y completar goleadores después.
                </Alert>
              )}
              {goals.map((goal, index) => {
                const scorers = playersByTeam.get(goal.teamId) ?? []
                const keepers = goalkeepers(opposingTeamId(goal.teamId))
                return (
                  <Box
                    key={`goal-${index}`}
                    sx={{
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 1,
                      p: 1.5,
                      display: 'flex',
                      flexDirection: 'column',
                      gap: 1.25,
                    }}
                  >
                    <Typography variant="subtitle2">Gol {index + 1}</Typography>
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25}>
                      <FormControl fullWidth size="small">
                        <InputLabel id={`goal-team-${index}`}>Equipo</InputLabel>
                        <Select
                          labelId={`goal-team-${index}`}
                          label="Equipo"
                          value={goal.teamId}
                          onChange={(e) => {
                            const teamId = e.target.value
                            setGoals((prev) => prev.map((g, i) => i === index
                              ? { teamId, scorerPlayerId: '', againstGoalkeeperPlayerId: '', minute: g.minute }
                              : g))
                          }}
                        >
                          <MenuItem value={match.homeTeamId}>{match.homeTeamName}</MenuItem>
                          <MenuItem value={match.awayTeamId}>{match.awayTeamName}</MenuItem>
                        </Select>
                      </FormControl>
                      <TextField
                        label="Minuto"
                        size="small"
                        type="number"
                        value={goal.minute}
                        onChange={(e) => setGoals((prev) => prev.map((g, i) => i === index ? { ...g, minute: e.target.value } : g))}
                        sx={{ width: { sm: 110 } }}
                        inputProps={{ min: 0 }}
                      />
                    </Stack>
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25}>
                      <FormControl fullWidth size="small">
                        <InputLabel id={`goal-scorer-${index}`}>Goleador ({teamName(goal.teamId)})</InputLabel>
                        <Select
                          labelId={`goal-scorer-${index}`}
                          label={`Goleador (${teamName(goal.teamId)})`}
                          value={goal.scorerPlayerId}
                          onChange={(e) => setGoals((prev) => prev.map((g, i) => i === index ? { ...g, scorerPlayerId: e.target.value } : g))}
                        >
                          <MenuItem value="">Sin especificar</MenuItem>
                          {scorers.map((p) => (
                            <MenuItem key={p.id} value={p.id}>{p.displayName}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <FormControl fullWidth size="small">
                        <InputLabel id={`goal-gk-${index}`}>Arquero rival</InputLabel>
                        <Select
                          labelId={`goal-gk-${index}`}
                          label="Arquero rival"
                          value={goal.againstGoalkeeperPlayerId}
                          onChange={(e) => setGoals((prev) => prev.map((g, i) => i === index ? { ...g, againstGoalkeeperPlayerId: e.target.value } : g))}
                        >
                          <MenuItem value="">Sin especificar</MenuItem>
                          {keepers.map((p) => (
                            <MenuItem key={p.id} value={p.id}>{p.displayName}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Stack>
                  </Box>
                )
              })}
            </Box>
          )}

          {mutation.isError && (
            <Typography color="error" variant="body2">
              {mutation.error instanceof Error ? mutation.error.message : 'No se pudo guardar'}
            </Typography>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending || !scoresValid}>
          {mutation.isPending ? <CircularProgress size={24} /> : 'Guardar'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
