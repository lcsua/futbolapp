import { useEffect, useState } from 'react'
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  TextField,
  Typography,
} from '@mui/material'
import { useTranslation } from 'react-i18next'
import { fixturesService } from '../api/fixtures'

interface AssignFixtureDatesModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  initialDivisionId: string
  divisions: { id: string; name: string }[]
  matchDaysText: string
  hasMatchDays: boolean
  onSuccess: (updatedCount: number, roundCount: number) => void
}

export function AssignFixtureDatesModal({
  open,
  onClose,
  leagueId,
  seasonId,
  initialDivisionId,
  divisions,
  matchDaysText,
  hasMatchDays,
  onSuccess,
}: AssignFixtureDatesModalProps) {
  const { t } = useTranslation()
  const [firstRoundDate, setFirstRoundDate] = useState('')
  const [scope, setScope] = useState<'all' | 'division'>('all')
  const [divisionId, setDivisionId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setError(null)
    setFirstRoundDate('')
    if (initialDivisionId) {
      setScope('division')
      setDivisionId(initialDivisionId)
    } else {
      setScope('all')
      setDivisionId('')
    }
  }, [open, initialDivisionId])

  const canSubmit =
    !!firstRoundDate && hasMatchDays && (scope === 'all' || !!divisionId) && !loading

  const handleAssign = async () => {
    if (!canSubmit) return
    setLoading(true)
    setError(null)
    try {
      const res = await fixturesService.assignDates(leagueId, seasonId, {
        firstRoundDate,
        divisionId: scope === 'division' ? divisionId : null,
      })
      onSuccess(res.updatedCount, res.roundCount)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.assignDatesModal.failed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('fixtures.assignDatesModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('fixtures.assignDatesModal.description')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
            {error}
          </Alert>
        )}

        {!hasMatchDays ? (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('fixtures.assignDatesModal.noMatchDays')}
          </Alert>
        ) : (
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('fixtures.assignDatesModal.matchDaysInfo', { days: matchDaysText })}
          </Alert>
        )}

        <TextField
          label={t('fixtures.assignDatesModal.firstRoundDate')}
          type="date"
          fullWidth
          size="small"
          value={firstRoundDate}
          onChange={(e) => setFirstRoundDate(e.target.value)}
          disabled={loading || !hasMatchDays}
          InputLabelProps={{ shrink: true }}
          sx={{ mb: 2 }}
        />

        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          {t('fixtures.assignDatesModal.scope')}
        </Typography>
        <RadioGroup
          value={scope}
          onChange={(_, v) => setScope(v as 'all' | 'division')}
          sx={{ mb: 2 }}
        >
          <FormControlLabel
            value="all"
            control={<Radio size="small" disabled={loading} />}
            label={t('fixtures.assignDatesModal.scopeAll')}
          />
          <FormControlLabel
            value="division"
            control={<Radio size="small" disabled={loading} />}
            label={t('fixtures.assignDatesModal.scopeDivision')}
          />
        </RadioGroup>

        {scope === 'division' && (
          <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={loading}>
            <InputLabel id="assign-dates-division">{t('fixtures.division')}</InputLabel>
            <Select
              labelId="assign-dates-division"
              label={t('fixtures.division')}
              value={divisionId}
              onChange={(e) => setDivisionId(e.target.value)}
            >
              {divisions.map((d) => (
                <MenuItem key={d.id} value={d.id}>
                  {d.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        <Alert severity="warning">
          {t('fixtures.assignDatesModal.overwriteWarning')}
        </Alert>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          {t('fixtures.assignDatesModal.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void handleAssign()} disabled={!canSubmit}>
          {loading ? <CircularProgress size={22} color="inherit" /> : t('fixtures.assignDatesModal.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
