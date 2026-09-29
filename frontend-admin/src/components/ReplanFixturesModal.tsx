import { useEffect, useState } from 'react'
import {
  Alert,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormHelperText,
  InputLabel,
  ListItemText,
  MenuItem,
  Select,
  TextField,
  Typography,
} from '@mui/material'
import { useTranslation } from 'react-i18next'
import { fixturesService, type FixtureDraft } from '../api/fixtures'
import { zoneGroupIds } from '../utils/zones'

interface ReplanFixturesModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  initialDivisionId: string
  divisions: { id: string; name: string }[]
  suggestedFromRound: number
  onSuccess: (draft: FixtureDraft) => void
}

export function ReplanFixturesModal({
  open,
  onClose,
  leagueId,
  seasonId,
  initialDivisionId,
  divisions,
  suggestedFromRound,
  onSuccess,
}: ReplanFixturesModalProps) {
  const { t } = useTranslation()
  const [divisionIds, setDivisionIds] = useState<string[]>([])
  const [fromRound, setFromRound] = useState('')
  const [fillByesWithInterzonal, setFillByesWithInterzonal] = useState(true)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setError(null)
    setFromRound(String(suggestedFromRound))
    setFillByesWithInterzonal(true)
    const group = zoneGroupIds(divisions, initialDivisionId)
    setDivisionIds((prev) => (group.length === 0 && prev.length === 0 ? prev : group))
  }, [open, initialDivisionId, divisions, suggestedFromRound])

  const fromRoundNumber = Number.parseInt(fromRound, 10)
  const canSubmit = divisionIds.length > 0 && Number.isFinite(fromRoundNumber) && fromRoundNumber >= 1 && !loading

  const handleSubmit = async () => {
    if (!canSubmit) return
    setLoading(true)
    setError(null)
    try {
      const draft = await fixturesService.replan(leagueId, seasonId, {
        divisionIds,
        fromRound: fromRoundNumber,
        fillByesWithInterzonal,
      })
      onSuccess(draft)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.replanModal.failed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('fixtures.replanModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('fixtures.replanModal.description')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
            {error}
          </Alert>
        )}

        <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={loading}>
          <InputLabel id="replan-zones">{t('fixtures.replanModal.zones')}</InputLabel>
          <Select
            labelId="replan-zones"
            label={t('fixtures.replanModal.zones')}
            multiple
            value={divisionIds}
            onChange={(e) => {
              const value = e.target.value
              setDivisionIds(typeof value === 'string' ? value.split(',') : value)
            }}
            renderValue={(selected) =>
              divisions
                .filter((d) => selected.includes(d.id))
                .map((d) => d.name)
                .join(', ')
            }
          >
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                <Checkbox size="small" checked={divisionIds.includes(d.id)} />
                <ListItemText primary={d.name} />
              </MenuItem>
            ))}
          </Select>
          <FormHelperText>{t('fixtures.replanModal.zonesHint')}</FormHelperText>
        </FormControl>

        <TextField
          label={t('fixtures.replanModal.fromRound')}
          type="number"
          size="small"
          fullWidth
          value={fromRound}
          onChange={(e) => setFromRound(e.target.value)}
          disabled={loading}
          inputProps={{ min: 1 }}
          helperText={t('fixtures.replanModal.fromRoundHint')}
          sx={{ mb: 2 }}
        />

        <FormControlLabel
          control={
            <Checkbox
              checked={fillByesWithInterzonal}
              onChange={(e) => setFillByesWithInterzonal(e.target.checked)}
              disabled={loading || divisionIds.length < 2}
            />
          }
          label={t('fixtures.replanModal.interzonal')}
        />
        <FormHelperText sx={{ mb: 2 }}>{t('fixtures.replanModal.interzonalHint')}</FormHelperText>

        <Alert severity="info">{t('fixtures.replanModal.keepsPlayed')}</Alert>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          {t('fixtures.replanModal.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void handleSubmit()} disabled={!canSubmit}>
          {loading ? <CircularProgress size={22} color="inherit" /> : t('fixtures.replanModal.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
