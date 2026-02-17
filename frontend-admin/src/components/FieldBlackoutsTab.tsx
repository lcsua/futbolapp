import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import { fieldBlackoutsService } from '../api/fieldBlackouts'
import type { FieldBlackoutFormData } from '../api/types'

export interface FieldBlackoutsTabProps {
  leagueId: string
  fieldId: string
}

export function FieldBlackoutsTab({ leagueId, fieldId }: FieldBlackoutsTabProps) {
  const queryClient = useQueryClient()
  const [date, setDate] = useState('')
  const [startTime, setStartTime] = useState('09:00')
  const [endTime, setEndTime] = useState('17:00')
  const [reason, setReason] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'fields', fieldId, 'blackouts'],
    queryFn: ({ signal }) => fieldBlackoutsService.get(leagueId, fieldId, signal),
    enabled: !!leagueId && !!fieldId,
  })

  const createMutation = useMutation({
    mutationFn: (payload: FieldBlackoutFormData) =>
      fieldBlackoutsService.create(leagueId, fieldId, payload, undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'fields', fieldId, 'blackouts'] })
      setDate('')
      setStartTime('09:00')
      setEndTime('17:00')
      setReason('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (blackoutId: string) =>
      fieldBlackoutsService.delete(leagueId, fieldId, blackoutId, undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'fields', fieldId, 'blackouts'] })
    },
  })

  const handleAdd = () => {
    if (!date.trim()) {
      setValidationError('Date is required')
      return
    }
    const start = startTime.replace(':', '')
    const end = endTime.replace(':', '')
    if (start >= end) {
      setValidationError('Start time must be before end time')
      return
    }
    setValidationError(null)
    createMutation.mutate({
      date: date.trim(),
      startTime: startTime.length === 5 ? `${startTime}:00` : startTime,
      endTime: endTime.length === 5 ? `${endTime}:00` : endTime,
      reason: reason.trim(),
    })
  }

  const items = data?.items ?? []

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Box>
      <Typography variant="subtitle1" color="text.secondary" sx={{ mb: 2 }}>
        Add blackout periods when the field is unavailable.
      </Typography>
      {validationError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {validationError}
        </Alert>
      )}
      {createMutation.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {createMutation.error instanceof Error ? createMutation.error.message : 'Failed to add blackout'}
        </Alert>
      )}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'flex-end', mb: 3 }}>
        <TextField
          label="Date"
          type="date"
          size="small"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          inputProps={{ style: { width: 140 } }}
        />
        <TextField
          label="Start"
          type="time"
          size="small"
          value={startTime}
          onChange={(e) => setStartTime(e.target.value)}
          inputProps={{ style: { width: 100 } }}
        />
        <TextField
          label="End"
          type="time"
          size="small"
          value={endTime}
          onChange={(e) => setEndTime(e.target.value)}
          inputProps={{ style: { width: 100 } }}
        />
        <TextField
          label="Reason"
          size="small"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          sx={{ minWidth: 180 }}
        />
        <Button
          variant="contained"
          onClick={handleAdd}
          disabled={createMutation.isPending}
          sx={{ minWidth: 100 }}
        >
          {createMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Add'}
        </Button>
      </Box>
      {items.length === 0 ? (
        <Typography color="text.secondary">No blackouts defined.</Typography>
      ) : (
        <Table size="small" sx={{ maxWidth: 720 }}>
          <TableHead>
            <TableRow>
              <TableCell>Date</TableCell>
              <TableCell>Start</TableCell>
              <TableCell>End</TableCell>
              <TableCell>Reason</TableCell>
              <TableCell width={56} />
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((b) => (
              <TableRow key={b.id}>
                <TableCell>{b.date}</TableCell>
                <TableCell>{typeof b.startTime === 'string' ? b.startTime.slice(0, 5) : b.startTime}</TableCell>
                <TableCell>{typeof b.endTime === 'string' ? b.endTime.slice(0, 5) : b.endTime}</TableCell>
                <TableCell>{b.reason || '—'}</TableCell>
                <TableCell>
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => deleteMutation.mutate(b.id)}
                    disabled={deleteMutation.isPending}
                    aria-label="Delete blackout"
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  )
}
