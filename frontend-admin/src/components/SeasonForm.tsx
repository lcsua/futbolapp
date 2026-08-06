import { useEffect, useState } from 'react'
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
  const merged = { ...defaultValues, ...initialValues }
  const [name, setName] = useState(merged.name)
  const [startDate, setStartDate] = useState(merged.startDate)
  const [endDate, setEndDate] = useState(merged.endDate || '')
  const [isPublic, setIsPublic] = useState(merged.isPublic === true)

  useEffect(() => {
    setName(initialValues?.name ?? '')
    setStartDate(initialValues?.startDate ?? '')
    setEndDate(initialValues?.endDate ?? '')
    setIsPublic(initialValues?.isPublic === true)
  }, [
    initialValues?.name,
    initialValues?.startDate,
    initialValues?.endDate,
    initialValues?.isPublic,
  ])

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    void onSubmit({
      name: name.trim(),
      startDate: startDate.trim(),
      endDate: endDate.trim(),
      isPublic,
    })
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
        value={name}
        onChange={(e) => setName(e.target.value)}
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
        value={startDate}
        onChange={(e) => setStartDate(e.target.value)}
        disabled={loading}
        InputLabelProps={{ shrink: true }}
        sx={{ mb: 2 }}
      />
      <TextField
        fullWidth
        name="endDate"
        label="Fecha de fin"
        type="date"
        value={endDate}
        onChange={(e) => setEndDate(e.target.value)}
        disabled={loading}
        InputLabelProps={{ shrink: true }}
        sx={{ mb: 2 }}
      />
      <FormControlLabel
        sx={{ mb: 0.5, alignItems: 'flex-start' }}
        control={
          <Checkbox
            checked={isPublic}
            onChange={(e) => setIsPublic(e.target.checked)}
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
