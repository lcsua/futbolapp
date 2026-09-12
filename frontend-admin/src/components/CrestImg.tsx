import { Box, type BoxProps } from '@mui/material'
import { deriveLogoThumbUrl } from '../utils/teamLogo'

type CrestImgProps = {
  src: string
  alt?: string
  size?: number
} & Omit<BoxProps, 'component' | 'src' | 'alt'>

export function CrestImg({ src, alt = '', size = 40, sx, onError, ...rest }: CrestImgProps) {
  const thumb = deriveLogoThumbUrl(src)
  return (
    <Box
      component="img"
      src={thumb ?? src}
      alt={alt}
      onError={(e) => {
        const img = e.currentTarget
        if (thumb && img.getAttribute('src') !== src) {
          img.setAttribute('src', src)
        }
        onError?.(e)
      }}
      sx={{
        width: size,
        height: size,
        objectFit: 'contain',
        flexShrink: 0,
        ...sx,
      }}
      {...rest}
    />
  )
}
