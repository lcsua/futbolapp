import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Box, Typography } from '@mui/material'
import { LeagueForm } from '../components/LeagueForm'
import { leaguesService } from '../api/leagues'
import type { LeagueFormData } from '../api/types'

export function CreateLeaguePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: (data: LeagueFormData) => leaguesService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues'] })
      navigate('/', { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create league')
    },
  })

  const handleSubmit = (data: LeagueFormData) => {
    setError(null)
    createMutation.mutate(data)
  }

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create league
      </Typography>
      <LeagueForm
        onSubmit={handleSubmit}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="League details"
      />
    </Box>
  )
}
