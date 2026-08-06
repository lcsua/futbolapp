import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  FormHelperText,
  TextField,
  Typography,
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
  isPublic: false,
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
      isPublic: (form.elements.namedItem('isPublic') as HTMLInputElement).checked,
    }
    void onSubmit(data)
  }

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 520 }}>
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
        label="Nombre"
        required
        defaultValue={values.name}
        disabled={loading}
        sx={{ mb: 2 }}
        autoFocus
      />
      <TextField
        fullWidth
        name="startDate"
        label="Fecha de inicio"
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
        label="Fecha de fin"
        type="date"
        defaultValue={values.endDate || ''}
        disabled={loading}
        InputLabelProps={{ shrink: true }}
        sx={{ mb: 2 }}
      />
      <FormControlLabel
        sx={{ mb: 0.5, alignItems: 'flex-start' }}
        control={
          <Checkbox
            name="isPublic"
            defaultChecked={values.isPublic}
            disabled={loading}
            sx={{ pt: 0.25 }}
          />
        }
        label="Mostrar esta temporada en la web de acceso público"
      />
      <FormHelperText sx={{ mt: 0, mb: 3, ml: 4 }}>
        Desmarcá esta opción mientras la temporada esté en borrador o en configuración.
        Si está desmarcada, no aparece en los combos de tabla, resultados y partidos del sitio público.
      </FormHelperText>
      <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
