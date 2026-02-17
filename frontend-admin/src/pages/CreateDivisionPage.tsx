import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { DivisionForm } from '../components/DivisionForm'
import { divisionsService } from '../api/divisions'
import type { DivisionFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function CreateDivisionPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const divisionsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/divisions` : '/divisions'

  const createMutation = useMutation({
    mutationFn: (data: DivisionFormData) => divisionsService.create(leagueId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'divisions'] })
      navigate(divisionsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create division')
    },
  })

  const handleSubmit = (data: DivisionFormData) => {
    setError(null)
    createMutation.mutate(data)
  }

  if (!leagueId) {
    return <Alert severity="error">Missing league.</Alert>
  }

  return (
    <Box>
      <Button component={RouterLink} to={divisionsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to divisions
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create division
      </Typography>
      <DivisionForm
        onSubmit={handleSubmit}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="Division details"
      />
    </Box>
  )
}
