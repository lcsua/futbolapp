import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import LockIcon from '@mui/icons-material/Lock'
import AddIcon from '@mui/icons-material/Add'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { fixturesService, type RoundInterzonalPair } from '../api/fixtures'
import { categoryKey, isZone } from '../utils/zones'

interface RoundInterzonalModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  initialDivisionId: string
  divisions: { id: string; name: string }[]
  roundNumbers: number[]
  initialRound: number
  onSaved: (warnings: string[]) => void
}

interface ZoneCategory {
  key: string
  label: string
  divisionIds: string[]
}

function formatTime(value: string | null | undefined): string | null {
  if (!value) return null
  const [hh, mm] = value.split(':')
  return mm !== undefined ? `${hh}:${mm}` : value
}

export function RoundInterzonalModal({
  open,
  onClose,
  leagueId,
  seasonId,
  initialDivisionId,
  divisions,
  roundNumbers,
  initialRound,
  onSaved,
}: RoundInterzonalModalProps) {
  const { t } = useTranslation()

  const categories = useMemo<ZoneCategory[]>(() => {
    const byKey = new Map<string, ZoneCategory>()
    for (const d of divisions) {
      if (!isZone(d.name)) continue
      const key = categoryKey(d.name)
      const entry = byKey.get(key) ?? { key, label: '', divisionIds: [] }
      entry.divisionIds.push(d.id)
      byKey.set(key, entry)
    }
    return [...byKey.values()]
      .filter((c) => c.divisionIds.length >= 2)
      .map((c) => ({
        ...c,
        label: divisions
          .filter((d) => c.divisionIds.includes(d.id))
          .map((d) => d.name)
          .join(' / '),
      }))
      .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }))
  }, [divisions])

  const [categoryKeyValue, setCategoryKeyValue] = useState('')
  const [round, setRound] = useState<number>(initialRound)
  const [pairs, setPairs] = useState<RoundInterzonalPair[]>([])
  const [homeId, setHomeId] = useState('')
  const [awayId, setAwayId] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setError(null)
    setHomeId('')
    setAwayId('')
    setRound(initialRound)
    const initial = divisions.find((d) => d.id === initialDivisionId)
    const initialKey = initial && isZone(initial.name) ? categoryKey(initial.name) : ''
    const match = categories.find((c) => c.key === initialKey) ?? categories[0]
    setCategoryKeyValue(match?.key ?? '')
  }, [open, initialDivisionId, initialRound, divisions, categories])

  const category = categories.find((c) => c.key === categoryKeyValue)
  const divisionIds = category?.divisionIds ?? []

  const { data, isLoading, error: loadError } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures', 'interzonal', round, divisionIds],
    queryFn: ({ signal }) => fixturesService.getRoundInterzonal(leagueId, seasonId, round, divisionIds, signal),
    enabled: open && divisionIds.length >= 2 && round >= 1,
    retry: false,
  })

  useEffect(() => {
    setPairs(data?.pairs ?? [])
    setHomeId('')
    setAwayId('')
  }, [data])

  const zoneOfTeam = useMemo(() => {
    const map = new Map<string, string>()
    for (const zone of data?.zones ?? []) {
      for (const team of zone.teams) map.set(team.teamDivisionSeasonId, zone.divisionSeasonId)
    }
    return map
  }, [data])

  const teamName = useMemo(() => {
    const map = new Map<string, string>()
    for (const zone of data?.zones ?? []) {
      for (const team of zone.teams) map.set(team.teamDivisionSeasonId, team.teamName)
    }
    return map
  }, [data])

  const pairedIds = useMemo(
    () => new Set(pairs.flatMap((p) => [p.homeTeamDivisionSeasonId, p.awayTeamDivisionSeasonId])),
    [pairs],
  )

  const freeByZone = useMemo(
    () =>
      (data?.zones ?? []).map((zone) => ({
        zone,
        teams: zone.teams.filter((tm) => !tm.isBusy && !pairedIds.has(tm.teamDivisionSeasonId)),
      })),
    [data, pairedIds],
  )

  const freeTeams = freeByZone.flatMap(({ zone, teams }) =>
    teams.map((tm) => ({ ...tm, zoneName: zone.divisionName })),
  )
  const homeZone = homeId ? zoneOfTeam.get(homeId) : undefined
  const awayCandidates = freeTeams.filter(
    (tm) => tm.teamDivisionSeasonId !== homeId && (!homeZone || zoneOfTeam.get(tm.teamDivisionSeasonId) !== homeZone),
  )

  const originalKey = (data?.pairs ?? [])
    .map((p) => `${p.homeTeamDivisionSeasonId}>${p.awayTeamDivisionSeasonId}`)
    .sort()
    .join('|')
  const currentKey = pairs
    .map((p) => `${p.homeTeamDivisionSeasonId}>${p.awayTeamDivisionSeasonId}`)
    .sort()
    .join('|')
  const dirty = !!data && originalKey !== currentKey

  const handleAdd = () => {
    if (!homeId || !awayId) return
    setPairs((prev) => [
      ...prev,
      {
        fixtureId: null,
        homeTeamDivisionSeasonId: homeId,
        homeTeamName: teamName.get(homeId) ?? '',
        awayTeamDivisionSeasonId: awayId,
        awayTeamName: teamName.get(awayId) ?? '',
        isLocked: false,
      },
    ])
    setHomeId('')
    setAwayId('')
  }

  const handleRemove = (index: number) => {
    setPairs((prev) => prev.filter((_, i) => i !== index))
  }

  const handleSave = async () => {
    if (!dirty || saving) return
    setSaving(true)
    setError(null)
    try {
      const result = await fixturesService.setRoundInterzonal(leagueId, seasonId, round, {
        divisionIds,
        pairs: pairs.map((p) => ({
          homeTeamDivisionSeasonId: p.homeTeamDivisionSeasonId,
          awayTeamDivisionSeasonId: p.awayTeamDivisionSeasonId,
        })),
      })
      onSaved(result.warnings ?? [])
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.interzonalModal.failed'))
    } finally {
      setSaving(false)
    }
  }

  const loadErrorMessage = loadError instanceof Error ? loadError.message : loadError ? String(loadError) : null

  return (
    <Dialog open={open} onClose={saving ? undefined : onClose} maxWidth="md" fullWidth>
      <DialogTitle>{t('fixtures.interzonalModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('fixtures.interzonalModal.description')}
        </Typography>

        {categories.length === 0 ? (
          <Alert severity="info">{t('fixtures.interzonalModal.noZones')}</Alert>
        ) : (
          <>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
              <FormControl size="small" sx={{ minWidth: 260 }} disabled={saving}>
                <InputLabel id="interzonal-category">{t('fixtures.interzonalModal.category')}</InputLabel>
                <Select
                  labelId="interzonal-category"
                  label={t('fixtures.interzonalModal.category')}
                  value={categoryKeyValue}
                  onChange={(e) => setCategoryKeyValue(e.target.value)}
                >
                  {categories.map((c) => (
                    <MenuItem key={c.key} value={c.key}>
                      {c.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 140 }} disabled={saving}>
                <InputLabel id="interzonal-round">{t('fixtures.interzonalModal.round')}</InputLabel>
                <Select
                  labelId="interzonal-round"
                  label={t('fixtures.interzonalModal.round')}
                  value={roundNumbers.includes(round) ? round : ''}
                  onChange={(e) => setRound(Number(e.target.value))}
                >
                  {roundNumbers.map((r) => (
                    <MenuItem key={r} value={r}>
                      {t('fixtures.cols.round')} {r}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>

            {error && (
              <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
                {error}
              </Alert>
            )}
            {loadErrorMessage && (
              <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
                {loadErrorMessage}
              </Alert>
            )}
            {(data?.warnings ?? []).length > 0 && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                {data!.warnings.map((w) => (
                  <Typography key={w} variant="body2">
                    {w}
                  </Typography>
                ))}
              </Alert>
            )}

            {isLoading && (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                <CircularProgress size={28} />
              </Box>
            )}

            {data && (
              <>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('fixtures.interzonalModal.pairs')}
                </Typography>
                {pairs.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {t('fixtures.interzonalModal.noPairs')}
                  </Typography>
                ) : (
                  <Table size="small" sx={{ mb: 2 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>{t('fixtures.cols.home')}</TableCell>
                        <TableCell>{t('fixtures.cols.away')}</TableCell>
                        <TableCell>{t('fixtures.interzonalModal.slot')}</TableCell>
                        <TableCell align="right" />
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {pairs.map((p, index) => (
                        <TableRow key={`${p.homeTeamDivisionSeasonId}-${p.awayTeamDivisionSeasonId}`}>
                          <TableCell>{p.homeTeamName}</TableCell>
                          <TableCell>{p.awayTeamName}</TableCell>
                          <TableCell>
                            {p.fixtureId
                              ? [p.fieldName, formatTime(p.startTime)].filter(Boolean).join(' \u00b7 ') || '-'
                              : t('fixtures.interzonalModal.new')}
                          </TableCell>
                          <TableCell align="right">
                            {p.isLocked ? (
                              <Tooltip title={t('fixtures.interzonalModal.lockedHint')}>
                                <Chip size="small" icon={<LockIcon />} label={t('fixtures.interzonalModal.locked')} />
                              </Tooltip>
                            ) : (
                              <IconButton size="small" onClick={() => handleRemove(index)} disabled={saving}>
                                <DeleteOutlineIcon fontSize="small" />
                              </IconButton>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}

                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('fixtures.interzonalModal.freeTeams')}
                </Typography>
                <Stack spacing={1} sx={{ mb: 2 }}>
                  {freeByZone.map(({ zone, teams }) => (
                    <Box key={zone.divisionSeasonId}>
                      <Typography variant="caption" color="text.secondary">
                        {zone.divisionName}
                      </Typography>
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 0.5 }}>
                        {teams.length === 0 ? (
                          <Typography variant="body2" color="text.secondary">
                            {t('fixtures.interzonalModal.noFreeTeams')}
                          </Typography>
                        ) : (
                          teams.map((tm) => (
                            <Chip key={tm.teamDivisionSeasonId} size="small" variant="outlined" label={tm.teamName} />
                          ))
                        )}
                      </Box>
                    </Box>
                  ))}
                </Stack>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems="center">
                  <FormControl size="small" sx={{ minWidth: 220 }} disabled={saving}>
                    <InputLabel id="interzonal-home">{t('fixtures.cols.home')}</InputLabel>
                    <Select
                      labelId="interzonal-home"
                      label={t('fixtures.cols.home')}
                      value={homeId}
                      onChange={(e) => {
                        setHomeId(e.target.value)
                        setAwayId('')
                      }}
                    >
                      {freeTeams.map((tm) => (
                        <MenuItem key={tm.teamDivisionSeasonId} value={tm.teamDivisionSeasonId}>
                          {tm.teamName} ({tm.zoneName})
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  <FormControl size="small" sx={{ minWidth: 220 }} disabled={saving || !homeId}>
                    <InputLabel id="interzonal-away">{t('fixtures.cols.away')}</InputLabel>
                    <Select
                      labelId="interzonal-away"
                      label={t('fixtures.cols.away')}
                      value={awayId}
                      onChange={(e) => setAwayId(e.target.value)}
                    >
                      {awayCandidates.map((tm) => (
                        <MenuItem key={tm.teamDivisionSeasonId} value={tm.teamDivisionSeasonId}>
                          {tm.teamName} ({tm.zoneName})
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  <Button startIcon={<AddIcon />} onClick={handleAdd} disabled={saving || !homeId || !awayId}>
                    {t('fixtures.interzonalModal.add')}
                  </Button>
                </Stack>
              </>
            )}
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={saving}>
          {t('fixtures.interzonalModal.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void handleSave()} disabled={!dirty || saving}>
          {saving ? <CircularProgress size={22} color="inherit" /> : t('fixtures.interzonalModal.save')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
