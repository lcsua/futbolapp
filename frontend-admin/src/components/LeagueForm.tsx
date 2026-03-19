import { useState, useEffect, useCallback } from 'react'
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
} from '@mui/material'
import type { LeagueFormData } from '../api/types'
import { leaguesService } from '../api/leagues'

export interface LeagueFormProps {
  initialValues?: Partial<LeagueFormData>
  onSubmit: (data: LeagueFormData) => void | Promise<void>
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
}

export function LeagueForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
  publicBaseUrl = 'https://ligapp.com',
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
  const [isPublic, setIsPublic] = useState(values.isPublic ?? false)
  const [isActive, setIsActive] = useState(values.isActive ?? true)

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
      setIsPublic(initialValues.isPublic ?? false)
      setIsActive(initialValues.isActive ?? true)
    }
  }, [initialValues?.name, initialValues?.slug, initialValues?.country, initialValues?.description, initialValues?.logoUrl, initialValues?.isPublic, initialValues?.isActive])

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

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const finalSlug = slug.trim() || leaguesService.generateSlug(name)
    const data: LeagueFormData = {
      name: name.trim(),
      country: country.trim(),
      slug: finalSlug,
      description: description.trim(),
      logoUrl: logoUrl.trim(),
      isPublic,
      isActive,
    }
    void onSubmit(data)
  }

  const slugError = slugAvailable === false
  const publicUrl = slug.trim()
    ? `${publicBaseUrl.replace(/\/$/, '')}/liga/${slug.trim()}`
    : ''

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 480 }}>
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
      <TextField
        fullWidth
        name="logoUrl"
        label="Logo URL"
        type="url"
        value={logoUrl}
        onChange={(e) => setLogoUrl(e.target.value)}
        disabled={loading}
        sx={{ mb: 2 }}
      />
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
        disabled={loading || slugError}
        sx={{ minWidth: 120 }}
      >
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
