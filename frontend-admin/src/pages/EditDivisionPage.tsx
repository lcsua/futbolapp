import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { DivisionForm } from '../components/DivisionForm'
import { divisionsService } from '../api/divisions'
import type { DivisionFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditDivisionPage() {
  const params = useParams<{ leagueId?: string; divisionId?: string }>()
  const leagueId = useLeagueId()
  const divisionId = params.divisionId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const divisionsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/divisions` : '/divisions'

  const { data: divisions, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const division = divisions?.find((d) => d.id === divisionId)

  const updateMutation = useMutation({
    mutationFn: (data: DivisionFormData) =>
      divisionsService.update(leagueId!, divisionId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'divisions'] })
      navigate(divisionsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update division')
    },
  })

  const handleSubmit = (data: DivisionFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  if (!leagueId || !divisionId) {
    return <Alert severity="error">Missing league or division.</Alert>
  }

  if (isLoading || divisions === undefined) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'Failed to load division'}
      </Alert>
    )
  }

  if (!division) {
    return <Alert severity="error">Division not found.</Alert>
  }

  const initialValues: DivisionFormData = {
    name: division.name,
    description: division.description ?? '',
  }

  return (
    <Box>
      <Button component={RouterLink} to={divisionsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to divisions
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Edit division
      </Typography>
      <DivisionForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Save"
        title="Division details"
      />
    </Box>
  )
}
