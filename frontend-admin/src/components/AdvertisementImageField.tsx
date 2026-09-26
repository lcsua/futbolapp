import { useRef } from 'react'
import { Box, Button, CircularProgress, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { AD_IMAGE_ACCEPT } from '../api/advertisements'

type AdvertisementImageFieldProps = {
  title: string
  imageUrl: string | null
  disabled?: boolean
  busy?: boolean
  onSelectFile: (file: File) => void
  onDelete: () => void
}

export function AdvertisementImageField({
  title,
  imageUrl,
  disabled = false,
  busy = false,
  onSelectFile,
  onDelete,
}: AdvertisementImageFieldProps) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement>(null)
  const locked = disabled || busy

  return (
    <Box
      sx={{
        border: 1,
        borderColor: 'divider',
        borderRadius: 1,
        p: 1.5,
        display: 'flex',
        flexDirection: 'column',
        gap: 1,
        minHeight: 180,
      }}
    >
      <Typography variant="subtitle2">{title}</Typography>
      <Typography variant="caption" color="text.secondary">
        {t('ads.images.restrictions')}
      </Typography>

      <Box
        sx={{
          position: 'relative',
          width: '100%',
          height: 120,
          borderRadius: 1,
          bgcolor: 'action.hover',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
        }}
      >
        {imageUrl ? (
          <Box
            component="img"
            src={imageUrl}
            alt={title}
            sx={{ width: '100%', height: '100%', objectFit: 'contain' }}
          />
        ) : (
          <Typography variant="caption" color="text.secondary">
            {t('ads.images.empty')}
          </Typography>
        )}
        {busy ? (
          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255,255,255,0.65)',
            }}
          >
            <CircularProgress size={28} />
          </Box>
        ) : null}
      </Box>

      <input
        ref={inputRef}
        type="file"
        accept={AD_IMAGE_ACCEPT}
        hidden
        disabled={locked}
        onChange={(e) => {
          const file = e.target.files?.[0]
          e.target.value = ''
          if (file) onSelectFile(file)
        }}
      />

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        <Button size="small" variant="outlined" disabled={locked} onClick={() => inputRef.current?.click()}>
          {imageUrl ? t('ads.images.replace') : t('ads.images.upload')}
        </Button>
        {imageUrl ? (
          <Button size="small" color="error" disabled={locked} onClick={onDelete}>
            {t('ads.images.remove')}
          </Button>
        ) : null}
      </Box>
    </Box>
  )
}
