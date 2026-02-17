import { useRef, useState, useCallback } from 'react'
import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  Grid,
} from '@mui/material'
import type { TeamFormData } from '../api/types'

const MAX_FILE_BYTES = 1024 * 1024
const ACCEPT_IMAGES = 'image/jpeg,image/png,image/gif,image/webp'
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function isValidEmail(value: string): boolean {
  return EMAIL_REGEX.test(value.trim())
}

export interface TeamFormProps {
  initialValues?: Partial<TeamFormData>
  onSubmit: (data: TeamFormData) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
}

const defaultValues: TeamFormData = {
  name: '',
  shortName: '',
  primaryColor: '#000000',
  secondaryColor: '#ffffff',
  foundedYear: undefined,
  delegateName: '',
  delegateContact: '',
  email: '',
  logoUrl: '',
  photoUrl: '',
}

export function TeamForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: TeamFormProps) {
  const values: TeamFormData = { ...defaultValues, ...initialValues }
  const [name, setName] = useState(values.name)
  const [shortName, setShortName] = useState(values.shortName ?? '')
  const [primaryColor, setPrimaryColor] = useState(values.primaryColor ?? '#000000')
  const [secondaryColor, setSecondaryColor] = useState(values.secondaryColor ?? '#ffffff')
  const [foundedYear, setFoundedYear] = useState<string>(values.foundedYear != null ? String(values.foundedYear) : '')
  const [delegateName, setDelegateName] = useState(values.delegateName ?? '')
  const [delegateContact, setDelegateContact] = useState(values.delegateContact ?? '')
  const [email, setEmail] = useState(values.email ?? '')
  const initialLogo = values.logoUrl && (values.logoUrl.startsWith('data:') || values.logoUrl.startsWith('http')) ? values.logoUrl : null
  const initialPhoto = values.photoUrl && (values.photoUrl.startsWith('data:') || values.photoUrl.startsWith('http')) ? values.photoUrl : null
  const [logoPreview, setLogoPreview] = useState<string | null>(initialLogo)
  const [logoDataUrl, setLogoDataUrl] = useState<string | null>(values.logoUrl?.startsWith('data:') ? values.logoUrl : null)
  const [logoRemoved, setLogoRemoved] = useState(false)
  const [photoPreview, setPhotoPreview] = useState<string | null>(initialPhoto)
  const [photoDataUrl, setPhotoDataUrl] = useState<string | null>(values.photoUrl?.startsWith('data:') ? values.photoUrl : null)
  const [photoRemoved, setPhotoRemoved] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  const [emailError, setEmailError] = useState<string | null>(null)
  const logoInputRef = useRef<HTMLInputElement>(null)
  const photoInputRef = useRef<HTMLInputElement>(null)

  const validateFile = useCallback((file: File, label: string): string | null => {
    if (!file.type.startsWith('image/')) return `${label} must be an image (e.g. JPEG, PNG, GIF, WebP).`
    if (file.size > MAX_FILE_BYTES) return `${label} must be no larger than 1 MB.`
    return null
  }, [])

  const handleLogoChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setFileError(null)
      const file = e.target.files?.[0]
      if (!file) {
        setLogoPreview(null)
        setLogoDataUrl(null)
        return
      }
      const err = validateFile(file, 'Logo')
      if (err) {
        setFileError(err)
        setLogoPreview(null)
        setLogoDataUrl(null)
        if (logoInputRef.current) logoInputRef.current.value = ''
        return
      }
      const reader = new FileReader()
      reader.onload = () => {
        const dataUrl = reader.result as string
        setLogoDataUrl(dataUrl)
        setLogoPreview(dataUrl)
        setLogoRemoved(false)
      }
      reader.readAsDataURL(file)
    },
    [validateFile]
  )

  const handlePhotoChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setFileError(null)
      const file = e.target.files?.[0]
      if (!file) {
        setPhotoPreview(null)
        setPhotoDataUrl(null)
        return
      }
      const err = validateFile(file, 'Photo')
      if (err) {
        setFileError(err)
        setPhotoPreview(null)
        setPhotoDataUrl(null)
        if (photoInputRef.current) photoInputRef.current.value = ''
        return
      }
      const reader = new FileReader()
      reader.onload = () => {
        const dataUrl = reader.result as string
        setPhotoDataUrl(dataUrl)
        setPhotoPreview(dataUrl)
        setPhotoRemoved(false)
      }
      reader.readAsDataURL(file)
    },
    [validateFile]
  )

  const clearLogo = useCallback(() => {
    setLogoPreview(null)
    setLogoDataUrl(null)
    setLogoRemoved(true)
    setFileError(null)
    if (logoInputRef.current) logoInputRef.current.value = ''
  }, [])

  const clearPhoto = useCallback(() => {
    setPhotoPreview(null)
    setPhotoDataUrl(null)
    setPhotoRemoved(true)
    setFileError(null)
    if (photoInputRef.current) photoInputRef.current.value = ''
  }, [])

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setFileError(null)
    setEmailError(null)
    const nameTrim = name.trim()
    if (!nameTrim) return
    const emailTrim = email.trim()
    if (emailTrim && !isValidEmail(emailTrim)) {
      setEmailError('Enter a valid email address.')
      return
    }
    if (fileError) return
    const logoUrl =
      logoRemoved ? undefined : (logoDataUrl ?? (logoPreview ? logoPreview : initialValues?.logoUrl) ?? undefined)
    const photoUrl =
      photoRemoved ? undefined : (photoDataUrl ?? (photoPreview ? photoPreview : initialValues?.photoUrl) ?? undefined)
    const foundedYearNum = foundedYear.trim() === '' ? undefined : parseInt(foundedYear, 10)
    if (foundedYear.trim() !== '' && (Number.isNaN(foundedYearNum) || foundedYearNum! < 1800 || foundedYearNum! > 2100)) {
      return
    }
    const data: TeamFormData = {
      name: nameTrim,
      shortName: shortName.trim() || undefined,
      primaryColor: primaryColor || undefined,
      secondaryColor: secondaryColor || undefined,
      foundedYear: foundedYearNum,
      delegateName: delegateName.trim() || undefined,
      delegateContact: delegateContact.trim() || undefined,
      email: emailTrim || undefined,
      logoUrl,
      photoUrl,
    }
    void onSubmit(data)
  }

  const canSubmit = !fileError && !loading && !emailError

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 640 }}>
      {title && (
        <Typography variant="h6" component="h2" sx={{ mb: 2, fontWeight: 600 }}>
          {title}
        </Typography>
      )}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      <Grid container spacing={2}>
        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Name"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={loading}
            autoFocus
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Short name"
            value={shortName}
            onChange={(e) => setShortName(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Founded year"
            type="number"
            inputProps={{ min: 1800, max: 2100 }}
            value={foundedYear}
            onChange={(e) => setFoundedYear(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Primary color"
            type="color"
            value={primaryColor}
            onChange={(e) => setPrimaryColor(e.target.value)}
            disabled={loading}
            slotProps={{
              input: {
                sx: { width: '100%', height: 40, p: 0.5, cursor: 'pointer' },
              },
            }}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Secondary color"
            type="color"
            value={secondaryColor}
            onChange={(e) => setSecondaryColor(e.target.value)}
            disabled={loading}
            slotProps={{
              input: {
                sx: { width: '100%', height: 40, p: 0.5, cursor: 'pointer' },
              },
            }}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Delegate name"
            value={delegateName}
            onChange={(e) => setDelegateName(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Delegate contact"
            value={delegateContact}
            onChange={(e) => setDelegateContact(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Email"
            type="email"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              setEmailError(null)
            }}
            onBlur={() => {
              if (email.trim() && !isValidEmail(email.trim())) setEmailError('Enter a valid email address.')
              else setEmailError(null)
            }}
            error={!!emailError}
            helperText={emailError}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Logo (max 1 MB, images only)
          </Typography>
          <input
            ref={logoInputRef}
            type="file"
            accept={ACCEPT_IMAGES}
            onChange={handleLogoChange}
            disabled={loading}
            style={{ display: 'block', marginBottom: 8 }}
            aria-label="Upload logo"
          />
          {logoPreview && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              <Box
                component="img"
                src={logoPreview}
                alt="Logo preview"
                sx={{ maxWidth: 80, maxHeight: 80, objectFit: 'contain', border: 1, borderColor: 'divider', borderRadius: 1 }}
              />
              <Button type="button" size="small" onClick={clearLogo} disabled={loading}>
                Remove
              </Button>
            </Box>
          )}
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Photo (max 1 MB, images only)
          </Typography>
          <input
            ref={photoInputRef}
            type="file"
            accept={ACCEPT_IMAGES}
            onChange={handlePhotoChange}
            disabled={loading}
            style={{ display: 'block', marginBottom: 8 }}
            aria-label="Upload photo"
          />
          {photoPreview && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              <Box
                component="img"
                src={photoPreview}
                alt="Photo preview"
                sx={{ maxWidth: 80, maxHeight: 80, objectFit: 'contain', border: 1, borderColor: 'divider', borderRadius: 1 }}
              />
              <Button type="button" size="small" onClick={clearPhoto} disabled={loading}>
                Remove
              </Button>
            </Box>
          )}
        </Grid>
        {fileError && (
          <Grid size={12}>
            <Alert severity="error">{fileError}</Alert>
          </Grid>
        )}
        <Grid size={12}>
          <Button type="submit" variant="contained" disabled={!canSubmit} sx={{ minWidth: 120 }}>
            {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
          </Button>
        </Grid>
      </Grid>
    </Box>
  )
}
