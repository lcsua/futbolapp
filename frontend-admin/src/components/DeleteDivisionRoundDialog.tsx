import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { matchesService } from '../api/matches'
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
  initialDivisionId: string
  rounds: FixtureDraftRound[]
  onClose: () => void
  onDeleted: (message: string) => void
  onError: (message: string) => void
}

function formatRoundDate(dateStr: string | null | undefined, locale: string): string {
  if (!dateStr) return ''
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateStr)
  if (!m) return dateStr
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])).toLocaleDateString(locale, {
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
  initialDivisionId,
  rounds,
  onClose,
  onDeleted,
  onError,
}: DeleteDivisionRoundDialogProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [divisionId, setDivisionId] = useState(initialDivisionId)
  const [round, setRound] = useState('')
  const [confirmText, setConfirmText] = useState('')

  useEffect(() => {
    if (!open) return
    setDivisionId(initialDivisionId)
    setRound('')
    setConfirmText('')
  }, [open, initialDivisionId])

  const divisionName = divisions.find((d) => d.id === divisionId)?.name ?? ''
  const dateLocale = i18n.language?.toLowerCase().startsWith('es') ? 'es-AR' : 'en-US'

  const ownedByRound = useMemo(() => {
    return rounds
      .map((item) => ({
        roundNumber: item.roundNumber,
        matchDate: item.matchDate,
        matches: item.matches.filter((m) => m.divisionName === divisionName),
        keptInterzonals: item.matches.filter(
          (m) => m.isInterzonal && m.awayDivisionName === divisionName && m.divisionName !== divisionName,
        ),
      }))
      .filter((item) => item.matches.length > 0)
  }, [rounds, divisionName])

  useEffect(() => {
    if (!ownedByRound.some((item) => String(item.roundNumber) === round)) setRound('')
  }, [ownedByRound, round])

  const selected = ownedByRound.find((item) => String(item.roundNumber) === round) ?? null

  const { data: matchesData } = useQuery({
    queryKey: ['leagues', leagueId, 'matches', seasonId, divisionId || null],
    queryFn: ({ signal }) => matchesService.getMatches(leagueId, { seasonId, divisionId }, signal),
    enabled: open && !!leagueId && !!seasonId && !!divisionId,
  })

  const withResults = useMemo(() => {
    if (!selected) return 0
    return (matchesData?.rounds ?? [])
      .filter((group) => group.roundNumber === selected.roundNumber && group.divisionName === divisionName)
      .flatMap((group) => group.matches)
      .filter(
        (m) =>
          m.homeScore != null ||
          m.awayScore != null ||
          (m.status && m.status.toUpperCase() !== 'SCHEDULED'),
      ).length
  }, [matchesData, selected, divisionName])

  const canConfirm =
    !!divisionId &&
    !!selected &&
    selected.matches.length > 0 &&
    confirmText.trim() === DELETE_ROUND_CONFIRM_WORD

  const mutation = useMutation({
    mutationFn: () =>
      matchesService.deleteDivisionRound(leagueId, {
        seasonId,
        divisionId,
        round: selected!.roundNumber,
        confirmation: DELETE_ROUND_CONFIRM_WORD,
      }),
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
          round: selected?.roundNumber,
          division: divisionName,
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
        <Alert severity="error" sx={{ mb: 2 }}>
          {t('fixtures.deleteRound.warning')}
        </Alert>

        <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={mutation.isPending}>
          <InputLabel id="delete-round-division">{t('fixtures.division')}</InputLabel>
          <Select
            labelId="delete-round-division"
            label={t('fixtures.division')}
            value={divisionId}
            onChange={(e) => {
              setDivisionId(e.target.value)
              setConfirmText('')
            }}
          >
            <MenuItem value="">
              <em>{t('fixtures.selectDivision')}</em>
            </MenuItem>
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={mutation.isPending || !divisionId}>
          <InputLabel id="delete-round-number">{t('fixtures.deleteRound.round')}</InputLabel>
          <Select
            labelId="delete-round-number"
            label={t('fixtures.deleteRound.round')}
            value={round}
            onChange={(e) => {
              setRound(e.target.value)
              setConfirmText('')
            }}
          >
            <MenuItem value="">
              <em>{t('fixtures.deleteRound.selectRound')}</em>
            </MenuItem>
            {ownedByRound.map((item) => {
              const dateLabel = formatRoundDate(item.matchDate, dateLocale)
              const label = dateLabel
                ? t('fixtures.deleteRound.roundOption', {
                    round: item.roundNumber,
                    count: item.matches.length,
                    date: dateLabel,
                  })
                : t('fixtures.deleteRound.roundOptionNoDate', {
                    round: item.roundNumber,
                    count: item.matches.length,
                  })
              return (
                <MenuItem key={item.roundNumber} value={String(item.roundNumber)}>
                  {label}
                </MenuItem>
              )
            })}
          </Select>
        </FormControl>

        {divisionId && ownedByRound.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            {t('fixtures.deleteRound.emptyDivision')}
          </Typography>
        )}

        {selected && (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {t('fixtures.deleteRound.matchList', { count: selected.matches.length })}
            </Typography>
            <Box
              component="ul"
              sx={{
                maxHeight: 220,
                overflow: 'auto',
                border: 1,
                borderColor: 'divider',
                borderRadius: 1,
                px: 2.5,
                py: 1,
                mb: 2,
                mt: 0,
              }}
            >
              {selected.matches.map((m) => (
                <Typography
                  key={`${m.homeTeamDivisionSeasonId}-${m.awayTeamDivisionSeasonId}`}
                  component="li"
                  variant="body2"
                  sx={{ py: 0.25 }}
                >
                  {m.homeTeamName} vs {m.awayTeamName}
                  {m.isInterzonal ? ` (${t('fixtures.interzonal')})` : ''}
                </Typography>
              ))}
            </Box>
            {selected.matches.some((m) => m.isInterzonal) && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                {t('fixtures.deleteRound.ownedInterzonals')}
              </Alert>
            )}
            {selected.keptInterzonals.length > 0 && (
              <Alert severity="info" sx={{ mb: 2 }}>
                {t('fixtures.deleteRound.keptInterzonals', { count: selected.keptInterzonals.length })}
              </Alert>
            )}
            {withResults > 0 && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                {t('fixtures.deleteRound.withResults', { count: withResults })}
              </Alert>
            )}
          </>
        )}

        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('fixtures.deleteRound.typeToConfirm', { word: DELETE_ROUND_CONFIRM_WORD })}
        </Typography>
        <TextField
          autoFocus
          fullWidth
          label={t('fixtures.deleteRound.confirmLabel')}
          placeholder={DELETE_ROUND_CONFIRM_WORD}
          value={confirmText}
          onChange={(e) => setConfirmText(e.target.value)}
          disabled={mutation.isPending || !selected}
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
