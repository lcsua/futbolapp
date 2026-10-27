import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
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
  Typography,
} from '@mui/material'
import { useTranslation } from 'react-i18next'
import { fixturesService } from '../api/fixtures'

export interface ShiftDateOption {
  date: string
  count: number
}

interface ShiftFixtureDatesModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  dates: ShiftDateOption[]
  onSuccess: (updatedCount: number, skippedCount: number) => void
}

function todayIso(): string {
  const now = new Date()
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  const dd = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${mm}-${dd}`
}

function formatDate(dateStr: string, locale: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(dateStr)
  if (!m) return dateStr
  const year = parseInt(m[1], 10)
  const month = parseInt(m[2], 10) - 1
  const day = parseInt(m[3], 10)
  return new Date(year, month, day).toLocaleDateString(locale, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function ShiftFixtureDatesModal({
  open,
  onClose,
  leagueId,
  seasonId,
  dates,
  onSuccess,
}: ShiftFixtureDatesModalProps) {
  const { t, i18n } = useTranslation()
  const [fromDate, setFromDate] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const locale = i18n.language?.toLowerCase().startsWith('es') ? 'es-AR' : 'en-US'

  useEffect(() => {
    if (!open) return
    setError(null)
    const today = todayIso()
    const eligible = dates.filter((d) => d.date <= today)
    setFromDate(eligible.length > 0 ? eligible[eligible.length - 1].date : '')
  }, [open, dates])

  const affectedCount = useMemo(
    () => dates.filter((d) => fromDate && d.date >= fromDate).reduce((sum, d) => sum + d.count, 0),
    [dates, fromDate],
  )

  const canSubmit = !!fromDate && !loading

  const handleShift = async () => {
    if (!canSubmit) return
    setLoading(true)
    setError(null)
    try {
      const res = await fixturesService.shiftDates(leagueId, seasonId, { fromDate })
      onSuccess(res.updatedCount, res.skippedCount)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.shiftDatesModal.failed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('fixtures.shiftDatesModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('fixtures.shiftDatesModal.description')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
            {error}
          </Alert>
        )}

        {dates.length === 0 ? (
          <Alert severity="warning">{t('fixtures.shiftDatesModal.noDates')}</Alert>
        ) : (
          <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={loading}>
            <InputLabel id="shift-dates-from">{t('fixtures.shiftDatesModal.fromDate')}</InputLabel>
            <Select
              labelId="shift-dates-from"
              label={t('fixtures.shiftDatesModal.fromDate')}
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            >
              {dates.map((d) => (
                <MenuItem key={d.date} value={d.date}>
                  {t('fixtures.shiftDatesModal.option', {
                    date: formatDate(d.date, locale),
                    count: d.count,
                  })}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        {fromDate && (
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('fixtures.shiftDatesModal.preview', {
              date: formatDate(fromDate, locale),
              count: affectedCount,
            })}
          </Alert>
        )}

        <Alert severity="warning">{t('fixtures.shiftDatesModal.playedWarning')}</Alert>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          {t('fixtures.shiftDatesModal.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void handleShift()} disabled={!canSubmit}>
          {loading ? <CircularProgress size={22} color="inherit" /> : t('fixtures.shiftDatesModal.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
