import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { Box, Typography } from '@mui/material'
import { LeagueForm } from '../components/LeagueForm'
import { leaguesService } from '../api/leagues'
import type { LeagueFormData } from '../api/types'

export function CreateLeaguePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (data: LeagueFormData, logoFile?: File | null) => {
    setError(null)
    setSaving(true)
    try {
      const created = await leaguesService.create(data)
      if (logoFile && created?.id) {
        const upload = await leaguesService.uploadImage(created.id, logoFile)
        await leaguesService.update(created.id, { ...data, logoUrl: upload.url })
      }
      void queryClient.invalidateQueries({ queryKey: ['leagues'] })
      navigate('/', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create league')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Create league
      </Typography>
      <LeagueForm
        onSubmit={handleSubmit}
        loading={saving}
        error={error}
        submitLabel="Create"
        title="League details"
      />
    </Box>
  )
}
