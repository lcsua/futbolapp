import { useState, useEffect, useCallback, useRef, type RefObject, type FormEvent } from 'react'
import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  FormControlLabel,
  Checkbox,
  Tooltip,
  InputAdornment,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material'
import type { LeagueFormData, LeagueFormFiles } from '../api/types'
import { leaguesService } from '../api/leagues'

const ACCEPT_IMAGES = 'image/jpeg,image/png,image/gif,image/webp'
const MAX_FILE_BYTES = 5 * 1024 * 1024
const DEFAULT_PRIMARY = '#16A34A'

const FONT_OPTIONS = [
  { value: '', label: 'Predeterminada (Inter)' },
  { value: 'barlow', label: 'Deportiva (Barlow)' },
  { value: 'nunito', label: 'Amigable (Nunito)' },
  { value: 'rubik', label: 'Moderna (Rubik)' },
]

export interface LeagueFormProps {
  initialValues?: Partial<LeagueFormData>
  onSubmit: (data: LeagueFormData, files?: LeagueFormFiles) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
  publicBaseUrl?: string
  /** When editing, pass leagueId to exclude it from slug availability check */
  excludeLeagueId?: string
}

const defaultValues: LeagueFormData = {
  name: '',
  country: '',
  description: '',
  logoUrl: '',
  isPublic: false,
  isActive: true,
  primaryColor: '',
  fontKey: '',
  heroImageUrl: '',
  teamHeroImageUrl: '',
}

export function LeagueForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
  publicBaseUrl = 'https://miliga.com.ar',
  excludeLeagueId,
}: LeagueFormProps) {
  const values: LeagueFormData = { ...defaultValues, ...initialValues }

  const [name, setName] = useState(values.name)
  const [slug, setSlug] = useState(values.slug ?? leaguesService.generateSlug(values.name))
  const [slugManuallyEdited, setSlugManuallyEdited] = useState(!!values.slug)
  const [slugAvailable, setSlugAvailable] = useState<boolean | null>(null)
  const [slugChecking, setSlugChecking] = useState(false)
  const [country, setCountry] = useState(values.country)
  const [description, setDescription] = useState(values.description ?? '')
  const [logoUrl, setLogoUrl] = useState(values.logoUrl ?? '')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(values.logoUrl?.trim() || null)
  const [logoRemoved, setLogoRemoved] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  const logoInputRef = useRef<HTMLInputElement>(null)
  const [isPublic, setIsPublic] = useState(values.isPublic ?? false)
  const [isActive, setIsActive] = useState(values.isActive ?? true)
  const [primaryColor, setPrimaryColor] = useState(values.primaryColor ?? '')
  const [fontKey, setFontKey] = useState(values.fontKey ?? '')
  const [heroImageUrl, setHeroImageUrl] = useState(values.heroImageUrl ?? '')
  const [heroFile, setHeroFile] = useState<File | null>(null)
  const [heroPreview, setHeroPreview] = useState<string | null>(values.heroImageUrl?.trim() || null)
  const [heroRemoved, setHeroRemoved] = useState(false)
  const heroInputRef = useRef<HTMLInputElement>(null)
  const [teamHeroImageUrl, setTeamHeroImageUrl] = useState(values.teamHeroImageUrl ?? '')
  const [teamHeroFile, setTeamHeroFile] = useState<File | null>(null)
  const [teamHeroPreview, setTeamHeroPreview] = useState<string | null>(values.teamHeroImageUrl?.trim() || null)
  const [teamHeroRemoved, setTeamHeroRemoved] = useState(false)
  const teamHeroInputRef = useRef<HTMLInputElement>(null)

  const checkSlug = useCallback(async (s: string) => {
    if (!s.trim()) {
      setSlugAvailable(null)
      return
    }
    setSlugChecking(true)
    try {
      const res = await leaguesService.checkSlugAvailability(s, excludeLeagueId)
      setSlugAvailable(res.available)
    } catch {
      setSlugAvailable(null)
    } finally {
      setSlugChecking(false)
    }
  }, [excludeLeagueId])

  useEffect(() => {
    if (initialValues?.name !== undefined) {
      setName(initialValues.name)
      setSlug(initialValues.slug ?? leaguesService.generateSlug(initialValues.name))
      setSlugManuallyEdited(!!initialValues.slug)
      setCountry(initialValues.country ?? '')
      setDescription(initialValues.description ?? '')
      setLogoUrl(initialValues.logoUrl ?? '')
      setLogoFile(null)
      setLogoPreview(initialValues.logoUrl?.trim() || null)
      setLogoRemoved(false)
      setFileError(null)
      setIsPublic(initialValues.isPublic ?? false)
      setIsActive(initialValues.isActive ?? true)
      setPrimaryColor(initialValues.primaryColor ?? '')
      setFontKey(initialValues.fontKey ?? '')
      setHeroImageUrl(initialValues.heroImageUrl ?? '')
      setHeroFile(null)
      setHeroPreview(initialValues.heroImageUrl?.trim() || null)
      setHeroRemoved(false)
      setTeamHeroImageUrl(initialValues.teamHeroImageUrl ?? '')
      setTeamHeroFile(null)
      setTeamHeroPreview(initialValues.teamHeroImageUrl?.trim() || null)
      setTeamHeroRemoved(false)
    }
  }, [
    initialValues?.name,
    initialValues?.slug,
    initialValues?.country,
    initialValues?.description,
    initialValues?.logoUrl,
    initialValues?.isPublic,
    initialValues?.isActive,
    initialValues?.primaryColor,
    initialValues?.fontKey,
    initialValues?.heroImageUrl,
    initialValues?.teamHeroImageUrl,
  ])

  useEffect(() => {
    if (!slugManuallyEdited && name) {
      setSlug(leaguesService.generateSlug(name))
    }
  }, [name, slugManuallyEdited])

  useEffect(() => {
    const timer = setTimeout(() => {
      if (slug.trim()) checkSlug(slug)
      else setSlugAvailable(null)
    }, 400)
    return () => clearTimeout(timer)
  }, [slug, checkSlug])

  const pickImage = useCallback((
    file: File | undefined,
    setFile: (file: File | null) => void,
    setPreview: (url: string | null) => void,
    setRemoved: (removed: boolean) => void,
    inputRef: RefObject<HTMLInputElement | null>,
    label: string,
  ) => {
    setFileError(null)
    if (!file) return
    if (!file.type.startsWith('image/')) {
      setFileError(`${label} debe ser una imagen (JPEG, PNG, GIF o WebP).`)
      if (inputRef.current) inputRef.current.value = ''
      return
    }
    if (file.size > MAX_FILE_BYTES) {
      setFileError(`${label} no puede superar 5 MB.`)
      if (inputRef.current) inputRef.current.value = ''
      return
    }
    setFile(file)
    setRemoved(false)
    const reader = new FileReader()
    reader.onload = () => setPreview(reader.result as string)
    reader.readAsDataURL(file)
  }, [])

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    if (fileError) return
    const finalSlug = slug.trim() || leaguesService.generateSlug(name)
    const data: LeagueFormData = {
      name: name.trim(),
      country: country.trim(),
      slug: finalSlug,
      description: description.trim(),
      logoUrl: logoRemoved ? '' : logoUrl.trim(),
      isPublic,
      isActive,
      primaryColor: primaryColor.trim(),
      fontKey: fontKey.trim(),
      heroImageUrl: heroRemoved ? '' : heroImageUrl.trim(),
      teamHeroImageUrl: teamHeroRemoved ? '' : teamHeroImageUrl.trim(),
    }
    void onSubmit(data, {
      logoFile: logoRemoved ? null : logoFile,
      heroFile: heroRemoved ? null : heroFile,
      teamHeroFile: teamHeroRemoved ? null : teamHeroFile,
    })
  }

  const slugError = slugAvailable === false
  const publicUrl = slug.trim()
    ? `${publicBaseUrl.replace(/\/$/, '')}/ligas/${slug.trim()}`
    : ''

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 560 }}>
      {title && (
        <Typography variant="h6" component="h2" sx={{ mb: 2, fontWeight: 600 }}>
          {title}
        </Typography>
      )}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => {}}>
          {error}
        </Alert>
      )}
      <TextField
        fullWidth
        name="name"
        label="League Name"
        required
        value={name}
        onChange={(e) => setName(e.target.value)}
        disabled={loading}
        sx={{ mb: 2 }}
        autoFocus
      />
      <TextField
        fullWidth
        name="slug"
        label="Slug"
        required
        value={slug}
        onChange={(e) => {
          setSlug(e.target.value)
          setSlugManuallyEdited(true)
        }}
        onBlur={() => checkSlug(slug)}
        disabled={loading}
        error={slugError}
        helperText={
          slugChecking
            ? 'Checking...'
            : slugError
              ? 'Slug already in use, please choose another one'
              : slugAvailable === true
                ? 'Available'
                : 'URL-friendly identifier. Auto-generated from name.'
        }
        InputProps={{
          endAdornment: slugChecking ? (
            <InputAdornment position="end">
              <CircularProgress size={20} />
            </InputAdornment>
          ) : null,
        }}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="country"
        label="Country"
        required
        value={country}
        onChange={(e) => setCountry(e.target.value)}
        disabled={loading}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="description"
        label="Description"
        multiline
        rows={2}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        disabled={loading}
        sx={{ mb: 2 }}
      />
      <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
        Logo de la liga
      </Typography>
      <input
        ref={logoInputRef}
        type="file"
        accept={ACCEPT_IMAGES}
        onChange={(e) => pickImage(e.target.files?.[0], setLogoFile, setLogoPreview, setLogoRemoved, logoInputRef, 'El logo')}
        disabled={loading}
        style={{ display: 'block', marginBottom: 8 }}
        aria-label="Subir logo de la liga"
      />
      {fileError && (
        <Alert severity="error" sx={{ mb: 1 }} onClose={() => setFileError(null)}>
          {fileError}
        </Alert>
      )}
      {logoPreview && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1, flexWrap: 'wrap' }}>
          <Box
            component="img"
            src={logoPreview}
            alt="Vista previa del logo"
            sx={{ width: 64, height: 64, objectFit: 'contain', border: '1px solid', borderColor: 'divider', borderRadius: 1, bgcolor: 'background.paper' }}
          />
          <Button
            size="small"
            onClick={() => {
              setLogoFile(null)
              setLogoUrl('')
              setLogoPreview(null)
              setLogoRemoved(true)
              if (logoInputRef.current) logoInputRef.current.value = ''
            }}
            disabled={loading}
          >
            Quitar logo
          </Button>
        </Box>
      )}
      <TextField
        fullWidth
        name="logoUrl"
        label="Logo URL (opcional)"
        value={logoUrl}
        onChange={(e) => {
          setLogoUrl(e.target.value)
          setLogoFile(null)
          setLogoRemoved(false)
          setLogoPreview(e.target.value.trim() || null)
        }}
        disabled={loading}
        helperText="Podés subir un archivo o pegar una URL. Si subís un archivo, reemplaza la URL."
        sx={{ mb: 3 }}
      />

      <Typography variant="h6" component="h3" sx={{ mb: 1, fontWeight: 600 }}>
        Apariencia del sitio público
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Si no cambiás nada, la liga se ve como ahora: verde MiLiga, Inter y las fotos de cabecera del producto.
      </Typography>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Color principal
          </Typography>
          <Box
            component="input"
            type="color"
            value={primaryColor.trim() || DEFAULT_PRIMARY}
            onChange={(e) => setPrimaryColor(e.target.value)}
            disabled={loading}
            aria-label="Color principal de la liga"
            sx={{ width: 56, height: 40, p: 0.25, cursor: 'pointer', bgcolor: 'transparent', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}
          />
        </Box>
        <Button
          size="small"
          disabled={loading || !primaryColor.trim()}
          onClick={() => setPrimaryColor('')}
        >
          Usar verde MiLiga
        </Button>
      </Box>

      <FormControl fullWidth sx={{ mb: 3 }}>
        <InputLabel id="league-font-label">Fuente</InputLabel>
        <Select
          labelId="league-font-label"
          label="Fuente"
          value={fontKey}
          onChange={(e) => setFontKey(String(e.target.value))}
          disabled={loading}
        >
          {FONT_OPTIONS.map((opt) => (
            <MenuItem key={opt.value || 'inter'} value={opt.value}>
              {opt.label}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
        Cabecera de la liga
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
        Portada, fixture, posiciones y resultados. Vacío = foto actual de MiLiga.
      </Typography>
      <input
        ref={heroInputRef}
        type="file"
        accept={ACCEPT_IMAGES}
        onChange={(e) => {
          pickImage(e.target.files?.[0], setHeroFile, setHeroPreview, setHeroRemoved, heroInputRef, 'La cabecera de la liga')
          setHeroImageUrl('')
        }}
        disabled={loading}
        style={{ display: 'block', marginBottom: 8 }}
        aria-label="Subir cabecera de la liga"
      />
      {heroPreview && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, flexWrap: 'wrap' }}>
          <Box
            component="img"
            src={heroPreview}
            alt="Vista previa de la cabecera de la liga"
            sx={{ width: 160, height: 64, objectFit: 'cover', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}
          />
          <Button
            size="small"
            onClick={() => {
              setHeroFile(null)
              setHeroImageUrl('')
              setHeroPreview(null)
              setHeroRemoved(true)
              if (heroInputRef.current) heroInputRef.current.value = ''
            }}
            disabled={loading}
          >
            Usar foto de MiLiga
          </Button>
        </Box>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
        Cabecera del equipo
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
        Detalle de un equipo. Vacío = foto actual de MiLiga.
      </Typography>
      <input
        ref={teamHeroInputRef}
        type="file"
        accept={ACCEPT_IMAGES}
        onChange={(e) => {
          pickImage(e.target.files?.[0], setTeamHeroFile, setTeamHeroPreview, setTeamHeroRemoved, teamHeroInputRef, 'La cabecera del equipo')
          setTeamHeroImageUrl('')
        }}
        disabled={loading}
        style={{ display: 'block', marginBottom: 8 }}
        aria-label="Subir cabecera del equipo"
      />
      {teamHeroPreview && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3, flexWrap: 'wrap' }}>
          <Box
            component="img"
            src={teamHeroPreview}
            alt="Vista previa de la cabecera del equipo"
            sx={{ width: 160, height: 64, objectFit: 'cover', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}
          />
          <Button
            size="small"
            onClick={() => {
              setTeamHeroFile(null)
              setTeamHeroImageUrl('')
              setTeamHeroPreview(null)
              setTeamHeroRemoved(true)
              if (teamHeroInputRef.current) teamHeroInputRef.current.value = ''
            }}
            disabled={loading}
          >
            Usar foto de MiLiga
          </Button>
        </Box>
      )}

      <Tooltip title="Inactive leagues may be hidden from lists and selection">
        <FormControlLabel
          control={
            <Checkbox
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              disabled={loading}
            />
          }
          label="Active"
          sx={{ mb: 2, display: 'block' }}
        />
      </Tooltip>
      <Tooltip title="If disabled, this league will not be accessible from the public website">
        <FormControlLabel
          control={
            <Checkbox
              checked={isPublic}
              onChange={(e) => setIsPublic(e.target.checked)}
              disabled={loading}
            />
          }
          label="Make this league public"
          sx={{ mb: 2, display: 'block' }}
        />
      </Tooltip>
      {publicUrl && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Public URL: {publicUrl}
        </Typography>
      )}
      <Button
        type="submit"
        variant="contained"
        disabled={loading || slugError || !!fileError}
        sx={{ minWidth: 120 }}
      >
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
