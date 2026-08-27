import { useEffect, useState } from 'react'
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
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  advertisementsService,
  normalizeAdvertisementSlot,
  validateAdvertisementImage,
  type Advertisement,
  type AdvertisementWriteBody,
} from '../api/advertisements'
import { AdvertisementImageField } from './AdvertisementImageField'
import {
  AD_SLOT_OPTIONS,
  fromDatetimeLocalValue,
  toDatetimeLocalValue,
} from '../utils/advertisementStatus'

type AdvertisementFormDialogProps = {
  open: boolean
  leagueId: string
  advertisement: Advertisement | null
  onClose: () => void
  onFeedback: (message: string, severity: 'success' | 'error') => void
}

type FormState = {
  name: string
  advertiserName: string
  targetUrl: string
  slot: number
  startsAt: string
  endsAt: string
  priority: string
  isActive: boolean
}

const emptyForm: FormState = {
  name: '',
  advertiserName: '',
  targetUrl: '',
  slot: 1,
  startsAt: '',
  endsAt: '',
  priority: '0',
  isActive: true,
}

function formFromAdvertisement(ad: Advertisement | null): FormState {
  if (!ad) return emptyForm
  return {
    name: ad.name,
    advertiserName: ad.advertiserName,
    targetUrl: ad.targetUrl ?? '',
    slot: normalizeAdvertisementSlot(ad.slot),
    startsAt: toDatetimeLocalValue(ad.startsAt),
    endsAt: toDatetimeLocalValue(ad.endsAt),
    priority: String(ad.priority ?? 0),
    isActive: ad.isActive,
  }
}

function buildWriteBody(form: FormState): AdvertisementWriteBody | string {
  const name = form.name.trim()
  const advertiserName = form.advertiserName.trim()
  if (!name) return 'ads.validation.nameRequired'
  if (!advertiserName) return 'ads.validation.advertiserRequired'

  const priority = Number(form.priority)
  if (!Number.isFinite(priority) || priority < 0 || !Number.isInteger(priority)) {
    return 'ads.validation.priorityInvalid'
  }

  const startsAt = fromDatetimeLocalValue(form.startsAt)
  const endsAt = fromDatetimeLocalValue(form.endsAt)
  if (form.startsAt && !startsAt) return 'ads.validation.startsInvalid'
  if (form.endsAt && !endsAt) return 'ads.validation.endsInvalid'
  if (startsAt && endsAt && new Date(endsAt).getTime() < new Date(startsAt).getTime()) {
    return 'ads.validation.periodInvalid'
  }

  const targetUrl = form.targetUrl.trim()
  if (targetUrl && !/^https?:\/\//i.test(targetUrl)) return 'ads.validation.targetUrlInvalid'

  return {
    name,
    advertiserName,
    targetUrl: targetUrl || null,
    slot: normalizeAdvertisementSlot(form.slot),
    startsAt,
    endsAt,
    priority,
    isActive: form.isActive,
  }
}

export function AdvertisementFormDialog({
  open,
  leagueId,
  advertisement,
  onClose,
  onFeedback,
}: AdvertisementFormDialogProps) {
  const { t } = useTranslation()
  const theme = useTheme()
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm'))
  const queryClient = useQueryClient()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [formError, setFormError] = useState<string | null>(null)
  const [workingId, setWorkingId] = useState<string | null>(null)
  const [desktopUrl, setDesktopUrl] = useState<string | null>(null)
  const [mobileUrl, setMobileUrl] = useState<string | null>(null)
  const [imageBusy, setImageBusy] = useState<'desktop' | 'mobile' | null>(null)

  useEffect(() => {
    if (!open) return
    setForm(formFromAdvertisement(advertisement))
    setWorkingId(advertisement?.id ?? null)
    setDesktopUrl(advertisement?.desktopImageUrl ?? null)
    setMobileUrl(advertisement?.mobileImageUrl ?? null)
    setFormError(null)
    setImageBusy(null)
  }, [open, advertisement])

  const invalidateList = () =>
    queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'advertisements'] })

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = buildWriteBody(form)
      if (typeof body === 'string') throw new Error(t(body))
      if (workingId) {
        await advertisementsService.update(leagueId, workingId, body)
        return { id: workingId, created: false }
      }
      const created = await advertisementsService.create(leagueId, body)
      return { id: created.id, created: true }
    },
    onSuccess: async ({ id, created }) => {
      setWorkingId(id)
      setFormError(null)
      await invalidateList()
      onFeedback(created ? t('ads.created') : t('ads.updated'), 'success')
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t('common.saveError'))
    },
  })

  const handleImageSelect = async (kind: 'desktop' | 'mobile', file: File) => {
    if (!workingId) return
    const validationKey = validateAdvertisementImage(file)
    if (validationKey) {
      setFormError(t(validationKey))
      return
    }
    setFormError(null)
    setImageBusy(kind)
    try {
      const updated =
        kind === 'desktop'
          ? await advertisementsService.uploadDesktopImage(leagueId, workingId, file)
          : await advertisementsService.uploadMobileImage(leagueId, workingId, file)
      setDesktopUrl(updated.desktopImageUrl)
      setMobileUrl(updated.mobileImageUrl)
      await invalidateList()
      onFeedback(t('ads.images.uploaded'), 'success')
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('ads.images.uploadFailed'))
    } finally {
      setImageBusy(null)
    }
  }

  const handleImageDelete = async (kind: 'desktop' | 'mobile') => {
    if (!workingId) return
    setFormError(null)
    setImageBusy(kind)
    try {
      if (kind === 'desktop') {
        await advertisementsService.deleteDesktopImage(leagueId, workingId)
        setDesktopUrl(null)
      } else {
        await advertisementsService.deleteMobileImage(leagueId, workingId)
        setMobileUrl(null)
      }
      await invalidateList()
      onFeedback(t('ads.images.removed'), 'success')
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('ads.images.deleteFailed'))
    } finally {
      setImageBusy(null)
    }
  }

  const busy = saveMutation.isPending || imageBusy != null
  const isCreate = !workingId

  const handleClose = () => {
    if (busy) return
    onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="md" fullScreen={fullScreen}>
      <DialogTitle>{isCreate ? t('ads.createTitle') : t('ads.editTitle')}</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        {formError ? <Alert severity="error">{formError}</Alert> : null}

        <TextField
          label={t('ads.fields.name')}
          value={form.name}
          onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
          required
          autoFocus
          disabled={busy}
          sx={{ mt: 1 }}
        />
        <TextField
          label={t('ads.fields.advertiser')}
          value={form.advertiserName}
          onChange={(e) => setForm((prev) => ({ ...prev, advertiserName: e.target.value }))}
          required
          disabled={busy}
        />
        <FormControl fullWidth disabled={busy}>
          <InputLabel id="ad-slot-label">{t('ads.fields.slot')}</InputLabel>
          <Select
            labelId="ad-slot-label"
            label={t('ads.fields.slot')}
            value={form.slot}
            onChange={(e) => setForm((prev) => ({ ...prev, slot: Number(e.target.value) }))}
          >
            {AD_SLOT_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {t(option.labelKey)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          label={t('ads.fields.targetUrl')}
          value={form.targetUrl}
          onChange={(e) => setForm((prev) => ({ ...prev, targetUrl: e.target.value }))}
          disabled={busy}
          helperText={t('ads.fields.targetUrlHelp')}
        />
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
          <TextField
            label={t('ads.fields.startsAt')}
            type="datetime-local"
            value={form.startsAt}
            onChange={(e) => setForm((prev) => ({ ...prev, startsAt: e.target.value }))}
            disabled={busy}
            InputLabelProps={{ shrink: true }}
          />
          <TextField
            label={t('ads.fields.endsAt')}
            type="datetime-local"
            value={form.endsAt}
            onChange={(e) => setForm((prev) => ({ ...prev, endsAt: e.target.value }))}
            disabled={busy}
            InputLabelProps={{ shrink: true }}
          />
        </Box>
        <TextField
          label={t('ads.fields.priority')}
          type="number"
          value={form.priority}
          onChange={(e) => setForm((prev) => ({ ...prev, priority: e.target.value }))}
          disabled={busy}
          inputProps={{ min: 0, step: 1 }}
          helperText={t('ads.fields.priorityHelp')}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={form.isActive}
              onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
              disabled={busy}
            />
          }
          label={t('ads.fields.isActive')}
        />

        <Typography variant="subtitle1" sx={{ mt: 1 }}>
          {t('ads.images.title')}
        </Typography>
        {isCreate ? (
          <Alert severity="info">{t('ads.images.afterCreate')}</Alert>
        ) : (
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
            <AdvertisementImageField
              title={t('ads.images.desktop')}
              imageUrl={desktopUrl}
              disabled={busy && imageBusy !== 'desktop'}
              busy={imageBusy === 'desktop'}
              onSelectFile={(file) => void handleImageSelect('desktop', file)}
              onDelete={() => void handleImageDelete('desktop')}
            />
            <AdvertisementImageField
              title={t('ads.images.mobile')}
              imageUrl={mobileUrl}
              disabled={busy && imageBusy !== 'mobile'}
              busy={imageBusy === 'mobile'}
              onSelectFile={(file) => void handleImageSelect('mobile', file)}
              onDelete={() => void handleImageDelete('mobile')}
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={handleClose} disabled={busy}>
          {t('common.cancel')}
        </Button>
        <Button
          variant="contained"
          onClick={() => saveMutation.mutate()}
          disabled={busy}
          startIcon={saveMutation.isPending ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {isCreate ? t('ads.createAction') : t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
