import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
} from '@mui/material'
import type { SeasonFormData } from '../api/types'

export interface SeasonFormProps {
  initialValues?: Partial<SeasonFormData>
  onSubmit: (data: SeasonFormData) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
}

const defaultValues: SeasonFormData = {
  name: '',
  startDate: '',
  endDate: '',
}

export function SeasonForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: SeasonFormProps) {
  const values: SeasonFormData = { ...defaultValues, ...initialValues }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = e.currentTarget
    const data: SeasonFormData = {
      name: (form.elements.namedItem('name') as HTMLInputElement).value.trim(),
      startDate: (form.elements.namedItem('startDate') as HTMLInputElement).value.trim(),
      endDate: (form.elements.namedItem('endDate') as HTMLInputElement).value.trim(),
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
        <Alert severity="error" sx={{ mb: 2 }}>
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
        name="startDate"
        label="Start date"
        type="date"
        required
        defaultValue={values.startDate}
        disabled={loading}
        InputLabelProps={{ shrink: true }}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="endDate"
        label="End date"
        type="date"
        defaultValue={values.endDate || ''}
        disabled={loading}
        InputLabelProps={{ shrink: true }}
        sx={{ mb: 3 }}
      />
      <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
