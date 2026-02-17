import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Tab, Tabs, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Link as RouterLink } from 'react-router-dom'
import { FieldForm } from '../components/FieldForm'
import { FieldAvailabilityTab } from '../components/FieldAvailabilityTab'
import { FieldBlackoutsTab } from '../components/FieldBlackoutsTab'
import { fieldsService } from '../api/fields'
import type { FieldFormData } from '../api/types'
import { useLeagueId } from '../contexts/LeagueContext'

export function EditFieldPage() {
  const params = useParams<{ leagueId?: string; fieldId?: string }>()
  const leagueId = useLeagueId()
  const fieldId = params.fieldId
  const queryClient = useQueryClient()
  const [tab, setTab] = useState(0)
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
      setError(null)
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
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        {field.name}
      </Typography>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tab label="Field info" />
        <Tab label="Availability" />
        <Tab label="Blackouts" />
      </Tabs>
      {tab === 0 && (
        <FieldForm
          initialValues={initialValues}
          onSubmit={handleSubmit}
          loading={updateMutation.isPending}
          error={error}
          submitLabel="Save"
          title="Field details"
        />
      )}
      {tab === 1 && leagueId && <FieldAvailabilityTab leagueId={leagueId} fieldId={fieldId} />}
      {tab === 2 && leagueId && <FieldBlackoutsTab leagueId={leagueId} fieldId={fieldId} />}
    </Box>
  )
}
