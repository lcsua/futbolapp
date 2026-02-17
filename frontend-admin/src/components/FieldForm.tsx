import { useState } from 'react'
import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  FormControlLabel,
  Switch,
  Grid,
} from '@mui/material'
import type { FieldFormData } from '../api/types'

export interface FieldFormProps {
  initialValues?: Partial<FieldFormData>
  onSubmit: (data: FieldFormData) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
}

const defaultValues: FieldFormData = {
  name: '',
  address: '',
  city: '',
  geoLat: null,
  geoLng: null,
  isAvailable: true,
  description: '',
}

export function FieldForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: FieldFormProps) {
  const values: FieldFormData = { ...defaultValues, ...initialValues }
  const [name, setName] = useState(values.name)
  const [address, setAddress] = useState(values.address ?? '')
  const [city, setCity] = useState(values.city ?? '')
  const [geoLat, setGeoLat] = useState(values.geoLat != null ? String(values.geoLat) : '')
  const [geoLng, setGeoLng] = useState(values.geoLng != null ? String(values.geoLng) : '')
  const [isAvailable, setIsAvailable] = useState(values.isAvailable ?? true)
  const [description, setDescription] = useState(values.description ?? '')

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const nameTrim = name.trim()
    if (!nameTrim) return
    const latNum = geoLat.trim() === '' ? null : parseFloat(geoLat)
    const lngNum = geoLng.trim() === '' ? null : parseFloat(geoLng)
    const data: FieldFormData = {
      name: nameTrim,
      address: address.trim(),
      city: city.trim(),
      geoLat: latNum ?? null,
      geoLng: lngNum ?? null,
      isAvailable,
      description: description.trim(),
    }
    void onSubmit(data)
  }

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 560 }}>
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
        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Address"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="City"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <FormControlLabel
            control={
              <Switch
                checked={isAvailable}
                onChange={(e) => setIsAvailable(e.target.checked)}
                disabled={loading}
                color="primary"
              />
            }
            label="Available"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Latitude"
            type="number"
            inputProps={{ step: 'any' }}
            value={geoLat}
            onChange={(e) => setGeoLat(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            fullWidth
            label="Longitude"
            type="number"
            inputProps={{ step: 'any' }}
            value={geoLng}
            onChange={(e) => setGeoLng(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={12}>
          <TextField
            fullWidth
            label="Description"
            multiline
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={loading}
          />
        </Grid>
        <Grid size={12}>
          <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
            {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
          </Button>
        </Grid>
      </Grid>
    </Box>
  )
}
