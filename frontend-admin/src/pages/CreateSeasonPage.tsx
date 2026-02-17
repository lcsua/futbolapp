import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { SeasonForm } from '../components/SeasonForm'
import { seasonsService } from '../api/seasons'
import type { SeasonFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function CreateSeasonPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const seasonsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/seasons` : '/seasons'

  const createMutation = useMutation({
    mutationFn: (data: SeasonFormData) => seasonsService.create(leagueId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
      navigate(seasonsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create season')
    },
  })

  const handleSubmit = (data: SeasonFormData) => {
    setError(null)
    createMutation.mutate(data)
  }

  if (!leagueId) {
    return <Alert severity="error">Missing league.</Alert>
  }

  return (
    <Box>
      <Button component={RouterLink} to={seasonsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to seasons
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create season
      </Typography>
      <SeasonForm
        onSubmit={handleSubmit}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="Season details"
      />
    </Box>
  )
}
