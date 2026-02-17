import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
} from '@mui/material'
import type { DivisionFormData } from '../api/types'

export interface DivisionFormProps {
  initialValues?: Partial<DivisionFormData>
  onSubmit: (data: DivisionFormData) => void | Promise<void>
  loading?: boolean
  error?: string | null
  submitLabel: string
  title?: string
}

const defaultValues: DivisionFormData = {
  name: '',
  description: '',
}

export function DivisionForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: DivisionFormProps) {
  const values: DivisionFormData = { ...defaultValues, ...initialValues }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = e.currentTarget
    const data: DivisionFormData = {
      name: (form.elements.namedItem('name') as HTMLInputElement).value.trim(),
      description: (form.elements.namedItem('description') as HTMLInputElement).value.trim(),
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
        name="description"
        label="Description"
        multiline
        rows={2}
        defaultValue={values.description || ''}
        disabled={loading}
        sx={{ mb: 3 }}
      />
      <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
