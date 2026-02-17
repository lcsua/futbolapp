import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  CircularProgress,
  Typography,
} from '@mui/material'
import { LeagueForm } from '../components/LeagueForm'
import { leaguesService } from '../api/leagues'
import type { LeagueFormData } from '../api/types'

export function EditLeaguePage() {
  const { leagueId } = useParams<{ leagueId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const { data: league, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId],
    queryFn: ({ signal }) => leaguesService.getById(leagueId!, signal),
    enabled: !!leagueId,
  })

  const updateMutation = useMutation({
    mutationFn: (data: LeagueFormData) =>
      leaguesService.update(leagueId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues'] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId] })
      navigate('/', { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update league')
    },
  })

  const handleSubmit = (data: LeagueFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  if (!leagueId) {
    return (
      <Alert severity="error">Missing league id.</Alert>
    )
  }

  if (isLoading || !league) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'Failed to load league'}
      </Alert>
    )
  }

  const initialValues: LeagueFormData = {
    name: league.name,
    country: league.country,
    description: league.description ?? '',
    logoUrl: league.logoUrl ?? '',
  }

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Edit league
      </Typography>
      <LeagueForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Save"
        title="League details"
      />
    </Box>
  )
}
