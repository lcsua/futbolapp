import { useState } from 'react'
import {
  Box,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  FormControlLabel,
  Checkbox,
} from '@mui/material'
import type { DivisionFormData } from '../api/types'

function toTimeInput(iso: string | null | undefined): string {
  if (!iso) return ''
  const m = iso.match(/^(\d{2}):(\d{2})/)
  return m ? `${m[1]}:${m[2]}` : ''
}

function normalizeTimeForApi(s: string): string | null {
  const t = s.trim()
  if (!t) return null
  return t.length === 5 ? `${t}:00` : t
}

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
  kickoffRestrictionEnabled: false,
  kickoffRestrictionStart: null,
  kickoffRestrictionEnd: null,
}

export function DivisionForm({
  initialValues,
  onSubmit,
  loading = false,
  error = null,
  submitLabel,
  title,
}: DivisionFormProps) {
  const merged: DivisionFormData = { ...defaultValues, ...initialValues }

  const [kickoffRestrictionEnabled, setKickoffRestrictionEnabled] = useState(
    merged.kickoffRestrictionEnabled,
  )
  const [kickoffStart, setKickoffStart] = useState(toTimeInput(merged.kickoffRestrictionStart))
  const [kickoffEnd, setKickoffEnd] = useState(toTimeInput(merged.kickoffRestrictionEnd))
  const [localError, setLocalError] = useState<string | null>(null)

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setLocalError(null)
    const form = e.currentTarget
    const name = (form.elements.namedItem('name') as HTMLInputElement).value.trim()
    const description = (form.elements.namedItem('description') as HTMLInputElement).value.trim()

    if (kickoffRestrictionEnabled) {
      const start = normalizeTimeForApi(kickoffStart)
      const end = normalizeTimeForApi(kickoffEnd)
      if (!start || !end) {
        setLocalError('Indica hora de inicio y fin del horario prohibido, o desactiva la restricción.')
        return
      }
      const data: DivisionFormData = {
        name,
        description,
        kickoffRestrictionEnabled: true,
        kickoffRestrictionStart: start,
        kickoffRestrictionEnd: end,
      }
      void onSubmit(data)
      return
    }

    const data: DivisionFormData = {
      name,
      description,
      kickoffRestrictionEnabled: false,
      kickoffRestrictionStart: null,
      kickoffRestrictionEnd: null,
    }
    void onSubmit(data)
  }

  const showError = error ?? localError

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 520 }}>
      {title && (
        <Typography variant="h6" component="h2" sx={{ mb: 2, fontWeight: 600 }}>
          {title}
        </Typography>
      )}
      {showError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {showError}
        </Alert>
      )}
      <TextField
        fullWidth
        name="name"
        label="Nombre"
        required
        defaultValue={merged.name}
        disabled={loading}
        sx={{ mb: 2 }}
        autoFocus
      />
      <TextField
        fullWidth
        name="description"
        label="Descripción"
        multiline
        rows={2}
        defaultValue={merged.description || ''}
        disabled={loading}
        sx={{ mb: 2 }}
      />
      <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
        Restricción de horario de inicio (opcional)
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Por ejemplo, categoría senior: no programar partidos con inicio entre dos horas (p. ej. 13:00–15:00 en
        días de mucho calor). La generación de fixtures no usará esos horarios de inicio para esta división.
      </Typography>
      <FormControlLabel
        control={
          <Checkbox
            checked={kickoffRestrictionEnabled}
            onChange={(_, c) => setKickoffRestrictionEnabled(c)}
            disabled={loading}
          />
        }
        label="Bloquear inicios en un rango horario"
        sx={{ mb: 1 }}
      />
      {kickoffRestrictionEnabled && (
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mb: 2 }}>
          <TextField
            label="Inicio del rango prohibido"
            type="time"
            value={kickoffStart}
            onChange={(ev) => setKickoffStart(ev.target.value)}
            disabled={loading}
            InputLabelProps={{ shrink: true }}
            inputProps={{ step: 300 }}
            sx={{ minWidth: 200 }}
          />
          <TextField
            label="Fin del rango (exclusivo)"
            type="time"
            value={kickoffEnd}
            onChange={(ev) => setKickoffEnd(ev.target.value)}
            disabled={loading}
            InputLabelProps={{ shrink: true }}
            inputProps={{ step: 300 }}
            sx={{ minWidth: 200 }}
          />
        </Box>
      )}
      <Button type="submit" variant="contained" disabled={loading} sx={{ minWidth: 120 }}>
        {loading ? <CircularProgress size={24} color="inherit" /> : submitLabel}
      </Button>
    </Box>
  )
}
