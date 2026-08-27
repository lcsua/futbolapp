import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Snackbar,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import CampaignIcon from '@mui/icons-material/Campaign'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useActiveLeague, useLeagueId } from '../contexts/LeagueContext'
import {
  advertisementsService,
  toAdvertisementWriteBody,
  type Advertisement,
} from '../api/advertisements'
import { AdvertisementFormDialog } from '../components/AdvertisementFormDialog'
import {
  advertisementSlotLabelKey,
  formatAdvertisementPeriod,
  getAdvertisementVisualStatus,
  type AdvertisementVisualStatus,
} from '../utils/advertisementStatus'

const STATUS_CHIP_COLOR: Record<AdvertisementVisualStatus, 'default' | 'success' | 'info' | 'warning'> = {
  inactive: 'default',
  active: 'success',
  scheduled: 'info',
  expired: 'warning',
}

export function LeagueAdsPage() {
  const { t, i18n } = useTranslation()
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<Advertisement | null>(null)
  const [deleting, setDeleting] = useState<Advertisement | null>(null)
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const queryKey = ['leagues', leagueId, 'advertisements'] as const

  const { data: ads = [], isLoading, isError, error } = useQuery({
    queryKey,
    queryFn: ({ signal }) => advertisementsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const deleteMutation = useMutation({
    mutationFn: (ad: Advertisement) => advertisementsService.remove(leagueId!, ad.id),
    onSuccess: async (_void, ad) => {
      queryClient.setQueryData<Advertisement[]>(queryKey, (current) =>
        (current ?? []).filter((item) => item.id !== ad.id),
      )
      await queryClient.invalidateQueries({ queryKey })
      setDeleting(null)
      setSnackbar({ message: t('ads.deleted'), severity: 'success' })
    },
    onError: (err) => {
      setSnackbar({
        message: err instanceof Error ? err.message : t('ads.deleteFailed'),
        severity: 'error',
      })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: (ad: Advertisement) =>
      advertisementsService.update(
        leagueId!,
        ad.id,
        toAdvertisementWriteBody(ad, { isActive: !ad.isActive }),
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey })
      setSnackbar({ message: t('ads.toggled'), severity: 'success' })
    },
    onError: (err) => {
      setSnackbar({
        message: err instanceof Error ? err.message : t('common.saveError'),
        severity: 'error',
      })
    },
  })

  const openCreate = () => {
    setEditing(null)
    setDialogOpen(true)
  }

  const openEdit = (ad: Advertisement) => {
    setEditing(ad)
    setDialogOpen(true)
  }

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>{t('ads.goToLeagues')}</Button>}>
        {t('ads.noLeague')}
      </Alert>
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return <Alert severity="error">{error instanceof Error ? error.message : t('ads.loadError')}</Alert>
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          {t('ads.title', { league: activeLeague?.name ?? t('nav.leagues') })}
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          {t('ads.create')}
        </Button>
      </Box>

      {ads.length === 0 ? (
        <Typography color="text.secondary">{t('ads.empty')}</Typography>
      ) : (
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' } }}>
          {ads.map((ad) => {
            const status = getAdvertisementVisualStatus(ad)
            const previewUrl = ad.desktopImageUrl || ad.mobileImageUrl
            const toggling = toggleMutation.isPending && toggleMutation.variables?.id === ad.id
            return (
              <Card key={ad.id} variant="outlined">
                <CardContent>
                  <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-start' }}>
                    <Box
                      sx={{
                        width: 88,
                        height: 64,
                        flexShrink: 0,
                        borderRadius: 1,
                        border: 1,
                        borderColor: 'divider',
                        bgcolor: 'action.hover',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        overflow: 'hidden',
                      }}
                    >
                      {previewUrl ? (
                        <Box
                          component="img"
                          src={previewUrl}
                          alt={ad.name}
                          sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
                        />
                      ) : (
                        <CampaignIcon color="disabled" />
                      )}
                    </Box>
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Typography variant="h6" sx={{ fontSize: '1.05rem', lineHeight: 1.3 }}>
                        {ad.name}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {ad.advertiserName}
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap', mt: 1 }}>
                        <Chip size="small" label={t(advertisementSlotLabelKey(ad.slot))} />
                        <Chip
                          size="small"
                          color={STATUS_CHIP_COLOR[status]}
                          label={t(`ads.status.${status}`)}
                        />
                      </Box>
                    </Box>
                  </Box>

                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
                    {t('ads.fields.period')}:{' '}
                    {formatAdvertisementPeriod(ad.startsAt, ad.endsAt, {
                      none: t('ads.noPeriod'),
                      from: t('ads.from'),
                      until: t('ads.until'),
                    }, i18n.language)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t('ads.fields.priority')}: {ad.priority}
                  </Typography>

                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 2 }}>
                    <Button size="small" startIcon={<EditIcon />} onClick={() => openEdit(ad)}>
                      {t('ads.edit')}
                    </Button>
                    <Button
                      size="small"
                      onClick={() => toggleMutation.mutate(ad)}
                      disabled={toggling}
                    >
                      {toggling ? (
                        <CircularProgress size={16} />
                      ) : ad.isActive ? (
                        t('ads.deactivate')
                      ) : (
                        t('ads.activate')
                      )}
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<DeleteOutlineIcon />}
                      onClick={() => setDeleting(ad)}
                    >
                      {t('ads.delete')}
                    </Button>
                  </Box>
                </CardContent>
              </Card>
            )
          })}
        </Box>
      )}

      <AdvertisementFormDialog
        open={dialogOpen}
        leagueId={leagueId}
        advertisement={editing}
        onClose={() => {
          setDialogOpen(false)
          setEditing(null)
        }}
        onFeedback={(message, severity) => setSnackbar({ message, severity })}
      />

      <Dialog
        open={!!deleting}
        onClose={() => !deleteMutation.isPending && setDeleting(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>{t('ads.deleteConfirmTitle')}</DialogTitle>
        <DialogContent>
          <DialogContentText>
            {t('ads.deleteConfirm', { name: deleting?.name ?? '' })}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleting(null)} disabled={deleteMutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() => deleting && deleteMutation.mutate(deleting)}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? <CircularProgress size={22} color="inherit" /> : t('ads.delete')}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={!!snackbar}
        autoHideDuration={5000}
        onClose={() => setSnackbar(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {snackbar ? (
          <Alert onClose={() => setSnackbar(null)} severity={snackbar.severity} variant="filled" sx={{ width: '100%' }}>
            {snackbar.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Box>
  )
}
