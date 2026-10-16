import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { matchesService, type MatchListItem } from '../api/matches'
import type { FixtureDraftRound } from '../api/fixtures'

export const DELETE_ROUND_CONFIRM_WORD = 'ELIMINAR'

interface DivisionOption {
  id: string
  name: string
}

interface DeleteDivisionRoundDialogProps {
  open: boolean
  leagueId: string
  seasonId: string
  divisions: DivisionOption[]
  rounds: FixtureDraftRound[]
  onClose: () => void
  onDeleted: (message: string) => void
  onError: (message: string) => void
}

interface RoundPair {
  divisionId: string
  divisionName: string
  roundNumber: number
  matchDate?: string | null
  matchCount: number
  withResults: number
  hasInterzonal: boolean
}

function pairKey(divisionId: string, roundNumber: number) {
  return `${divisionId}:${roundNumber}`
}

function matchHasLoadedResult(match: MatchListItem) {
  if (match.homeScore != null || match.awayScore != null) return true
  return (match.status || 'SCHEDULED').toUpperCase() !== 'SCHEDULED'
}

function formatRoundDate(dateStr: string | null | undefined, locale: string): string {
  if (!dateStr) return ''
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateStr)
  if (!match) return dateStr
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])).toLocaleDateString(locale, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
  })
}

export function DeleteDivisionRoundDialog({
  open,
  leagueId,
  seasonId,
  divisions,
  rounds,
  onClose,
  onDeleted,
  onError,
}: DeleteDivisionRoundDialogProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [allDivisions, setAllDivisions] = useState(true)
  const [divisionIds, setDivisionIds] = useState<Set<string>>(() => new Set())
  const [roundNumbers, setRoundNumbers] = useState<Set<number>>(() => new Set())
  const [confirmText, setConfirmText] = useState('')

  const dateLocale = i18n.language?.toLowerCase().startsWith('es') ? 'es-AR' : 'en-US'

  const { data: matchesData, isSuccess, isError, isLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, 'delete-round'],
    queryFn: ({ signal }) => matchesService.getMatches(leagueId, { seasonId }, signal),
    enabled: open && !!leagueId && !!seasonId,
  })

  useEffect(() => {
    if (!open) return
    setAllDivisions(true)
    setDivisionIds(new Set())
    setRoundNumbers(new Set())
    setConfirmText('')
  }, [open])

  const pairs = useMemo(() => {
    const divisionIdByName = new Map(divisions.map((division) => [division.name, division.id]))
    const resultsByKey = new Map<string, number>()
    for (const group of matchesData?.rounds ?? []) {
      const divisionId = divisionIdByName.get(group.divisionName)
      if (!divisionId) continue
      resultsByKey.set(
        pairKey(divisionId, group.roundNumber),
        group.matches.filter(matchHasLoadedResult).length,
      )
    }

    const built: RoundPair[] = []
    for (const round of rounds) {
      const byDivision = new Map<string, RoundPair>()
      for (const match of round.matches) {
        const divisionId = divisionIdByName.get(match.divisionName)
        if (!divisionId) continue
        const key = pairKey(divisionId, round.roundNumber)
        const current = byDivision.get(key) ?? {
          divisionId,
          divisionName: match.divisionName,
          roundNumber: round.roundNumber,
          matchDate: round.matchDate,
          matchCount: 0,
          withResults: resultsByKey.get(key) ?? 0,
          hasInterzonal: false,
        }
        current.matchCount += 1
        current.hasInterzonal = current.hasInterzonal || !!match.isInterzonal
        byDivision.set(key, current)
      }
      built.push(...byDivision.values())
    }
    return built
  }, [divisions, rounds, matchesData])

  const divisionsInFixture = useMemo(() => {
    const ids = new Set(pairs.map((pair) => pair.divisionId))
    return divisions.filter((division) => ids.has(division.id))
  }, [divisions, pairs])

  const selectedDivisionIds = allDivisions
    ? new Set(divisionsInFixture.map((division) => division.id))
    : divisionIds

  const roundOptions = useMemo(() => {
    const groups = new Map<number, RoundPair[]>()
    for (const pair of pairs) {
      if (!selectedDivisionIds.has(pair.divisionId)) continue
      const list = groups.get(pair.roundNumber) ?? []
      list.push(pair)
      groups.set(pair.roundNumber, list)
    }
    return Array.from(groups.entries())
      .map(([roundNumber, items]) => ({
        roundNumber,
        matchDate: items[0]?.matchDate,
        matchCount: items.reduce((sum, item) => sum + item.matchCount, 0),
        withResults: items.reduce((sum, item) => sum + item.withResults, 0),
        hasInterzonal: items.some((item) => item.hasInterzonal),
      }))
      .sort((a, b) => a.roundNumber - b.roundNumber)
  }, [pairs, selectedDivisionIds])

  const selectedRounds = roundOptions.filter((round) => roundNumbers.has(round.roundNumber))
  const selectedMatchCount = selectedRounds.reduce((sum, round) => sum + round.matchCount, 0)
  const selectedWithResults = selectedRounds.reduce((sum, round) => sum + round.withResults, 0)
  const hasInterzonal = selectedRounds.some((round) => round.hasInterzonal)

  const clearConfirm = () => setConfirmText('')

  const toggleDivision = (divisionId: string) => {
    const next = new Set(divisionIds)
    if (next.has(divisionId)) next.delete(divisionId)
    else next.add(divisionId)
    setAllDivisions(false)
    setDivisionIds(next)
    clearConfirm()
  }

  const toggleRound = (roundNumber: number) => {
    const next = new Set(roundNumbers)
    if (next.has(roundNumber)) next.delete(roundNumber)
    else next.add(roundNumber)
    setRoundNumbers(next)
    clearConfirm()
  }

  const selectRoundsWithoutResults = () => {
    setRoundNumbers(new Set(roundOptions.filter((round) => round.withResults === 0).map((round) => round.roundNumber)))
    clearConfirm()
  }

  const canConfirm =
    isSuccess &&
    selectedDivisionIds.size > 0 &&
    selectedRounds.length > 0 &&
    confirmText.trim() === DELETE_ROUND_CONFIRM_WORD

  const mutation = useMutation({
    mutationFn: () => {
      const chosenRounds = new Set(selectedRounds.map((round) => round.roundNumber))
      const byDivision = new Map<string, number[]>()
      for (const pair of pairs) {
        if (!selectedDivisionIds.has(pair.divisionId) || !chosenRounds.has(pair.roundNumber)) continue
        const list = byDivision.get(pair.divisionId) ?? []
        list.push(pair.roundNumber)
        byDivision.set(pair.divisionId, list)
      }
      return matchesService.deleteDivisionRound(leagueId, {
        seasonId,
        confirmation: confirmText.trim(),
        targets: Array.from(byDivision, ([divisionId, roundsForDivision]) => ({
          divisionId,
          rounds: roundsForDivision,
        })),
      })
    },
    onSuccess: async (res) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] }),
        queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'matches'] }),
        queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'standings'] }),
        queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] }),
      ])
      onDeleted(
        t('fixtures.deleteRound.success', {
          count: res.deletedCount,
          divisions: selectedDivisionIds.size,
          rounds: selectedRounds.length,
          results: res.resultsRemovedCount,
        }),
      )
      onClose()
    },
    onError: (err) => {
      onError(err instanceof Error ? err.message : t('fixtures.deleteRound.failed'))
    },
  })

  return (
    <Dialog open={open} onClose={() => !mutation.isPending && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle>{t('fixtures.deleteRound.title')}</DialogTitle>
      <DialogContent>
        <Alert severity="warning" sx={{ mb: 2 }}>
          {t('fixtures.deleteRound.warning')}
        </Alert>

        {isLoading && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
            <CircularProgress size={22} />
            <Typography variant="body2" color="text.secondary">
              {t('fixtures.deleteRound.loadingResults')}
            </Typography>
          </Box>
        )}
        {isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {t('fixtures.deleteRound.resultsUnknown')}
          </Alert>
        )}

        {isSuccess && (
          <>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              {t('fixtures.deleteRound.divisions')}
            </Typography>
            <FormControlLabel
              control={
                <Checkbox
                  checked={allDivisions}
                  onChange={() => {
                    setAllDivisions((current) => !current)
                    clearConfirm()
                  }}
                  disabled={mutation.isPending}
                />
              }
              label={t('fixtures.deleteRound.allDivisions')}
            />
            {!allDivisions && (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', pl: 1, mb: 1 }}>
                {divisionsInFixture.map((division) => (
                  <FormControlLabel
                    key={division.id}
                    control={
                      <Checkbox
                        checked={divisionIds.has(division.id)}
                        onChange={() => toggleDivision(division.id)}
                        disabled={mutation.isPending}
                      />
                    }
                    label={division.name}
                  />
                ))}
              </Box>
            )}

            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mt: 1, mb: 0.5 }}>
              <Typography variant="subtitle2">{t('fixtures.deleteRound.rounds')}</Typography>
              <Button size="small" onClick={selectRoundsWithoutResults} disabled={mutation.isPending || selectedDivisionIds.size === 0}>
                {t('fixtures.deleteRound.selectWithoutResults')}
              </Button>
            </Box>
            <Box sx={{ maxHeight: 320, overflow: 'auto', border: 1, borderColor: 'divider', borderRadius: 1, mb: 2 }}>
              {roundOptions.map((round) => {
                const dateLabel = formatRoundDate(round.matchDate, dateLocale)
                const title = dateLabel
                  ? t('fixtures.deleteRound.roundLabelDate', { round: round.roundNumber, date: dateLabel })
                  : t('fixtures.deleteRound.roundLabel', { round: round.roundNumber })
                return (
                  <FormControlLabel
                    key={round.roundNumber}
                    sx={{ display: 'flex', mx: 0, px: 1, borderBottom: 1, borderColor: 'divider' }}
                    control={
                      <Checkbox
                        checked={roundNumbers.has(round.roundNumber)}
                        onChange={() => toggleRound(round.roundNumber)}
                        disabled={mutation.isPending}
                        color={round.withResults > 0 ? 'warning' : 'primary'}
                      />
                    }
                    label={
                      round.withResults > 0
                        ? `${title} · ${t('fixtures.deleteRound.matchCount', { count: round.matchCount })} · ${t('fixtures.deleteRound.withResults')}`
                        : `${title} · ${t('fixtures.deleteRound.matchCount', { count: round.matchCount })}`
                    }
                  />
                )
              })}
              {roundOptions.length === 0 && (
                <Typography variant="body2" color="text.secondary" sx={{ p: 1.5 }}>
                  {t('fixtures.deleteRound.noRounds')}
                </Typography>
              )}
            </Box>

            {selectedRounds.length > 0 && (
              <Alert severity={selectedWithResults > 0 ? 'warning' : 'info'} sx={{ mb: 2 }}>
                {t('fixtures.deleteRound.summary', {
                  count: selectedMatchCount,
                  divisions: selectedDivisionIds.size,
                  rounds: selectedRounds.length,
                })}
                {selectedWithResults > 0
                  ? ` ${t('fixtures.deleteRound.withResultsWarning', { count: selectedWithResults })}`
                  : ''}
              </Alert>
            )}
            {hasInterzonal && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                {t('fixtures.deleteRound.ownedInterzonals')}
              </Alert>
            )}
          </>
        )}

        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('fixtures.deleteRound.typeToConfirm', { word: DELETE_ROUND_CONFIRM_WORD })}
        </Typography>
        <TextField
          fullWidth
          label={t('fixtures.deleteRound.confirmLabel')}
          placeholder={DELETE_ROUND_CONFIRM_WORD}
          value={confirmText}
          onChange={(e) => setConfirmText(e.target.value)}
          disabled={mutation.isPending || !isSuccess || selectedDivisionIds.size === 0 || selectedRounds.length === 0}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={mutation.isPending}>
          {t('fixtures.deleteRound.cancel')}
        </Button>
        <Button
          color="error"
          variant="contained"
          disabled={!canConfirm || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? <CircularProgress size={22} color="inherit" /> : t('fixtures.deleteRound.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
