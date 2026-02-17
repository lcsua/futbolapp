import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { FieldForm } from '../components/FieldForm'
import { fieldsService } from '../api/fields'
import type { FieldFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditFieldPage() {
  const params = useParams<{ leagueId?: string; fieldId?: string }>()
  const leagueId = useLeagueId()
  const fieldId = params.fieldId
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const fieldsBase = params.leagueId && leagueId ? `/leagues/${leagueId}/fields` : '/fields'

  const { data: fields, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['leagues', leagueId, 'fields'],
    queryFn: ({ signal }) => fieldsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const field = fields?.find((f) => f.id === fieldId)

  const updateMutation = useMutation({
    mutationFn: (data: FieldFormData) =>
      fieldsService.update(leagueId!, fieldId!, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'fields'] })
      navigate(fieldsBase, { replace: true })
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update field')
    },
  })

  const handleSubmit = (data: FieldFormData) => {
    setError(null)
    updateMutation.mutate(data)
  }

  if (!leagueId || !fieldId) {
    return <Alert severity="error">Missing league or field.</Alert>
  }

  if (isLoading || fields === undefined) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Alert severity="error">
        {queryError instanceof Error ? queryError.message : 'Failed to load field'}
      </Alert>
    )
  }

  if (!field) {
    return <Alert severity="error">Field not found.</Alert>
  }

  const initialValues: FieldFormData = {
    name: field.name,
    address: field.address ?? '',
    city: field.city ?? '',
    geoLat: field.geoLat,
    geoLng: field.geoLng,
    isAvailable: field.isAvailable,
    description: field.description ?? '',
  }

  return (
    <Box>
      <Button component={RouterLink} to={fieldsBase} startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to fields
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Edit field
      </Typography>
      <FieldForm
        initialValues={initialValues}
        onSubmit={handleSubmit}
        loading={updateMutation.isPending}
        error={error}
        submitLabel="Save"
        title="Field details"
      />
    </Box>
  )
}
