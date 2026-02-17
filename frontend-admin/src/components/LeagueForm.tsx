import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
} from '@mui/material'
import type { LeagueFormData } from '../api/types'

export interface LeagueFormProps {
  initialValues?: Partial<LeagueFormData>
  onSubmit: (data: LeagueFormData) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
}

const defaultValues: LeagueFormData = {
  name: '',
  country: '',
  description: '',
  logoUrl: '',
}

export function LeagueForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: LeagueFormProps) {
  const values: LeagueFormData = { ...defaultValues, ...initialValues }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = e.currentTarget
    const data: LeagueFormData = {
      name: (form.elements.namedItem('name') as HTMLInputElement).value.trim(),
      country: (form.elements.namedItem('country') as HTMLInputElement).value.trim(),
      description: (form.elements.namedItem('description') as HTMLInputElement).value.trim(),
      logoUrl: (form.elements.namedItem('logoUrl') as HTMLInputElement).value.trim(),
    }
    void onSubmit(data)
  }

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
        label="Name"
        required
        defaultValue={values.name}
        disabled={loading}
        sx={{ mb: 2 }}
        autoFocus
      />
      <TextField
        fullWidth
        name="country"
        label="Country"
        required
        defaultValue={values.country}
        disabled={loading}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="description"
        label="Description"
        multiline
        rows={2}
        defaultValue={values.description}
        disabled={loading}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="logoUrl"
        label="Logo URL"
        type="url"
        defaultValue={values.logoUrl}
        disabled={loading}
        sx={{ mb: 3 }}
      />
      <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
