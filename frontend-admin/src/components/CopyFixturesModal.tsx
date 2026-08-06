import { useEffect, useMemo, useState } from 'react'
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
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  Typography,
} from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { Season } from '../api/types'
import { fixturesService } from '../api/fixtures'

interface CopyFixturesModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  targetSeasonId: string
  seasons: Season[]
  /** Preselected division from FixturesPage filter; empty = user can choose all or one. */
  initialDivisionId: string
  divisions: { id: string; name: string }[]
  onSuccess: (copiedCount: number) => void
}

export function CopyFixturesModal({
  open,
  onClose,
  leagueId,
  targetSeasonId,
  seasons,
  initialDivisionId,
  divisions,
  onSuccess,
}: CopyFixturesModalProps) {
  const { t } = useTranslation()
  const sourceOptions = useMemo(
    () => seasons.filter((s) => s.id !== targetSeasonId),
    [seasons, targetSeasonId]
  )

  const [sourceSeasonId, setSourceSeasonId] = useState('')
  const [scope, setScope] = useState<'all' | 'division'>('all')
  const [divisionId, setDivisionId] = useState('')
  const [invertHomes, setInvertHomes] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setError(null)
    setInvertHomes(false)
    setSourceSeasonId(sourceOptions[0]?.id ?? '')
    if (initialDivisionId) {
      setScope('division')
      setDivisionId(initialDivisionId)
    } else {
      setScope('all')
      setDivisionId('')
    }
  }, [open, initialDivisionId, sourceOptions])

  const canSubmit = !!sourceSeasonId && (scope === 'all' || !!divisionId) && !loading

  const handleCopy = async () => {
    if (!canSubmit) return
    setLoading(true)
    setError(null)
    try {
      const res = await fixturesService.copyFromSeason(leagueId, targetSeasonId, {
        sourceSeasonId,
        divisionId: scope === 'division' ? divisionId : null,
        invertHomes,
      })
      onSuccess(res.copiedCount)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.copyModal.failed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('fixtures.copyModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('fixtures.copyModal.description')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }}>
            {error}
          </Alert>
        )}

        <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={loading || sourceOptions.length === 0}>
          <InputLabel id="copy-source-season">{t('fixtures.copyModal.sourceSeason')}</InputLabel>
          <Select
            labelId="copy-source-season"
            label={t('fixtures.copyModal.sourceSeason')}
            value={sourceSeasonId}
            onChange={(e) => setSourceSeasonId(e.target.value)}
          >
            {sourceOptions.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {sourceOptions.length === 0 && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('fixtures.copyModal.noSource')}
          </Alert>
        )}

        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          {t('fixtures.copyModal.scope')}
        </Typography>
        <RadioGroup
          value={scope}
          onChange={(_, v) => setScope(v as 'all' | 'division')}
          sx={{ mb: 2 }}
        >
          <FormControlLabel
            value="all"
            control={<Radio size="small" disabled={loading} />}
            label={t('fixtures.copyModal.scopeAll')}
          />
          <FormControlLabel
            value="division"
            control={<Radio size="small" disabled={loading} />}
            label={t('fixtures.copyModal.scopeDivision')}
          />
        </RadioGroup>

        {scope === 'division' && (
          <FormControl fullWidth size="small" sx={{ mb: 2 }} disabled={loading}>
            <InputLabel id="copy-division">{t('fixtures.division')}</InputLabel>
            <Select
              labelId="copy-division"
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

        <FormControlLabel
          control={
            <Checkbox
              checked={invertHomes}
              onChange={(e) => setInvertHomes(e.target.checked)}
              disabled={loading}
            />
          }
          label={t('fixtures.copyModal.invertHomes')}
        />
        <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4, mb: 1 }}>
          {t('fixtures.copyModal.invertHomesHint')}
        </Typography>

        <Alert severity="warning" sx={{ mt: 1 }}>
          {t('fixtures.copyModal.replaceWarning')}
        </Alert>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          {t('fixtures.copyModal.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void handleCopy()} disabled={!canSubmit}>
          {loading ? <CircularProgress size={22} color="inherit" /> : t('fixtures.copyModal.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
