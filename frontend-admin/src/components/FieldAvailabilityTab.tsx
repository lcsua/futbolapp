import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControlLabel,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { fieldAvailabilityService } from '../api/fieldAvailability'
import type { FieldAvailabilitySlot } from '../api/types'

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

function timeToInputValue(t: string): string {
  if (!t) return '09:00'
  const part = t.split('T')[0].split(':').slice(0, 2).join(':')
  return part.length >= 5 ? part : t.slice(0, 5)
}

export interface FieldAvailabilityTabProps {
  leagueId: string
  fieldId: string
}

export function FieldAvailabilityTab({ leagueId, fieldId }: FieldAvailabilityTabProps) {
  const queryClient = useQueryClient()
  const [slots, setSlots] = useState<FieldAvailabilitySlot[]>([])
  const [validationError, setValidationError] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'fields', fieldId, 'availability'],
    queryFn: ({ signal }) => fieldAvailabilityService.get(leagueId, fieldId, signal),
    enabled: !!leagueId && !!fieldId,
  })

  useEffect(() => {
    if (!data) return
    const byDay = new Map<number, FieldAvailabilitySlot>()
    ;(data.items ?? []).forEach((item) => {
      byDay.set(item.dayOfWeek, {
        dayOfWeek: item.dayOfWeek,
        startTime: timeToInputValue(item.startTime),
        endTime: timeToInputValue(item.endTime),
        isActive: item.isActive,
      })
    })
    const next: FieldAvailabilitySlot[] = []
    for (let d = 0; d <= 6; d++) {
      next.push(
        byDay.get(d) ?? {
          dayOfWeek: d,
          startTime: '09:00',
          endTime: '17:00',
          isActive: false,
        }
      )
    }
    setSlots(next)
  }, [data])

  const putMutation = useMutation({
    mutationFn: (payload: FieldAvailabilitySlot[]) =>
      fieldAvailabilityService.put(leagueId, fieldId, payload, undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'fields', fieldId, 'availability'] })
    },
  })

  const updateSlot = (dayIndex: number, patch: Partial<FieldAvailabilitySlot>) => {
    setSlots((prev) =>
      prev.map((s) => (s.dayOfWeek === dayIndex ? { ...s, ...patch } : s))
    )
  }

  const validate = (): boolean => {
    for (const s of slots) {
      if (!s.isActive) continue
      const start = s.startTime.replace(':', '')
      const end = s.endTime.replace(':', '')
      if (start >= end) {
        setValidationError(`On ${DAY_NAMES[s.dayOfWeek]}: start time must be before end time`)
        return false
      }
    }
    setValidationError(null)
    return true
  }

  const handleSave = () => {
    if (!validate()) return
    const toSend = slots
      .filter((s) => s.isActive)
      .map((s) => ({
        dayOfWeek: s.dayOfWeek,
        startTime: s.startTime.length === 5 ? `${s.startTime}:00` : s.startTime,
        endTime: s.endTime.length === 5 ? `${s.endTime}:00` : s.endTime,
        isActive: true,
      }))
    putMutation.mutate(toSend)
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (slots.length === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Box>
      <Typography variant="subtitle1" color="text.secondary" sx={{ mb: 2 }}>
        Set available time ranges per day. Start must be before end.
      </Typography>
      {validationError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {validationError}
        </Alert>
      )}
      {putMutation.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {putMutation.error instanceof Error ? putMutation.error.message : 'Failed to save'}
        </Alert>
      )}
      <Table size="small" sx={{ maxWidth: 560 }}>
        <TableHead>
          <TableRow>
            <TableCell>Day</TableCell>
            <TableCell>Start</TableCell>
            <TableCell>End</TableCell>
            <TableCell>Active</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {slots.map((s) => (
            <TableRow key={s.dayOfWeek}>
              <TableCell>{DAY_NAMES[s.dayOfWeek]}</TableCell>
              <TableCell>
                <TextField
                  type="time"
                  size="small"
                  value={s.startTime}
                  onChange={(e) => updateSlot(s.dayOfWeek, { startTime: e.target.value })}
                  disabled={!s.isActive}
                  inputProps={{ style: { width: 120 } }}
                />
              </TableCell>
              <TableCell>
                <TextField
                  type="time"
                  size="small"
                  value={s.endTime}
                  onChange={(e) => updateSlot(s.dayOfWeek, { endTime: e.target.value })}
                  disabled={!s.isActive}
                  inputProps={{ style: { width: 120 } }}
                />
              </TableCell>
              <TableCell>
                <FormControlLabel
                  control={
                    <Switch
                      size="small"
                      checked={s.isActive}
                      onChange={(e) => updateSlot(s.dayOfWeek, { isActive: e.target.checked })}
                    />
                  }
                  label=""
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Button
        variant="contained"
        onClick={handleSave}
        disabled={putMutation.isPending}
        sx={{ mt: 2, minWidth: 120 }}
      >
        {putMutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Save availability'}
      </Button>
    </Box>
  )
}
