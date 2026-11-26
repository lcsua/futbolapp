import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  Radio,
  RadioGroup,
  TextField,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  championsService,
  type ChampionMode,
  type ChampionOptions,
  type ChampionTeamOption,
  type ChampionWriteBody,
  type LeagueChampion,
} from '../api/champions'
import { ChampionPhotoGallery } from './ChampionPhotoGallery'

type ChampionFormDialogProps = {
  open: boolean
  leagueId: string
  championId: string | null
  onClose: () => void
  onSaved: (message: string) => void
}

type FormState = {
  mode: ChampionMode
  teamId: string
  seasonId: string
  divisionId: string
  year: string
  championshipName: string
  divisionName: string
  linkSeasonId: string
  linkDivisionId: string
  wonAt: string
  notes: string
}

const emptyForm = (): FormState => ({
  mode: 'historical',
  teamId: '',
  seasonId: '',
  divisionId: '',
  year: '',
  championshipName: '',
  divisionName: '',
  linkSeasonId: '',
  linkDivisionId: '',
  wonAt: '',
  notes: '',
})

export function ChampionFormDialog({ open, leagueId, championId, onClose, onSaved }: ChampionFormDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [activeId, setActiveId] = useState<string | null>(championId)
  const [formError, setFormError] = useState<string | null>(null)
  const [loadedKey, setLoadedKey] = useState<string | null>(null)

  const optionsQuery = useQuery({
    queryKey: ['leagues', leagueId, 'champion-options'],
    queryFn: ({ signal }) => championsService.options(leagueId, signal),
    enabled: open,
  })

  const detailQuery = useQuery({
    queryKey: ['leagues', leagueId, 'champions', activeId],
    queryFn: ({ signal }) => championsService.get(leagueId, activeId!, signal),
    enabled: open && !!activeId,
  })

  useEffect(() => {
    if (!open) {
      setActiveId(championId)
      setLoadedKey(null)
      setFormError(null)
      return
    }
    setActiveId(championId)
  }, [open, championId])

  useEffect(() => {
    if (!open || !optionsQuery.data) return
    const key = activeId ?? 'new'
    if (loadedKey === key) return
    if (activeId && !detailQuery.data) return

    if (!activeId) {
      setForm(emptyForm())
      setLoadedKey(key)
      return
    }

    setForm(formFromChampion(detailQuery.data!, optionsQuery.data))
    setLoadedKey(key)
  }, [open, activeId, detailQuery.data, optionsQuery.data, loadedKey])

  const options = optionsQuery.data
  const season = options?.seasons.find((item) => item.id === form.seasonId) ?? null
  const roster = options?.participants.find(
    (item) => item.seasonId === form.seasonId && item.divisionId === form.divisionId,
  )
  const existingDivisions = useMemo(() => {
    if (!options || !form.seasonId) return []
    const ids = new Set(
      options.participants.filter((item) => item.seasonId === form.seasonId).map((item) => item.divisionId),
    )
    return options.divisions.filter((division) => ids.has(division.id))
  }, [options, form.seasonId])
  const linkDivisions = useMemo(() => {
    if (!options || !form.linkSeasonId) return []
    const ids = new Set(
      options.participants.filter((item) => item.seasonId === form.linkSeasonId).map((item) => item.divisionId),
    )
    return options.divisions.filter((division) => ids.has(division.id))
  }, [options, form.linkSeasonId])

  const teamChoices = useMemo(() => {
    if (!options) return []
    if (form.mode === 'historical') return options.teams
    const ids = new Set(roster?.teamIds ?? [])
    return options.teams.filter((team) => ids.has(team.id) || team.id === form.teamId)
  }, [options, form.mode, form.teamId, roster])

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'champions'] })
  }

  const saveMutation = useMutation({
    mutationFn: (body: ChampionWriteBody) =>
      activeId ? championsService.update(leagueId, activeId, body) : championsService.create(leagueId, body),
    onSuccess: async (saved) => {
      queryClient.setQueryData(['leagues', leagueId, 'champions', saved.id], saved)
      setActiveId(saved.id)
      setLoadedKey(saved.id)
      if (options) setForm(formFromChampion(saved, options))
      await invalidate()
      onSaved(activeId ? t('champions.updated') : t('champions.created'))
      setFormError(null)
    },
  })

  const submit = () => {
    const error = validateForm(form, t)
    if (error) {
      setFormError(error)
      return
    }
    setFormError(null)
    saveMutation.mutate(toWriteBody(form, options))
  }

  const busy = saveMutation.isPending
  const loading = optionsQuery.isLoading || (!!activeId && detailQuery.isLoading && !detailQuery.data)

  return (
    <Dialog open={open} onClose={() => !busy && onClose()} fullWidth maxWidth="md">
      <DialogTitle>{activeId ? t('champions.editTitle') : t('champions.createTitle')}</DialogTitle>
      <DialogContent>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : optionsQuery.isError ? (
          <Alert severity="error">
            {optionsQuery.error instanceof Error ? optionsQuery.error.message : t('champions.loadError')}
          </Alert>
        ) : (
          <Box sx={{ display: 'grid', gap: 2, pt: 1 }}>
            {formError ? <Alert severity="error">{formError}</Alert> : null}
            {saveMutation.isError ? (
              <Alert severity="error">
                {saveMutation.error instanceof Error ? saveMutation.error.message : t('common.saveError')}
              </Alert>
            ) : null}

            <FormControl>
              <RadioGroup
                row
                value={form.mode}
                onChange={(event) => setForm((current) => ({ ...current, mode: event.target.value as ChampionMode, teamId: '' }))}
              >
                <FormControlLabel value="existing" control={<Radio />} label={t('champions.modes.existing')} disabled={busy} />
                <FormControlLabel value="historical" control={<Radio />} label={t('champions.modes.historical')} disabled={busy} />
              </RadioGroup>
            </FormControl>

            {form.mode === 'existing' ? (
              <>
                <Alert severity="info">{t('champions.existingHelp')}</Alert>
                <Autocomplete
                  options={options?.seasons ?? []}
                  value={season}
                  onChange={(_, value) =>
                    setForm((current) => ({
                      ...current,
                      seasonId: value?.id ?? '',
                      divisionId: '',
                      teamId: '',
                    }))
                  }
                  getOptionLabel={(option) => `${option.name} (${option.year})`}
                  isOptionEqualToValue={(option, value) => option.id === value.id}
                  renderInput={(params) => <TextField {...params} label={t('champions.fields.championship')} />}
                  disabled={busy}
                />
                <Autocomplete
                  options={existingDivisions}
                  value={existingDivisions.find((division) => division.id === form.divisionId) ?? null}
                  onChange={(_, value) =>
                    setForm((current) => ({ ...current, divisionId: value?.id ?? '', teamId: '' }))
                  }
                  getOptionLabel={(option) => option.name}
                  isOptionEqualToValue={(option, value) => option.id === value.id}
                  renderInput={(params) => <TextField {...params} label={t('champions.fields.division')} />}
                  disabled={busy || !form.seasonId}
                />
                {form.seasonId && form.divisionId && (roster?.teamIds.length ?? 0) === 0 ? (
                  <Alert severity="warning">{t('champions.noParticipants')}</Alert>
                ) : null}
                <TextField label={t('champions.fields.year')} value={season?.year ?? ''} disabled />
                <TextField label={t('champions.fields.championshipName')} value={season?.name ?? ''} disabled />
              </>
            ) : (
              <>
                <Alert severity="info">{t('champions.historicalHelp')}</Alert>
                <TextField
                  label={t('champions.fields.year')}
                  type="number"
                  value={form.year}
                  onChange={(event) => setForm((current) => ({ ...current, year: event.target.value }))}
                  disabled={busy}
                  slotProps={{ htmlInput: { min: 1900, max: 2100 } }}
                />
                <TextField
                  label={t('champions.fields.championshipName')}
                  value={form.championshipName}
                  onChange={(event) => setForm((current) => ({ ...current, championshipName: event.target.value }))}
                  disabled={busy}
                  helperText={t('champions.fields.championshipHelp')}
                />
                <Autocomplete
                  freeSolo
                  options={options?.divisions ?? []}
                  inputValue={form.divisionName}
                  onInputChange={(_, value) => setForm((current) => ({ ...current, divisionName: value }))}
                  getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
                  renderInput={(params) => (
                    <TextField {...params} label={t('champions.fields.division')} helperText={t('champions.fields.divisionHelp')} />
                  )}
                  disabled={busy}
                />
                <TypographyLink help={t('champions.fields.linkHelp')} />
                <Autocomplete
                  options={options?.seasons ?? []}
                  value={options?.seasons.find((item) => item.id === form.linkSeasonId) ?? null}
                  onChange={(_, value) =>
                    setForm((current) => ({ ...current, linkSeasonId: value?.id ?? '', linkDivisionId: '' }))
                  }
                  getOptionLabel={(option) => `${option.name} (${option.year})`}
                  isOptionEqualToValue={(option, value) => option.id === value.id}
                  renderInput={(params) => <TextField {...params} label={t('champions.fields.linkChampionship')} />}
                  disabled={busy}
                />
                <Autocomplete
                  options={linkDivisions}
                  value={linkDivisions.find((division) => division.id === form.linkDivisionId) ?? null}
                  onChange={(_, value) => setForm((current) => ({ ...current, linkDivisionId: value?.id ?? '' }))}
                  getOptionLabel={(option) => option.name}
                  isOptionEqualToValue={(option, value) => option.id === value.id}
                  renderInput={(params) => <TextField {...params} label={t('champions.fields.linkDivision')} />}
                  disabled={busy || !form.linkSeasonId}
                />
              </>
            )}

            <TeamPicker
              teams={teamChoices}
              teamId={form.teamId}
              disabled={busy || (form.mode === 'existing' && (!form.seasonId || !form.divisionId))}
              onChange={(teamId) => setForm((current) => ({ ...current, teamId }))}
            />

            <TextField
              label={t('champions.fields.wonAt')}
              type="date"
              value={form.wonAt}
              onChange={(event) => setForm((current) => ({ ...current, wonAt: event.target.value }))}
              disabled={busy}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label={t('champions.fields.notes')}
              value={form.notes}
              onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
              disabled={busy}
              multiline
              minRows={3}
            />

            {activeId && detailQuery.data ? (
              <ChampionPhotoGallery
                photos={detailQuery.data.photos ?? []}
                disabled={busy}
                onError={setFormError}
                onUpload={async (files) => {
                  try {
                    const saved = await championsService.uploadPhotos(leagueId, activeId, files)
                    queryClient.setQueryData(['leagues', leagueId, 'champions', activeId], saved)
                  } catch (err) {
                    await invalidate()
                    throw err
                  }
                  await invalidate()
                }}
                onDelete={async (photoId) => {
                  await championsService.deletePhoto(leagueId, activeId, photoId)
                  await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'champions', activeId] })
                  await invalidate()
                }}
                onCommit={async (photos) => {
                  const saved = await championsService.updateGallery(
                    leagueId,
                    activeId,
                    photos.map((photo, index) => ({
                      id: photo.id,
                      caption: photo.caption,
                      displayOrder: index,
                      isCover: photo.isCover,
                    })),
                  )
                  queryClient.setQueryData(['leagues', leagueId, 'champions', activeId], saved)
                  await invalidate()
                }}
              />
            ) : (
              <Alert severity="info">{t('champions.photos.afterCreate')}</Alert>
            )}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={submit} disabled={busy || loading || optionsQuery.isError}>
          {busy ? <CircularProgress size={22} color="inherit" /> : t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function TypographyLink({ help }: { help: string }) {
  return <Alert severity="info">{help}</Alert>
}

function TeamPicker({
  teams,
  teamId,
  disabled,
  onChange,
}: {
  teams: ChampionTeamOption[]
  teamId: string
  disabled?: boolean
  onChange: (teamId: string) => void
}) {
  const { t } = useTranslation()
  const selected = teams.find((team) => team.id === teamId) ?? null

  return (
    <Autocomplete
      options={teams}
      value={selected}
      onChange={(_, team) => onChange(team?.id ?? '')}
      getOptionLabel={(team) => team.name}
      isOptionEqualToValue={(option, value) => option.id === value.id}
      disabled={disabled}
      renderOption={(props, team) => {
        const { key, ...rest } = props
        return (
          <li key={key} {...rest}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%' }}>
              <Box component="span" sx={{ flex: 1 }}>{team.name}</Box>
              {team.isInactive ? <Chip size="small" label={t('champions.inactive')} /> : null}
              {team.isArchived ? <Chip size="small" color="warning" label={t('champions.archived')} /> : null}
            </Box>
          </li>
        )
      }}
      renderInput={(params) => <TextField {...params} required label={t('champions.fields.team')} />}
    />
  )
}

function formFromChampion(champion: LeagueChampion, options: ChampionOptions): FormState {
  const season = options.seasons.find((item) => item.id === champion.seasonId)
  const division = options.divisions.find((item) => item.id === champion.divisionId)
  const matchesExisting = !!season && !!division
    && season.year === champion.year
    && season.name.trim().toLowerCase() === champion.championshipName.trim().toLowerCase()
    && division.name.trim().toLowerCase() === champion.divisionName.trim().toLowerCase()

  if (matchesExisting) {
    return {
      ...emptyForm(),
      mode: 'existing',
      teamId: champion.teamId,
      seasonId: champion.seasonId ?? '',
      divisionId: champion.divisionId ?? '',
      wonAt: champion.wonAt ?? '',
      notes: champion.notes ?? '',
    }
  }

  return {
    ...emptyForm(),
    mode: 'historical',
    teamId: champion.teamId,
    year: String(champion.year),
    championshipName: champion.championshipName,
    divisionName: champion.divisionName,
    linkSeasonId: champion.seasonId ?? '',
    linkDivisionId: champion.seasonId ? champion.divisionId ?? '' : '',
    wonAt: champion.wonAt ?? '',
    notes: champion.notes ?? '',
  }
}

function validateForm(form: FormState, t: (key: string) => string) {
  if (!form.teamId) return t('champions.validation.team')
  if (form.mode === 'existing') {
    if (!form.seasonId || !form.divisionId) return t('champions.validation.existing')
    return null
  }

  const year = Number(form.year)
  if (!form.year || !Number.isInteger(year) || year < 1900 || year > 2100) return t('champions.validation.year')
  if (!form.championshipName.trim()) return t('champions.validation.championship')
  if (!form.divisionName.trim()) return t('champions.validation.division')
  if (form.linkSeasonId && !form.linkDivisionId) return t('champions.validation.link')
  return null
}

function toWriteBody(form: FormState, options?: ChampionOptions): ChampionWriteBody {
  if (form.mode === 'existing') {
    return {
      mode: 'existing',
      teamId: form.teamId,
      seasonId: form.seasonId,
      divisionId: form.divisionId,
      year: null,
      championshipName: null,
      divisionName: null,
      wonAt: form.wonAt || null,
      notes: form.notes.trim() || null,
    }
  }

  const matchedDivision = options?.divisions.find(
    (division) => division.name.trim().toLowerCase() === form.divisionName.trim().toLowerCase(),
  )
  const seasonId = form.linkSeasonId || null
  const divisionId = seasonId ? form.linkDivisionId || null : matchedDivision?.id ?? null

  return {
    mode: 'historical',
    teamId: form.teamId,
    seasonId,
    divisionId,
    year: Number(form.year),
    championshipName: form.championshipName.trim(),
    divisionName: form.divisionName.trim(),
    wonAt: form.wonAt || null,
    notes: form.notes.trim() || null,
  }
}
