/** Matches backend LogoThumbnailService: abc.png → abc.thumb.webp */
export function deriveLogoThumbUrl(logoUrl?: string | null): string | null {
  if (!logoUrl?.trim()) return null
  const url = logoUrl.trim()
  if (url.startsWith('data:')) return null
  if (!url.toLowerCase().includes('/uploads/')) return null
  if (url.toLowerCase().includes('.thumb.webp')) return url

  const match = url.match(/^(.*\/)([^/?#]+)(\?.*)?(#.*)?$/)
  if (!match) return null
  const [, dir, file, query = '', hash = ''] = match
  const dot = file.lastIndexOf('.')
  const name = dot >= 0 ? file.slice(0, dot) : file
  return `${dir}${name}.thumb.webp${query}${hash}`
}

export function effectiveTeamLogoUrl(team: {
  logoUrl?: string | null
  clubLogoUrl?: string | null
}) {
  const own = team.logoUrl?.trim()
  if (own) return own
  const club = team.clubLogoUrl?.trim()
  return club || null
}
