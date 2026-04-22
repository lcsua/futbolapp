import { useEffect, useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  FormControl,
  IconButton,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  TextField,
  Typography,
  type SelectChangeEvent,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { Link as RouterLink, useLocation, useParams } from 'react-router-dom'
import { useLeagueId } from '../contexts/LeagueContext'
import { schedulingExtrasService, type UpsertDivisionSchedulingExtrasBody } from '../api/schedulingExtras'
import { fieldsService } from '../api/fields'
import { useTranslation } from 'react-i18next'
import {
  type KickoffRangeRow,
  hasInvalidFilledRange,
  kickoffRangesToAllowedJson,
  newKickoffRow,
  parseKickoffRangesJson,
} from '../utils/kickoffTimeRanges'

function optionalIntFromField(value: string): number | null {
  const t = value.trim()
  if (t === '') return null
  const n = parseInt(t, 10)
  return Number.isFinite(n) ? n : null
}

function numOrEmpty(v: number | null | undefined): string {
  return v != null ? String(v) : ''
}

export function DivisionSchedulingRulesPage() {
  const { t } = useTranslation()
  const leagueId = useLeagueId()
  const location = useLocation()
  const { seasonId, divisionId } = useParams<{ seasonId: string; divisionId: string }>()
  const hubPath =
    leagueId && seasonId
      ? location.pathname.includes('/leagues/')
        ? `/leagues/${leagueId}/seasons/${seasonId}/division-scheduling`
        : `/seasons/${seasonId}/division-scheduling`
      : '/seasons'
  const queryClient = useQueryClient()
  const [half, setHalf] = useState('')
  const [breakHalves, setBreakHalves] = useState('')
  const [warmup, setWarmup] = useState('')
  const [slotGran, setSlotGran] = useState('')
  const [firstTol, setFirstTol] = useState('')
  const [breakBetween, setBreakBetween] = useState('')
  const [kickoffRanges, setKickoffRanges] = useState<KickoffRangeRow[]>([])
  const [explicitIds, setExplicitIds] = useState<string[]>([])
  const [success, setSuccess] = useState(false)

  const idsReady = !!(leagueId && seasonId && divisionId)

  const { data: bundle, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'divisions', divisionId, 'scheduling-extras'],
    queryFn: ({ signal }) =>
      schedulingExtrasService.getDivisionBundle(leagueId!, seasonId!, divisionId!, signal),
    enabled: idsReady,
  })

  const { data: fieldsList } = useQuery({
    queryKey: ['leagues', leagueId, 'fields'],
    queryFn: ({ signal }) => fieldsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const fieldNameById = useMemo(() => {
    const m = new Map<string, string>()
    for (const f of fieldsList ?? []) m.set(f.id, f.name)
    return m
  }, [fieldsList])

  const globalDefaults = bundle?.globalMatchRule

  useEffect(() => {
    if (!bundle) return
    const ex = bundle.divisionExtras
    const g = bundle.globalMatchRule
    setHalf(numOrEmpty(ex?.halfMinutes ?? g.halfMinutes))
    setBreakHalves(numOrEmpty(ex?.breakMinutes ?? g.breakMinutes))
    setWarmup(numOrEmpty(ex?.warmupBufferMinutes ?? g.warmupBufferMinutes))
    setSlotGran(numOrEmpty(ex?.slotGranularityMinutes ?? g.slotGranularityMinutes))
    setFirstTol(numOrEmpty(ex?.firstMatchToleranceMinutes ?? g.firstMatchToleranceMinutes))
    setBreakBetween(ex?.breakBetweenMatchesMinutes != null ? String(ex.breakBetweenMatchesMinutes) : '')
    setKickoffRanges(parseKickoffRangesJson(ex?.allowedTimeRangesJson))
    setExplicitIds([...bundle.explicitFieldIds])
  }, [bundle])

  const putMutation = useMutation({
    mutationFn: (body: UpsertDivisionSchedulingExtrasBody) =>
      schedulingExtrasService.putDivisionExtras(leagueId!, seasonId!, divisionId!, body),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['leagues', leagueId, 'seasons', seasonId, 'divisions', divisionId, 'scheduling-extras'],
      })
      setSuccess(true)
      setTimeout(() => setSuccess(false), 2500)
    },
  })

  const applyGlobalDefaults = () => {
    if (!globalDefaults) return
    setHalf(String(globalDefaults.halfMinutes))
    setBreakHalves(String(globalDefaults.breakMinutes))
    setWarmup(String(globalDefaults.warmupBufferMinutes))
    setSlotGran(String(globalDefaults.slotGranularityMinutes))
    setFirstTol(String(globalDefaults.firstMatchToleranceMinutes))
    setBreakBetween('')
  }

  const buildRulesBody = (): UpsertDivisionSchedulingExtrasBody => ({
    halfMinutes: optionalIntFromField(half),
    breakMinutes: optionalIntFromField(breakHalves),
    warmupBufferMinutes: optionalIntFromField(warmup),
    slotGranularityMinutes: optionalIntFromField(slotGran),
    firstMatchToleranceMinutes: optionalIntFromField(firstTol),
    breakBetweenMatchesMinutes: optionalIntFromField(breakBetween),
    allowedTimeRangesJson: kickoffRangesToAllowedJson(kickoffRanges),
  })

  const handleSaveRules = () => {
    putMutation.mutate(buildRulesBody())
  }

  const handleSaveFieldsOnly = () => {
    putMutation.mutate({ explicitFieldIds: [...explicitIds] })
  }

  const handleClearDivisionRow = () => {
    putMutation.mutate({
      halfMinutes: null,
      breakMinutes: null,
      warmupBufferMinutes: null,
      slotGranularityMinutes: null,
      firstMatchToleranceMinutes: null,
      breakBetweenMatchesMinutes: null,
      allowedTimeRangesJson: null,
    })
  }

  const handleClearExplicitFields = () => {
    putMutation.mutate({ explicitFieldIds: [] })
  }

  const onExplicitChange = (e: SelectChangeEvent<string[]>) => {
    const v = e.target.value
    setExplicitIds(typeof v === 'string' ? v.split(',') : v)
  }

  if (!leagueId || !seasonId || !divisionId) {
    return <Alert severity="info">Missing league, season, or division in the URL.</Alert>
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {error instanceof Error ? error.message : 'Failed to load'}
      </Alert>
    )
  }

  const eff = bundle!.effectivePreview

  return (
    <Box sx={{ maxWidth: 800 }}>
      <Button component={RouterLink} to={hubPath} size="small" sx={{ mb: 2 }}>
        {t('divisionRules.backToDivisions')}
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        {t('divisionRules.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('nav.seasons')} <code>{seasonId}</code> · {t('nav.divisions')} <code>{divisionId}</code>
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('divisionRules.inheritanceInfo')}
      </Typography>

      {globalDefaults && (
        <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
            {t('divisionRules.globalDefaults')}
          </Typography>
          <Typography variant="body2">
            Halves {globalDefaults.halfMinutes} min · Halftime {globalDefaults.breakMinutes} min · Warmup{' '}
            {globalDefaults.warmupBufferMinutes} min · Slot {globalDefaults.derivedTotalMatchSlotMinutes} min block ·
            Granularity {globalDefaults.slotGranularityMinutes} min · First-match tolerance {globalDefaults.firstMatchToleranceMinutes}{' '}
            min
          </Typography>
          <Button size="small" sx={{ mt: 1 }} onClick={applyGlobalDefaults}>
            {t('divisionRules.copyGlobals')}
          </Button>
        </Paper>
      )}

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
          {t('divisionRules.preview')}
        </Typography>
        <Typography variant="body2">
          Total slot block: {eff.totalMatchSlotBlockMinutes} min · Break between matches: {eff.breakBetweenMatchesMinutes}{' '}
          min · Granularity: {eff.slotGranularityMinutes} min · First-match tolerance: {eff.firstMatchToleranceMinutes} min
        </Typography>
        {eff.allowedFieldIds?.length ? (
          <Typography variant="body2" sx={{ mt: 1 }}>
            {t('divisionRules.fieldsAllowed')} {eff.allowedFieldIds.join(', ')}
          </Typography>
        ) : (
          <Typography variant="body2" sx={{ mt: 1 }}>
            {t('divisionRules.fieldsAll')}
          </Typography>
        )}
        {eff.allowedKickoffTimeRanges?.length ? (
          <Box component="ul" sx={{ m: 0, pl: 2, mt: 1 }}>
            {eff.allowedKickoffTimeRanges.map((r, i) => (
              <li key={i}>
                <Typography variant="body2" component="span">
                  {r.start} – {r.end}
                </Typography>
              </li>
            ))}
          </Box>
        ) : null}
      </Paper>

      {success && (
        <Alert severity="success" sx={{ mb: 2 }}>
          {t('matchRules.saved')}
        </Alert>
      )}
      {putMutation.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {putMutation.error instanceof Error ? putMutation.error.message : t('common.saveError')}
        </Alert>
      )}

      <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
        {t('divisionRules.overrides')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('divisionRules.overridesInfo')}
      </Typography>

      <TextField fullWidth label={t('matchRules.halfMinutes')} value={half} onChange={(e) => setHalf(e.target.value)} sx={{ mb: 2 }} />
      <TextField
        fullWidth
        label={t('matchRules.breakMinutes')}
        value={breakHalves}
        onChange={(e) => setBreakHalves(e.target.value)}
        sx={{ mb: 2 }}
      />
      <TextField fullWidth label={t('matchRules.warmupBufferMinutes')} value={warmup} onChange={(e) => setWarmup(e.target.value)} sx={{ mb: 2 }} />
      <TextField
        fullWidth
        label={t('matchRules.slotGranularityMinutes')}
        value={slotGran}
        onChange={(e) => setSlotGran(e.target.value)}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        label={t('matchRules.firstMatchToleranceMinutes')}
        value={firstTol}
        onChange={(e) => setFirstTol(e.target.value)}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        label={t('divisionRules.breakBetween')}
        value={breakBetween}
        onChange={(e) => setBreakBetween(e.target.value)}
        sx={{ mb: 2 }}
      />
      <Typography variant="subtitle2" sx={{ mb: 0.5, fontWeight: 600 }}>
        {t('divisionRules.timeRanges')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {t('divisionRules.timeRangesHelp')}
      </Typography>
      <Box sx={{ mb: 2 }}>
        {kickoffRanges.map((row) => (
          <Box
            key={row.id}
            sx={{
              display: 'flex',
              flexWrap: 'wrap',
              gap: 1,
              alignItems: 'flex-start',
              mb: 1.5,
            }}
          >
            <TextField
              label={t('divisionRules.rangeStart')}
              type="time"
              value={row.start}
              onChange={(e) => {
                const v = e.target.value
                setKickoffRanges((prev) =>
                  prev.map((r) => (r.id === row.id ? { ...r, start: v } : r)),
                )
              }}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 140 }}
            />
            <TextField
              label={t('divisionRules.rangeEnd')}
              type="time"
              value={row.end}
              onChange={(e) => {
                const v = e.target.value
                setKickoffRanges((prev) =>
                  prev.map((r) => (r.id === row.id ? { ...r, end: v } : r)),
                )
              }}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 140 }}
            />
            <IconButton
              aria-label={t('divisionRules.removeRange')}
              onClick={() =>
                setKickoffRanges((prev) => prev.filter((r) => r.id !== row.id))
              }
              sx={{ mt: 0.5 }}
              color="error"
              edge="end"
            >
              <DeleteOutlineIcon />
            </IconButton>
          </Box>
        ))}
        <Button
          size="small"
          variant="outlined"
          onClick={() => setKickoffRanges((prev) => [...prev, newKickoffRow()])}
        >
          {t('divisionRules.addKickoffRange')}
        </Button>
      </Box>
      {hasInvalidFilledRange(kickoffRanges) && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {t('divisionRules.invalidRangeHint')}
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 3 }}>
        <Button
          variant="contained"
          onClick={handleSaveRules}
          disabled={putMutation.isPending || hasInvalidFilledRange(kickoffRanges)}
        >
          {putMutation.isPending ? <CircularProgress size={22} color="inherit" /> : t('divisionRules.saveRules')}
        </Button>
        <Button variant="outlined" color="warning" onClick={handleClearDivisionRow} disabled={putMutation.isPending}>
          {t('divisionRules.clearRules')}
        </Button>
      </Box>

      <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
        {t('divisionRules.explicitFields')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {t('divisionRules.explicitFieldsInfo')}
      </Typography>

      <FormControl fullWidth sx={{ mb: 2 }}>
        <InputLabel id="explicit-fields-label">Fields</InputLabel>
        <Select
          labelId="explicit-fields-label"
          multiple
          value={explicitIds}
          onChange={onExplicitChange}
          input={<OutlinedInput label="Fields" />}
          renderValue={(selected) => (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {selected.map((id) => (
                <Chip key={id} size="small" label={fieldNameById.get(id) ?? id} />
              ))}
            </Box>
          )}
        >
          {(fieldsList ?? []).map((f) => (
            <MenuItem key={f.id} value={f.id}>
              <Checkbox checked={explicitIds.includes(f.id)} />
              <ListItemText primary={f.name} secondary={f.address} />
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
        <Button variant="contained" onClick={handleSaveFieldsOnly} disabled={putMutation.isPending}>
          {t('divisionRules.saveFields')}
        </Button>
        <Button variant="outlined" onClick={handleClearExplicitFields} disabled={putMutation.isPending}>
          {t('divisionRules.clearFields')}
        </Button>
        <Button component={RouterLink} to={leagueId ? `/leagues/${leagueId}/match-rules` : '/'} variant="outlined">
          {t('divisionRules.leagueRules')}
        </Button>
        <Button component={RouterLink} to={leagueId ? `/leagues/${leagueId}/season-setup` : '/'} variant="outlined">
          {t('divisionRules.seasonSetup')}
        </Button>
      </Box>
    </Box>
  )
}
