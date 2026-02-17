import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { FieldForm } from '../components/FieldForm'
import { fieldsService } from '../api/fields'
import type { FieldFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function CreateFieldPage() {
  const params = useParams<{ leagueId?: string }>()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const fieldsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/fields` : '/fields'

  const createMutation = useMutation({
    mutationFn: (data: FieldFormData) =>
      fieldsService.create(leagueId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'fields'] })
      navigate(fieldsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create field')
    },
  })

  const handleSubmit = (data: FieldFormData) => {
    setError(null)
    createMutation.mutate(data)
  }

  if (!leagueId) {
    return <Alert severity="error">Missing league.</Alert>
  }

  return (
    <Box>
      <Button component={RouterLink} to={fieldsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to fields
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create field
      </Typography>
      <FieldForm
        onSubmit={handleSubmit}
        loading={createMutation.isPending}
        error={error}
        submitLabel="Create"
        title="Field details"
      />
    </Box>
  )
}
